using System.Diagnostics;
using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Core.Services;

public class WebAppInstaller
{
    private readonly IIisInstaller _iisInstaller;
    private readonly IProcessExecutor _executor;

    public WebAppInstaller() : this(new ProcessExecutorService(), new IisInstaller()) { }

    public WebAppInstaller(IProcessExecutor executor) : this(executor, new IisInstaller(executor)) { }

    public WebAppInstaller(IProcessExecutor executor, IIisInstaller iisInstaller)
    {
        _executor = executor;
        _iisInstaller = iisInstaller;
    }

    private static int TotalSteps(string siteName) =>
        string.Equals(siteName, "WebAppUI", StringComparison.OrdinalIgnoreCase) ? 7 : 6;

    private static string StepLabel(int current, int total) => $"  [{current}/{total}] ";

    private static void WriteStep(string label, bool? success = null)
    {
        if (success == true)
            UIScope.WriteMessage($"{label}[green][[OK]][/]");
        else if (success == false)
            UIScope.WriteMessage($"{label}[red][[FALHA]][/]");
        else
            UIScope.WriteMessage(label);
    }

    public async Task<bool> InstallAsync(WebAppModel model, InstallationPaths? paths = null, RollbackManager? rollbackManager = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        try
        {
            UIScope.WriteMessage($"\n [cyan]{MarkupHelper.Escape(model.SiteName)}[/] Iniciando instalacao...");

            var totalSteps = TotalSteps(model.SiteName);
            string? stagingDir = null;
            string? webRootDir = null;

            // Etapa 1: Extracao deterministica do MSI via Admin Install
            WriteStep($"{StepLabel(1, totalSteps)}Extraindo MSI via Admin Install (msiexec /a)");
            await (UIScope.Current?.ShowStatus("Extraindo arquivos do MSI...", async update =>
            {
                update("Executando msiexec /a...");
                stagingDir = await AdminInstallAsync(model);
                if (stagingDir != null)
                {
                    webRootDir = LocateWebRoot(stagingDir);
                }
            }) ?? Task.CompletedTask);

            if (stagingDir == null || webRootDir == null)
            {
                WriteStep("", false);
                UIScope.WriteMessage($"   [red][[ERRO]] Falha ao extrair ou localizar arquivos do WebApp no pacote MSI.[/]");
                return false;
            }

            WriteStep("", true);
            UIScope.WriteMessage($"         Origem detectada: [cyan]{MarkupHelper.Escape(webRootDir)}[/]");

            // Etapa 2: Copiar arquivos para o destino final
            WriteStep($"{StepLabel(2, totalSteps)}Copiando arquivos para {MarkupHelper.Escape(model.TargetDirectory)}");
            var copied = false;

            await (UIScope.Current?.ShowStatus("Copiando para destino...", async update =>
            {
                copied = await CopyToTargetAsync(webRootDir, model.TargetDirectory);

                // Limpeza da pasta de staging temporaria
                if (Directory.Exists(stagingDir))
                {
                    try { Directory.Delete(stagingDir, true); }
                    catch (Exception ex)
                    {
                        AuditLogger.Log("WebApp Staging Cleanup", stagingDir, false, ex.Message);
                    }
                }
            }) ?? Task.CompletedTask);

            if (!copied)
            {
                WriteStep("", false);
                return false;
            }
            WriteStep("", true);

            // Etapa 3: Conceder permissoes NTFS para IIS_IUSRS
            WriteStep($"{StepLabel(3, totalSteps)}Configurando permissoes NTFS (IIS_IUSRS)");
            var permissionsOk = await _iisInstaller.GrantDirectoryPermissionsAsync(model.TargetDirectory);
            WriteStep("", permissionsOk);

            // Etapa 4: Provisionamento do IIS (AppPool e Site)
            WriteStep($"{StepLabel(4, totalSteps)}Configurando IIS (AppPool e Site na porta {model.Port})");
            var iisOk = await EnsureIisConfiguredAsync(model, rollbackManager);
            WriteStep("", iisOk);
            if (!iisOk) return false;

            // Etapa 5: Configurar web.config
            var overallSuccess = true;
            var isUI = string.Equals(model.SiteName, "WebAppUI", StringComparison.OrdinalIgnoreCase);

            if (isUI)
            {
                WriteStep($"{StepLabel(5, totalSteps)}Configurando web.config (WebAppUI)");
                var webConfigOk = WebAppConfigHelper.UpdateWebAppUIConfig(model.TargetDirectory);
                WriteStep("", webConfigOk);
                if (!webConfigOk) overallSuccess = false;
            }
            else
            {
                WriteStep($"{StepLabel(5, totalSteps)}Configurando web.config (WebAppDS)");
                var webConfigOk = WebAppConfigHelper.UpdateWebAppDSConfig(model.TargetDirectory);
                WriteStep("", webConfigOk);
                if (!webConfigOk) overallSuccess = false;
            }

            // Etapa 6: Reiniciar IIS (Site e AppPool)
            WriteStep($"{StepLabel(6, totalSteps)}Reiniciando Site e AppPool");
            var restarted = await RestartIisAsync(model.SiteName, model.AppPoolName);
            WriteStep("", restarted);
            if (!restarted) overallSuccess = false;

            // Etapa 7 (WebAppUI apenas): DLL fabricante
            if (isUI)
            {
                WriteStep($"{StepLabel(7, totalSteps)}Copiando fabricante.Configuracao");
                var dllCopied = paths != null && CopyFabricanteConfigDll(paths);
                WriteStep("", dllCopied);
                if (!dllCopied) overallSuccess = false;
            }

            return overallSuccess;
        }
        catch (Exception ex)
        {
            UIScope.WriteError($"{ex.Message}");
            return false;
        }
    }

    private async Task<bool> EnsureIisConfiguredAsync(WebAppModel model, RollbackManager? rollbackManager = null)
    {
        UIScope.WriteMessage($" [cyan]Verificando IIS para:[/] {MarkupHelper.Escape(model.SiteName)}");

        var appPoolExists = await _iisInstaller.AppPoolExistsAsync(model.AppPoolName);
        if (!appPoolExists)
        {
            var created = await _iisInstaller.CreateApplicationPoolAsync(model.AppPoolName, "v4.0", "Classic");
            if (!created)
            {
                UIScope.WriteMessage($"   [red][[ERRO]] Falha ao criar Application Pool {MarkupHelper.Escape(model.AppPoolName)}[/]");
                return false;
            }

            rollbackManager?.Push(async () =>
            {
                UIScope.WriteMessage($"   [yellow][[ROLLBACK]] Removendo AppPool:[/] {MarkupHelper.Escape(model.AppPoolName)}");
                await _iisInstaller.RemoveAppPoolAsync(model.AppPoolName);
            });
        }
        else
        {
            UIScope.WriteMessage($"   [gray][[INFO]] AppPool {MarkupHelper.Escape(model.AppPoolName)} ja existe.[/]");
        }

        var siteExists = await _iisInstaller.SiteExistsAsync(model.SiteName);
        if (!siteExists)
        {
            var created = await _iisInstaller.CreateSiteAsync(
                model.SiteName,
                model.AppPoolName,
                model.TargetDirectory,
                model.Port
            );
            if (!created)
            {
                UIScope.WriteMessage($"   [red][[ERRO]] Falha ao criar WebSite {MarkupHelper.Escape(model.SiteName)}[/]");
                return false;
            }

            rollbackManager?.Push(async () =>
            {
                UIScope.WriteMessage($"   [yellow][[ROLLBACK]] Removendo WebSite:[/] {MarkupHelper.Escape(model.SiteName)}");
                await _iisInstaller.RemoveSiteAsync(model.SiteName);
            });
        }
        else
        {
            UIScope.WriteMessage($"   [gray][[INFO]] WebSite {MarkupHelper.Escape(model.SiteName)} ja existe. Atualizando PhysicalPath...");
            await _iisInstaller.UpdateSitePhysicalPathAsync(model.SiteName, model.TargetDirectory);
        }

        UIScope.WriteMessage($"   [green][[OK]] IIS configurado com sucesso para {MarkupHelper.Escape(model.SiteName)}.[/]");
        return true;
    }

    public static string? LocateWebRoot(string stagingDir)
    {
        if (!Directory.Exists(stagingDir))
            return null;

        // 1. O proprio diretorio raiz ja e um WebRoot
        if (IsWebRoot(stagingDir))
            return stagingDir;

        // 2. Procurar subdiretorios com web.config
        var configs = Directory.GetFiles(stagingDir, "web.config", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(stagingDir, "Web.config", SearchOption.AllDirectories))
            .Distinct()
            .OrderBy(f => f.Length);

        foreach (var config in configs)
        {
            var dir = Path.GetDirectoryName(config);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                return dir;
        }

        // 3. Procurar diretorio contendo subpasta "bin" com DLLs
        var binDirs = Directory.GetDirectories(stagingDir, "bin", SearchOption.AllDirectories);
        if (binDirs.Length > 0)
        {
            foreach (var binDir in binDirs.OrderBy(d => d.Length))
            {
                if (Directory.GetFiles(binDir, "*.dll").Length > 0)
                {
                    var parent = Directory.GetParent(binDir)?.FullName;
                    if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                        return parent;
                }
            }
        }

        // 4. Procurar diretorios com arquivos deployables (.dll, .aspx, .svc, .config)
        var allDirs = Directory.GetDirectories(stagingDir, "*", SearchOption.AllDirectories)
            .Append(stagingDir)
            .OrderBy(d => d.Length);

        foreach (var dir in allDirs)
        {
            if (HasDeployableFiles(dir, SearchOption.TopDirectoryOnly))
                return dir;
        }

        return Directory.GetFiles(stagingDir, "*.*", SearchOption.AllDirectories).Length > 0
            ? stagingDir
            : null;
    }

    public static bool IsWebRoot(string directory)
    {
        if (!Directory.Exists(directory))
            return false;

        if (File.Exists(Path.Combine(directory, "web.config")) || File.Exists(Path.Combine(directory, "Web.config")))
            return true;

        var binDir = Path.Combine(directory, "bin");
        if (Directory.Exists(binDir) && Directory.GetFiles(binDir, "*.dll").Length > 0)
            return true;

        return false;
    }

    internal static string? LocateInstalledPath(string forcedInstallPath, string siteName)
    {
        if (HasDeployableFiles(forcedInstallPath, SearchOption.TopDirectoryOnly))
            return forcedInstallPath;

        var subfolder = Path.Combine(forcedInstallPath, siteName);
        if (HasDeployableFiles(subfolder, SearchOption.AllDirectories))
            return subfolder;

        return null;
    }

    public static bool HasDeployableFiles(string directory, SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        if (!Directory.Exists(directory))
            return false;

        return Directory.GetFiles(directory, "*.dll", searchOption).Length > 0 ||
               Directory.GetFiles(directory, "*.aspx", searchOption).Length > 0 ||
               Directory.GetFiles(directory, "*.svc", searchOption).Length > 0 ||
               Directory.GetFiles(directory, "*.config", searchOption).Length > 0;
    }

    private static async Task<string?> AdminInstallAsync(WebAppModel model)
    {
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "InstaladorNewAcesso", "AdminInstall", model.SiteName);
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); }
                catch { /* best effort */ }
            }
            Directory.CreateDirectory(tempDir);

            var logArg = "";
            string? logPath = null;
            if (model.GenerateLog)
            {
                logPath = MsiLogHelper.GenerateLogFilePath(model.MsiPath + "_admin");
                logArg = $" /lvx* \"{logPath}\"";
            }

            var args = $"/a \"{model.MsiPath}\" TARGETDIR=\"{tempDir}\" /qn{logArg}";
            UIScope.WriteMessage($"   [gray][[DEBUG]] msiexec.exe {MarkupHelper.Escape(args)}[/]");

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "msiexec.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            UIScope.WriteMessage($"   [gray][[DEBUG]] Admin Install ExitCode: {process.ExitCode}[/]");

            if (process.ExitCode != 0)
            {
                if (model.GenerateLog && logPath != null)
                    MsiLogHelper.DisplayLogAnalysis(logPath);
                return null;
            }

            var files = Directory.GetFiles(tempDir, "*.*", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                UIScope.WriteMessage($"   [red][[ERRO]] Nenhum arquivo extraido para: {MarkupHelper.Escape(tempDir)}[/]");
                return null;
            }

            return tempDir;
        }
        catch (Exception ex)
        {
            UIScope.WriteError($"Admin Install: {ex.Message}");
            return null;
        }
    }

    private static async Task<bool> CopyToTargetAsync(string sourceDir, string targetDir)
    {
        try
        {
            Directory.CreateDirectory(targetDir);
            var robocopyArgs = $"\"{sourceDir}\" \"{targetDir}\" /E /R:3 /W:5";
            UIScope.WriteMessage($"   [gray][[DEBUG]] robocopy.exe {MarkupHelper.Escape(robocopyArgs)}[/]");

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "robocopy.exe",
                    Arguments = robocopyArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            UIScope.WriteMessage($"   [gray][[DEBUG]] Robocopy ExitCode: {process.ExitCode}[/]");

            return process.ExitCode >= 0 && process.ExitCode <= 7;
        }
        catch (Exception ex)
        {
            UIScope.WriteError($"{ex.Message}");
            return false;
        }
    }

    private async Task<bool> RestartIisAsync(string siteName, string appPoolName)
    {
        var args = $"-Command \"" +
                      $"Restart-WebAppPool -Name \'{appPoolName}\'; " +
                      $"Start-Website -Name \'{siteName}\'\"";
        return await _executor.RunPowerShellCommandAsync(args, $"Reiniciar {siteName}");
    }

    internal static bool CopyFabricanteConfigDll(InstallationPaths paths)
    {
        var sourceDir = paths.Fabricantes;
        var destDir = paths.WebAppUIFabricantes;

        if (!Directory.Exists(sourceDir))
        {
            UIScope.WriteMessage($"  [yellow][[AVISO]] Diretorio de origem nao encontrado: {MarkupHelper.Escape(sourceDir)}[/]");
            return false;
        }

        var dllFiles = Directory.GetFiles(sourceDir, "fabricante.Configuracao*.dll", SearchOption.TopDirectoryOnly);
        if (dllFiles.Length == 0)
            dllFiles = Directory.GetFiles(sourceDir, "*fabricante.Configuracao*", SearchOption.TopDirectoryOnly);

        if (dllFiles.Length == 0)
        {
            UIScope.WriteMessage($"  [yellow][[AVISO]] Nenhum arquivo \'fabricante.Configuracao\' encontrado em: {MarkupHelper.Escape(sourceDir)}[/]");
            return false;
        }

        Directory.CreateDirectory(destDir);
        var copiados = 0;

        foreach (var dllPath in dllFiles)
        {
            var fileName = Path.GetFileName(dllPath);
            var destPath = Path.Combine(destDir, fileName);

            try
            {
                File.Copy(dllPath, destPath, overwrite: true);
                UIScope.WriteMessage($"   [green][[OK]][/] Copiado: {MarkupHelper.Escape(fileName)}");
                copiados++;
            }
            catch (Exception ex)
            {
                UIScope.WriteMessage($"   [red][[ERRO]] Falha ao copiar {MarkupHelper.Escape(fileName)}: {MarkupHelper.Escape(ex.Message)}[/]");
            }
        }

        return copiados > 0;
    }
}