using System.Diagnostics;
using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Core.Services;

public class WebAppInstaller
{
    private readonly IisInstaller _iisInstaller;
    private readonly IProcessExecutor _executor;

    public WebAppInstaller() : this(new ProcessExecutorService()) { }

    public WebAppInstaller(IProcessExecutor executor)
    {
        _executor = executor;
        _iisInstaller = new IisInstaller(executor);
    }

    private static int TotalSteps(string siteName) =>
        string.Equals(siteName, "WebAppUI", StringComparison.OrdinalIgnoreCase) ? 6 : 5;

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

    public async Task<bool> InstallAsync(WebAppModel model, InstallationPaths? paths = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        try
        {
            UIScope.WriteMessage($"\n [cyan]{MarkupHelper.Escape(model.SiteName)}[/] Iniciando instalação...");

            if (!await EnsureIisConfiguredAsync(model))
                return false;

            var installed = false;
            string? installedPath = null;
            var totalSteps = TotalSteps(model.SiteName);

            // Etapa 1: Instalação do MSI
            await (UIScope.Current?.ShowStatus("Instalando MSI...", async update =>
            {
                update("Executando msiexec...");
                installed = await InstallMsiSilentlyAsync(model);

                if (installed)
                {
                    for (var i = 0; i < 20; i++)
                    {
                        installedPath = LocateInstalledPath(model.ForcedInstallPath, model.SiteName);
                        if (installedPath != null) break;
                        if (i < 19) await Task.Delay(500);
                    }
                }
            }) ?? Task.CompletedTask);

            if (installed)
            {
                WriteStep($"{StepLabel(1, totalSteps)}Instalando MSI (normal)", true);
                if (installedPath != null)
                    UIScope.WriteMessage($"         Instalados em: [cyan]{MarkupHelper.Escape(installedPath)}[/]");
            }

            // Fallback: Admin Install
            if (!installed || installedPath == null)
            {
                if (!installed)
                    WriteStep($"{StepLabel(1, totalSteps)}Instalando MSI (normal)", false);

                if (!UIScope.Confirm("\n[yellow]Instalação normal falhou. Tentar Admin Install (msiexec /a)?[/]", false))
                {
                    UIScope.WriteMessage("[gray]Admin Install cancelado.[/]");
                    return false;
                }

                await (UIScope.Current?.ShowStatus("Extraindo via Admin Install...", async update =>
                {
                    update("Executando msiexec /a...");
                    installedPath = await AdminInstallAsync(model);
                }) ?? Task.CompletedTask);

                if (installedPath == null)
                {
                    WriteStep($"{StepLabel(1, totalSteps)}Extraindo MSI (Admin Install)", false);
                    return false;
                }

                WriteStep($"{StepLabel(1, totalSteps)}Extraindo MSI (Admin Install)", true);
                UIScope.WriteMessage($"         Extraído em: [cyan]{MarkupHelper.Escape(installedPath)}[/]");
            }

            // Etapa 2: Copiar/mover arquivos
            WriteStep($"{StepLabel(2, totalSteps)}Copiando para {MarkupHelper.Escape(model.TargetDirectory)}");
            var copied = false;
            var isFromForcedPath = string.Equals(installedPath, model.ForcedInstallPath, StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(installedPath, Path.Combine(model.ForcedInstallPath, model.SiteName), StringComparison.OrdinalIgnoreCase);

            await (UIScope.Current?.ShowStatus("Copiando arquivos...", async update =>
            {
                if (isFromForcedPath)
                    copied = await MoveToTargetAsync(installedPath!, model.TargetDirectory, model.SiteName);
                else
                {
                    copied = await CopyToTargetAsync(installedPath!, model.TargetDirectory);
                    try { if (Directory.Exists(installedPath)) Directory.Delete(installedPath, true); }
                    catch (Exception ex)
                    {
                        AuditLogger.Log("WebApp Cleanup", installedPath, false, ex.Message);
                    }
                }
            }) ?? Task.CompletedTask);

            if (!copied)
            {
                WriteStep("", false);
                return false;
            }
            WriteStep("", true);

            // Etapa 3: Atualizar IIS
            WriteStep($"{StepLabel(3, totalSteps)}Atualizando IIS physicalPath");
            var pathUpdated = await _iisInstaller.UpdateSitePhysicalPathAsync(model.SiteName, model.TargetDirectory);
            WriteStep("", pathUpdated);

            // Etapa 4: Reiniciar IIS
            WriteStep($"{StepLabel(4, totalSteps)}Reiniciando IIS");
            var restarted = await RestartIisAsync(model.SiteName, model.AppPoolName);
            WriteStep("", restarted);

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

            // Etapa 6 (WebAppUI apenas): DLL fabricante
            if (isUI)
            {
                WriteStep($"{StepLabel(6, totalSteps)}Copiando fabricante.Configuracao");
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

    private async Task<bool> EnsureIisConfiguredAsync(WebAppModel model)
    {
        UIScope.WriteMessage($" [cyan]Verificando IIS para:[/] {MarkupHelper.Escape(model.SiteName)}");

        // Batch check: AppPool + Site em um único comando PowerShell
        var poolNames = new[] { model.AppPoolName };
        var siteNames = new[] { model.SiteName };
        var (poolsStatus, sitesStatus) = await _iisInstaller.CheckAppPoolsAndSitesExistAsync(poolNames, siteNames);
        var appPoolExists = poolsStatus.GetValueOrDefault(model.AppPoolName, false);
        var siteExists = sitesStatus.GetValueOrDefault(model.SiteName, false);

        // AppPool
        UIScope.WriteMessage($"  Verificando AppPool '[yellow]{MarkupHelper.Escape(model.AppPoolName)}[/]'... ");
        if (!appPoolExists)
        {
            UIScope.WriteMessage("[blue][[CRIANDO]][/]");
            var created = await _iisInstaller.CreateApplicationPoolAsync(model.AppPoolName, "v4.0", "Integrated");
            if (!created)
            {
                UIScope.WriteMessage($"[red][[ERRO]] Falha ao criar AppPool '{MarkupHelper.Escape(model.AppPoolName)}'.[/]");
                return false;
            }
        }
        else
            UIScope.WriteMessage("[cyan]IGNORADO Já existe.[/]");

        // Site
        UIScope.WriteMessage($"  Verificando Site '[yellow]{MarkupHelper.Escape(model.SiteName)}[/]'... ");
        if (!siteExists)
        {
            UIScope.WriteMessage("[blue][[CRIANDO]][/]");
            var created = await _iisInstaller.CreateSiteAsync(model.SiteName, model.AppPoolName, model.ForcedInstallPath, model.Port);
            if (!created)
            {
                UIScope.WriteMessage($"[red][[ERRO]] Falha ao criar Site '{MarkupHelper.Escape(model.SiteName)}'.[/]");
                return false;
            }
        }
        else
        {
            UIScope.WriteMessage("[cyan]IGNORADO[/] Atualizando physicalPath...");
            await _iisInstaller.UpdateSitePhysicalPathAsync(model.SiteName, model.ForcedInstallPath);
        }

        return true;
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

    internal static bool HasDeployableFiles(string directory, SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        if (!Directory.Exists(directory))
            return false;

        return Directory.GetFiles(directory, "*.dll", searchOption).Length > 0 ||
               Directory.GetFiles(directory, "*.aspx", searchOption).Length > 0 ||
               Directory.GetFiles(directory, "*.config", searchOption).Length > 0;
    }

    private static async Task<bool> InstallMsiSilentlyAsync(WebAppModel model)
    {
        try
        {
            var logArg = "";
            string? logPath = null;
            if (model.GenerateLog)
            {
                logPath = MsiLogHelper.GenerateLogFilePath(model.MsiPath);
                logArg = $" /lvx* \"{logPath}\"";
            }

            var args = $"/i \"{model.MsiPath}\" /qn{logArg}";
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

            UIScope.WriteMessage($"   [gray][[DEBUG]] ExitCode: {process.ExitCode}[/]");

            if (process.ExitCode != 0 && model.GenerateLog && logPath != null)
                MsiLogHelper.DisplayLogAnalysis(logPath);

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            UIScope.WriteError($"{ex.Message}");
            return false;
        }
    }

    private static async Task<string?> AdminInstallAsync(WebAppModel model)
    {
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "InstaladorNewAcesso", "AdminInstall", model.SiteName);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            var logArg = "";
            string? logPath = null;
            if (model.GenerateLog)
            {
                logPath = MsiLogHelper.GenerateLogFilePath(model.MsiPath + "_admin");
                logArg = $" /lvx* \"{logPath}\"";
            }

            var args = $"/a \"{model.MsiPath}\" TARGETDIR=\"{tempDir}\" /qb{logArg}";
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
                UIScope.WriteMessage($"   [red][[ERRO]] Nenhum arquivo extraído para: {MarkupHelper.Escape(tempDir)}[/]");
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

    private async Task<bool> MoveToTargetAsync(string sourceDir, string targetDir, string siteName)
    {
        try
        {
            Directory.CreateDirectory(targetDir);

            var stopArgs = $"-Command \"Stop-Website -Name '{siteName}' -ErrorAction SilentlyContinue\"";
            await _executor.RunPowerShellCommandAsync(stopArgs, $"Parar {siteName}");

            var robocopyArgs = $"\"{sourceDir}\" \"{targetDir}\" /E /MOVE /R:3 /W:5";
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

            if (Directory.Exists(sourceDir))
            {
                try { Directory.Delete(sourceDir, true); }
                catch (Exception ex)
                {
                    AuditLogger.Log("WebApp MoveToTarget Cleanup", sourceDir, false, ex.Message);
                }
            }

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
                      $"Restart-WebAppPool -Name '{appPoolName}'; " +
                      $"Start-Website -Name '{siteName}'\"";
        return await _executor.RunPowerShellCommandAsync(args, $"Reiniciar {siteName}");
    }

    internal static bool CopyFabricanteConfigDll(InstallationPaths paths)
    {
        var sourceDir = paths.Fabricantes;
        var destDir = paths.WebAppUIFabricantes;

        if (!Directory.Exists(sourceDir))
        {
            UIScope.WriteMessage($"  [yellow][[AVISO]] Diretório de origem não encontrado: {MarkupHelper.Escape(sourceDir)}[/]");
            return false;
        }

        var dllFiles = Directory.GetFiles(sourceDir, "fabricante.Configuracao*.dll", SearchOption.TopDirectoryOnly);
        if (dllFiles.Length == 0)
            dllFiles = Directory.GetFiles(sourceDir, "*fabricante.Configuracao*", SearchOption.TopDirectoryOnly);

        if (dllFiles.Length == 0)
        {
            UIScope.WriteMessage($"  [yellow][[AVISO]] Nenhum arquivo 'fabricante.Configuracao' encontrado em: {MarkupHelper.Escape(sourceDir)}[/]");
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
