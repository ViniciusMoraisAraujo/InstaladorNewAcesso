using System.Text.Json;
using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Implementations;
using InstaladorNewAcesso.Core.Services;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Console.Services;

public class UnattendedRunner
{
    private readonly IUIService _ui;

    public UnattendedRunner(IUIService ui)
    {
        _ui = ui;
    }

    public async Task RunAsync(string configPath)
    {
        if (!File.Exists(configPath))
        {
            _ui.WriteError($"Arquivo de configuração Unattended não encontrado: {configPath}");
            return;
        }

        UnattendedConfig? config;
        try
        {
            var json = await File.ReadAllTextAsync(configPath);
            config = JsonSerializer.Deserialize<UnattendedConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _ui.WriteError($"Falha ao ler o arquivo de configuração JSON: {ex.Message}");
            return;
        }

        if (config == null)
        {
            _ui.WriteError("Configuração JSON vazia ou inválida.");
            return;
        }

        _ui.WriteMessage("[cyan]Iniciando modo autônomo (Unattended)...[/]");

        var paths = new InstallationPaths(config.BasePath);
        var rollback = new RollbackManager(_ui);

        try
        {

        // 1. Recursos do Windows
        if (config.InstallWindowsFeatures)
        {
            _ui.WriteMessage("\n[yellow]Instalando Recursos do Windows...[/]");
            var executor = new ProcessExecutorService();
            var wsInstaller = new WindowsServerInstaller(executor);
            var result = await wsInstaller.InstallAllAsync(CancellationToken.None);
            if (!result)
                _ui.WriteWarning("Alguns recursos do Windows falharam ao instalar.");
        }

        // 2. Diretórios
        if (config.CreateDirectories)
        {
            _ui.WriteMessage("\n[yellow]Criando Diretórios...[/]");
            Directory.CreateDirectory(paths.InstallationPath);
            Directory.CreateDirectory(paths.Controller);
            Directory.CreateDirectory(paths.WebApps);
            Directory.CreateDirectory(paths.ConnectionRecord);
            Directory.CreateDirectory(paths.TempPath);
            _ui.WriteMessage("[green]Diretórios criados.[/]");
        }

        // 3. IIS
        if (config.ConfigureIIS)
        {
            _ui.WriteMessage("\n[yellow]Configurando IIS base...[/]");
            var executor = new ProcessExecutorService();
            var iisInstaller = new InstaladorNewAcesso.Core.Utils.IisInstaller(executor);
            await iisInstaller.ConfigureBaseIisAsync(paths.InstallationPath);
        }

        // 4. MSIs
        if (config.MsisToInstall?.Count > 0)
        {
            _ui.WriteMessage("\n[yellow]Instalando MSIs...[/]");
            var scanner = new MsiScanner(paths, "SQLServer", config.InstallersPath);
            var todosMsi = scanner.Scan();

            var msiInstaller = new MsiInstaller();
            foreach (var msi in todosMsi)
            {
                var nome = Path.GetFileName(msi.MsiPath);
                if (config.MsisToInstall.Contains("*") || config.MsisToInstall.Contains(nome, StringComparer.OrdinalIgnoreCase))
                {
                    _ui.WriteMessage($"Instalando MSI: {nome}");
                    var installed = MsiUninstaller.IsInstalled(msi.TargetDirectory);
                    if (installed)
                    {
                        var backupPath = ConfigBackupService.Backup(msi.TargetDirectory, nome);
                        await MsiUninstaller.UninstallByMsiPathAsync(msi.MsiPath);
                        await msiInstaller.InstallAsync(msi, rollback);
                        ConfigBackupService.Restore(backupPath, msi.TargetDirectory);
                        ConfigBackupService.Cleanup(backupPath);
                    }
                    else
                    {
                        var ok = await msiInstaller.InstallAsync(msi, rollback);
                        if (!ok)
                        {
                            throw new InvalidOperationException($"Falha ao instalar o MSI {nome}.");
                        }
                    }
                }
            }
        }

        // 5. WebApps
        if (config.InstallWebApps)
        {
            _ui.WriteMessage("\n[yellow]Instalando WebApps...[/]");
            var webAppScanner = new WebAppScanner(paths, config.InstallersPath);
            var webApps = webAppScanner.Scan();
            
            var webAppInstaller = new WebAppInstaller(new ProcessExecutorService());
            foreach (var app in webApps)
            {
                _ui.WriteMessage($"Instalando WebApp: {app.Name}");
                var ok = await webAppInstaller.InstallAsync(app, paths, rollback);
                if (!ok)
                {
                    throw new InvalidOperationException($"Falha ao instalar o WebApp {app.Name}.");
                }
            }
        }

        // 6. Task Scheduler
        if (config.TaskScheduler?.Install == true)
        {
            _ui.WriteMessage("\n[yellow]Configurando Tarefas Agendadas...[/]");
            var taskInstaller = new WindowsTaskInstaller(new ProcessExecutorService());
            await taskInstaller.InstallTaskAsync(config.TaskScheduler.TaskName, config.TaskScheduler.ExecutablePath, config.TaskScheduler.IntervalMinutes);
        }

        _ui.WriteMessage("\n[green]Instalação autônoma concluída![/]");
        }
        catch (Exception ex)
        {
            _ui.WriteError($"Erro crítico durante instalação autônoma: {ex.Message}");
            await rollback.ExecuteRollbackAsync();
        }
    }
}
