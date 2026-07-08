using System.Diagnostics;
using InstaladorNewAcesso.Interfaces;
using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Utils;
using Spectre.Console;

namespace InstaladorNewAcesso.Services;

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

    private int TotalSteps(string siteName) =>
        string.Equals(siteName, "WebAppUI", StringComparison.OrdinalIgnoreCase) ? 6 : 5;

    private string StepLabel(int current, int total) => $"  [{current}/{total}] ";

    private void WriteStep(string label, bool? success = null)
    {
        if (success == true)
            AnsiConsole.MarkupLine($"{label}[green][OK][/]");
        else if (success == false)
            AnsiConsole.MarkupLine($"{label}[red][FALHA][/]");
        else
            AnsiConsole.Markup(label);
    }

    public async Task<bool> InstallAsync(WebAppModel model, InstallationPaths? paths = null)
    {
        try
        {
            AnsiConsole.MarkupLine($"\n [cyan]{model.SiteName.EscapeMarkup()}[/] Iniciando instalação...\n");

            if (!await EnsureIisConfiguredAsync(model))
                return false;

            bool installed = false;
            string? installedPath = null;
            var totalSteps = TotalSteps(model.SiteName);

            // Etapa 1: Instalação do MSI
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Instalando MSI...", async ctx =>
                {
                    ctx.Status = "Executando msiexec...";
                    installed = await InstallMsiSilentlyAsync(model);

                    if (installed)
                    {
                        await Task.Delay(2000);
                        installedPath = LocateInstalledPath(model.ForcedInstallPath, model.SiteName);
                    }
                });

            if (installed)
            {
                WriteStep($"{StepLabel(1, totalSteps)}Instalando MSI (normal)", true);
                if (installedPath != null)
                    AnsiConsole.MarkupLine($"         Instalados em: [cyan]{installedPath.EscapeMarkup()}[/]");
            }

            // Fallback: Admin Install
            if (!installed || installedPath == null)
            {
                if (!installed)
                    WriteStep($"{StepLabel(1, totalSteps)}Instalando MSI (normal)", false);

                if (!AnsiConsole.Confirm("\n[yellow]Instalação normal falhou. Tentar Admin Install (msiexec /a)?[/]", false))
                {
                    AnsiConsole.MarkupLine("[gray]Admin Install cancelado.[/]");
                    return false;
                }

                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Extraindo via Admin Install...", async ctx =>
                    {
                        ctx.Status = "Executando msiexec /a...";
                        installedPath = await AdminInstallAsync(model);
                    });

                if (installedPath == null)
                {
                    WriteStep($"{StepLabel(1, totalSteps)}Extraindo MSI (Admin Install)", false);
                    return false;
                }

                WriteStep($"{StepLabel(1, totalSteps)}Extraindo MSI (Admin Install)", true);
                AnsiConsole.MarkupLine($"         Extraído em: [cyan]{installedPath.EscapeMarkup()}[/]");
            }

            // Etapa 2: Copiar/mover arquivos
            WriteStep($"{StepLabel(2, totalSteps)}Copiando para {model.TargetDirectory.EscapeMarkup()}");
            bool copied = false;
            bool isFromForcedPath = string.Equals(installedPath, model.ForcedInstallPath, StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(installedPath, Path.Combine(model.ForcedInstallPath, model.SiteName), StringComparison.OrdinalIgnoreCase);

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Copiando arquivos...", async ctx =>
                {
                    if (isFromForcedPath)
                        copied = await MoveToTargetAsync(installedPath!, model.TargetDirectory, model.SiteName);
                    else
                    {
                        copied = await CopyToTargetAsync(installedPath!, model.TargetDirectory);
                        try { if (Directory.Exists(installedPath)) Directory.Delete(installedPath, true); }
                        catch { }
                    }
                });

            if (!copied)
            {
                WriteStep("", false);
                return false;
            }
            WriteStep("", true);

            // Etapa 3: Atualizar IIS
            WriteStep($"{StepLabel(3, totalSteps)}Atualizando IIS physicalPath");
            bool pathUpdated = await _iisInstaller.UpdateSitePhysicalPathAsync(model.SiteName, model.TargetDirectory);
            WriteStep("", pathUpdated);

            // Etapa 4: Reiniciar IIS
            WriteStep($"{StepLabel(4, totalSteps)}Reiniciando IIS");
            bool restarted = await RestartIisAsync(model.SiteName, model.AppPoolName);
            WriteStep("", restarted);

            // Etapa 5: Configurar web.config
            var isUI = string.Equals(model.SiteName, "WebAppUI", StringComparison.OrdinalIgnoreCase);

            if (isUI)
            {
                WriteStep($"{StepLabel(5, totalSteps)}Configurando web.config (WebAppUI)");
                bool webConfigOk = WebAppConfigHelper.UpdateWebAppUIConfig(model.TargetDirectory);
                WriteStep("", webConfigOk);
            }
            else
            {
                WriteStep($"{StepLabel(5, totalSteps)}Configurando web.config (WebAppDS)");
                bool webConfigOk = WebAppConfigHelper.UpdateWebAppDSConfig(model.TargetDirectory);
                WriteStep("", webConfigOk);
            }

            // Etapa 6 (WebAppUI apenas): DLL fabricante
            if (isUI)
            {
                WriteStep($"{StepLabel(6, totalSteps)}Copiando fabricante.Configuracao");
                bool dllCopied = paths != null && CopyFabricanteConfigDll(paths);
                WriteStep("", dllCopied);
            }

            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"\n[red][ERRO] {ex.Message.EscapeMarkup()}[/]");
            return false;
        }
    }

    private async Task<bool> EnsureIisConfiguredAsync(WebAppModel model)
    {
        AnsiConsole.MarkupLine($" [cyan]Verificando IIS para:[/] {model.SiteName.EscapeMarkup()}");

        // AppPool
        AnsiConsole.Markup($"  Verificando AppPool '[yellow]{model.AppPoolName.EscapeMarkup()}[/]'... ");
        if (!await _iisInstaller.AppPoolExistsAsync(model.AppPoolName))
        {
            AnsiConsole.MarkupLine("[blue][CRIANDO][/]");
            bool created = await _iisInstaller.CreateApplicationPoolAsync(model.AppPoolName, "v4.0", "Integrated");
            if (!created)
            {
                AnsiConsole.MarkupLine($"[red][ERRO] Falha ao criar AppPool '{model.AppPoolName.EscapeMarkup()}'.[/]");
                return false;
            }
        }
        else
            AnsiConsole.MarkupLine("[cyan][IGNORADO] Já existe.[/]");

        // Site
        AnsiConsole.Markup($"  Verificando Site '[yellow]{model.SiteName.EscapeMarkup()}[/]'... ");
        if (!await _iisInstaller.SiteExistsAsync(model.SiteName))
        {
            AnsiConsole.MarkupLine("[blue][CRIANDO][/]");
            bool created = await _iisInstaller.CreateSiteAsync(model.SiteName, model.AppPoolName, model.ForcedInstallPath, model.Port);
            if (!created)
            {
                AnsiConsole.MarkupLine($"[red][ERRO] Falha ao criar Site '{model.SiteName.EscapeMarkup()}'.[/]");
                return false;
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[cyan][IGNORADO][/] Atualizando physicalPath...");
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
               Directory.GetFiles(directory, "*.config", searchOption).Length > 0 ||
               Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly).Length >= 3;
    }

    private async Task<bool> InstallMsiSilentlyAsync(WebAppModel model)
    {
        try
        {
            string logArg = "";
            string? logPath = null;
            if (model.GenerateLog)
            {
                logPath = MsiLogHelper.GenerateLogFilePath(model.MsiPath);
                logArg = $" /lvx* \"{logPath}\"";
            }

            string args = $"/i \"{model.MsiPath}\" /qn{logArg}";
            AnsiConsole.MarkupLine($"   [gray][DEBUG] msiexec.exe {args.EscapeMarkup()}[/]");

            var process = new Process
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

            AnsiConsole.MarkupLine($"   [gray][DEBUG] ExitCode: {process.ExitCode}[/]");

            if (process.ExitCode != 0 && model.GenerateLog && logPath != null)
                MsiLogHelper.DisplayLogAnalysis(logPath);

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"   [red][ERRO] {ex.Message.EscapeMarkup()}[/]");
            return false;
        }
    }

    private async Task<string?> AdminInstallAsync(WebAppModel model)
    {
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "InstaladorNewAcesso", "AdminInstall", model.SiteName);
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            string logArg = "";
            string? logPath = null;
            if (model.GenerateLog)
            {
                logPath = MsiLogHelper.GenerateLogFilePath(model.MsiPath + "_admin");
                logArg = $" /lvx* \"{logPath}\"";
            }

            string args = $"/a \"{model.MsiPath}\" TARGETDIR=\"{tempDir}\" /qb{logArg}";
            AnsiConsole.MarkupLine($"   [gray][DEBUG] msiexec.exe {args.EscapeMarkup()}[/]");

            var process = new Process
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

            AnsiConsole.MarkupLine($"   [gray][DEBUG] Admin Install ExitCode: {process.ExitCode}[/]");

            if (process.ExitCode != 0)
            {
                if (model.GenerateLog && logPath != null)
                    MsiLogHelper.DisplayLogAnalysis(logPath);
                return null;
            }

            var files = Directory.GetFiles(tempDir, "*.*", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                AnsiConsole.MarkupLine($"   [red][ERRO] Nenhum arquivo extraído para: {tempDir.EscapeMarkup()}[/]");
                return null;
            }

            return tempDir;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"   [red][ERRO] Admin Install: {ex.Message.EscapeMarkup()}[/]");
            return null;
        }
    }

    private async Task<bool> CopyToTargetAsync(string sourceDir, string targetDir)
    {
        try
        {
            Directory.CreateDirectory(targetDir);
            string robocopyArgs = $"\"{sourceDir}\" \"{targetDir}\" /E /R:3 /W:5";
            AnsiConsole.MarkupLine($"   [gray][DEBUG] robocopy.exe {robocopyArgs.EscapeMarkup()}[/]");

            var process = new Process
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
            AnsiConsole.MarkupLine($"   [gray][DEBUG] Robocopy ExitCode: {process.ExitCode}[/]");

            return process.ExitCode >= 0 && process.ExitCode <= 7;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"   [red][ERRO] {ex.Message.EscapeMarkup()}[/]");
            return false;
        }
    }

    private async Task<bool> MoveToTargetAsync(string sourceDir, string targetDir, string siteName)
    {
        try
        {
            Directory.CreateDirectory(targetDir);

            string stopArgs = $"-Command \"Stop-Website -Name '{siteName}' -ErrorAction SilentlyContinue\"";
            await _executor.RunPowerShellCommandAsync(stopArgs, $"Parar {siteName}");

            string robocopyArgs = $"\"{sourceDir}\" \"{targetDir}\" /E /MOVE /R:3 /W:5";
            AnsiConsole.MarkupLine($"   [gray][DEBUG] robocopy.exe {robocopyArgs.EscapeMarkup()}[/]");

            var process = new Process
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
            AnsiConsole.MarkupLine($"   [gray][DEBUG] Robocopy ExitCode: {process.ExitCode}[/]");

            if (Directory.Exists(sourceDir))
            {
                try { Directory.Delete(sourceDir, true); }
                catch { }
            }

            return process.ExitCode >= 0 && process.ExitCode <= 7;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"   [red][ERRO] {ex.Message.EscapeMarkup()}[/]");
            return false;
        }
    }

    private async Task<bool> RestartIisAsync(string siteName, string appPoolName)
    {
        string args = $"-Command \"" +
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
            AnsiConsole.MarkupLine($"  [yellow][AVISO] Diretório de origem não encontrado: {sourceDir.EscapeMarkup()}[/]");
            return false;
        }

        var dllFiles = Directory.GetFiles(sourceDir, "fabricante.Configuracao*.dll", SearchOption.TopDirectoryOnly);
        if (dllFiles.Length == 0)
            dllFiles = Directory.GetFiles(sourceDir, "*fabricante.Configuracao*", SearchOption.TopDirectoryOnly);

        if (dllFiles.Length == 0)
        {
            AnsiConsole.MarkupLine($"  [yellow][AVISO] Nenhum arquivo 'fabricante.Configuracao' encontrado em: {sourceDir.EscapeMarkup()}[/]");
            return false;
        }

        Directory.CreateDirectory(destDir);
        int copiados = 0;

        foreach (var dllPath in dllFiles)
        {
            var fileName = Path.GetFileName(dllPath);
            var destPath = Path.Combine(destDir, fileName);

            try
            {
                File.Copy(dllPath, destPath, overwrite: true);
                AnsiConsole.MarkupLine($"   [green][OK][/] Copiado: {fileName.EscapeMarkup()}");
                copiados++;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"   [red][ERRO] Falha ao copiar {fileName.EscapeMarkup()}: {ex.Message.EscapeMarkup()}[/]");
            }
        }

        return copiados > 0;
    }
}
