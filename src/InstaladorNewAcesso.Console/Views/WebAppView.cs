using System.Globalization;
using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Services;
using InstaladorNewAcesso.Core.Utils;
using Spectre.Console;

namespace InstaladorNewAcesso.Console.Views;

public class WebAppView
{
    private readonly IUIService _ui;
    private readonly WebAppInstaller _installer = new();
    private readonly MsiUninstaller _msiUninstaller = new();
    private readonly SummaryPanelView _summaryView;
    private readonly ViewHelper _viewHelper;
    private List<WebAppModel> _webApps = new();

    public WebAppView(IUIService ui)
    {
        _ui = ui;
        _summaryView = new SummaryPanelView(ui);
        _viewHelper = new ViewHelper(ui);
    }

    public async Task ExecuteAsync(InstallationPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _ui.WriteRule("INSTALAÇÃO DE WEB APPS", "cyan");
        _ui.WriteEmptyLine();

        // Auto-detectar versões disponíveis no diretório de instaladores
        var msiRoot = _viewHelper.ResolveInstallerPath(paths.InstallationPath, "instaladores WEBApp");

        if (!Directory.Exists(msiRoot))
        {
            _ui.WriteError($"Diretório não encontrado: {msiRoot.EscapeMarkup()}");
            _ui.WaitForEnter();
            return;
        }

        var dbChoice = _ui.AskChoice(
            "[bold yellow]Banco de dados:[/]",
            ["SQLServer", "Oracle"]);

        // =========================================
        // FASE 1: SCAN - Escanear Web Apps
        // =========================================
        _ui.WriteEmptyLine();
        _ui.WriteRule("FASE 1: Escaneando Web Apps", "yellow");
        _ui.WriteEmptyLine();

        var scanner = new WebAppScanner(paths, dbChoice, msiRoot);
        await _ui.ShowStatus("Escaneando Web Apps...", async update =>
        {
            _webApps = scanner.Scan();
        });

        _ui.WriteMessage($" [green][[{_webApps.Count} ENCONTRADOS]][/]");

        if (_webApps.Count == 0)
        {
            _ui.WriteMessage("\n[yellow]Nenhum Web App encontrado (WebAppUI/WebAppDS).[/]");
        }
        else
        {
            // Exibir tabela de Web Apps encontrados com status de instalação
            var headers = new[] { "#", "MSI", "Site", "Porta", "Destino", "Instalado?" };
            var rows = new List<string[]>();
            var instaladosCount = 0;

            for (var i = 0; i < _webApps.Count; i++)
            {
                var app = _webApps[i];
                var nome = Path.GetFileName(app.MsiPath) ?? "";
                var installed = MsiUninstaller.IsInstalled(app.TargetDirectory);
                if (installed) instaladosCount++;
                rows.Add([
                    $"{i + 1}",
                    nome,
                    app.SiteName,
                    app.Port.ToString(CultureInfo.InvariantCulture),
                    app.TargetDirectory,
                    installed ? "[green]Sim[/]" : "[gray]Não[/]"
                ]);
            }

            _ui.WriteTable(headers, rows);
            _ui.WriteMessage($"\n[cyan]{instaladosCount}/{_webApps.Count} Web App(s) já instalado(s).[/]");
            _ui.WriteEmptyLine();

            var generateLog = _ui.Confirm("\n[bold yellow]Gerar log verbose da instalação para diagnóstico?[/]", false);
            if (generateLog)
            {
                _ui.WriteMessage($" [gray]Os logs serão salvos em: {MsiLogHelper.GetLogDirectory().EscapeMarkup()}[/]");
            }

            foreach (var app in _webApps)
                app.GenerateLog = generateLog;

            // =========================================
            // FASE 2: INSTALAÇÃO
            // =========================================
            _ui.WriteEmptyLine();
            _ui.WriteRule("FASE 2: Instalando Web Apps", "yellow");
            _ui.WriteEmptyLine();

            var opcao = _viewHelper.PromptSelection();

            switch (opcao)
            {
                case "T":
                    await InstallSelectedAsync(_webApps, paths);
                    break;
                case "S":
                    var indices = _viewHelper.AskIndices("Digite os números dos Web Apps", _webApps.Count);
                    if (indices.Count > 0)
                    {
                        var selecionados = indices.Select(i => _webApps[i]).ToList();
                        await InstallSelectedAsync(selecionados, paths);
                    }
                    else
                    {
                        _ui.WriteWarning("Nenhum índice válido.");
                    }
                    break;
                case "I":
                    var reinstalar = _webApps.Where(a => MsiUninstaller.IsInstalled(a.TargetDirectory)).ToList();
                    if (reinstalar.Count == 0)
                    {
                        _ui.WriteWarning("Nenhum Web App já instalado encontrado.");
                    }
                    else
                    {
                        await InstallSelectedAsync(reinstalar, paths);
                    }
                    break;
                case "N":
                    _ui.WriteMessage("[gray]Instalação cancelada.[/]");
                    break;
            }
        }

        _ui.WriteEmptyLine();
        _ui.WriteRule("Fim da etapa de Instalação de Web Apps", "cyan");
        _ui.WaitForEnter();
    }

    private async Task InstallSelectedAsync(List<WebAppModel> lista, InstallationPaths paths)
    {
        var resultadosDaEtapa = new List<SummaryResult>();
        var sucessos = 0;

        for (var i = 0; i < lista.Count; i++)
        {
            var app = lista[i];
            var nome = Path.GetFileName(app.MsiPath) ?? app.SiteName;
            _ui.WriteMessage($"\n [cyan]{app.SiteName.EscapeMarkup()}[/] ({i + 1}/{lista.Count}) ");

            var installed = MsiUninstaller.IsInstalled(app.TargetDirectory);
            string? backupPath = null;

            // Se já instalado: backup web.config
            if (installed)
            {
                _ui.WriteMessage("\n   [cyan]Web App já instalado. Realizando backup e reinstalação...[/]");
                backupPath = ConfigBackupService.Backup(app.TargetDirectory, app.SiteName);
            }

            var ok = await _installer.InstallAsync(app, paths);

            // Restaurar configs se havia backup
            if (backupPath != null)
            {
                if (ok)
                {
                    ConfigBackupService.Restore(backupPath, app.TargetDirectory);
                }
                else
                {
                    _ui.WriteWarning($"   Instalação falhou, restaurando backup em: {app.TargetDirectory.EscapeMarkup()}");
                    ConfigBackupService.Restore(backupPath, app.TargetDirectory);
                }
                ConfigBackupService.Cleanup(backupPath);
            }

            if (ok)
            {
                _ui.WriteMessage("[green][[OK]][/]");
                resultadosDaEtapa.Add(SummaryStore.Add("WebApps", $"{app.SiteName} (porta {app.Port})", true, $"Instalado em {app.TargetDirectory}"));
                sucessos++;
            }
            else
            {
                _ui.WriteMessage("[red][[FALHA]][/]");
                resultadosDaEtapa.Add(SummaryStore.Add("WebApps", $"{app.SiteName} (porta {app.Port})", false, "Falha na instalação"));
            }
        }

        _ui.WriteEmptyLine();
        _summaryView.ExibirEtapa("WebApps", "🌍", resultadosDaEtapa);
        _ui.WriteMessage($"\n[cyan]Instalação concluída. {sucessos}/{lista.Count} Web Apps instalados com sucesso.[/]");
    }
}
