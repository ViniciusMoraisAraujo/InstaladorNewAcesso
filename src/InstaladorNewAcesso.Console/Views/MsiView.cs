using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Services;
using InstaladorNewAcesso.Core.Utils;
using Spectre.Console;

namespace InstaladorNewAcesso.Console.Views;

public class MsiView
{
    private readonly IUIService _ui;
    private readonly MsiInstaller _installer = new();
    private readonly MsiUninstaller _msiUninstaller = new();
    private readonly SummaryPanelView _summaryView;
    private readonly ViewHelper _viewHelper;
    private List<MsiInstallationModel> _todosMsi = new();
    private readonly List<MsiInstallationModel> _outros = new();
    private readonly List<MsiInstallationModel> _fabricantes = new();

    public MsiView(IUIService ui)
    {
        _ui = ui;
        _summaryView = new SummaryPanelView(ui);
        _viewHelper = new ViewHelper(ui);
    }

    public async Task ExecuteAsync(InstallationPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _outros.Clear();
        _fabricantes.Clear();

        _ui.WriteRule("INSTALAÇÃO DE APLICAÇÕES (MSIs)", "cyan");
        _ui.WriteEmptyLine();

        // Auto-detectar versões disponíveis no diretório de instaladores
        var msiRoot = _viewHelper.ResolveInstallerPath(paths.InstallationPath, "instaladores MSI");

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
        // FASE 1: SCAN - Escanear MSIs
        // =========================================
        _ui.WriteEmptyLine();
        _ui.WriteRule("FASE 1: Escaneando MSIs", "yellow");
        _ui.WriteEmptyLine();

        var scanner = new MsiScanner(paths, dbChoice, msiRoot);
        await _ui.ShowStatus("Escaneando MSIs...", async update =>
        {
            try
            {
                _todosMsi = scanner.Scan();
                _ui.WriteMessage($" [green][[{_todosMsi.Count} ENCONTRADOS]][/]");
            }
            catch (Exception ex)
            {
                _ui.WriteError(ex.Message.EscapeMarkup());
                _ui.WaitForEnter();
                return;
            }
        });

        if (_todosMsi.Count == 0)
        {
            _ui.WriteMessage("\n[yellow]Nenhum MSI regular encontrado (WebApps são ignorados).[/]");
        }
        else
        {
            // Exibir tabela resumo dos MSIs encontrados
            var headers = new[] { "#", "MSI", "Destino", "Instalado?" };
            var rows = new List<string[]>();
            var instaladosCount = 0;

            for (var i = 0; i < Math.Min(_todosMsi.Count, 20); i++)
            {
                var nome = Path.GetFileName(_todosMsi[i].MsiPath) ?? "";
                var installed = MsiUninstaller.IsInstalled(_todosMsi[i].TargetDirectory);
                if (installed) instaladosCount++;
                rows.Add([
                    $"{i + 1}",
                    nome,
                    _todosMsi[i].TargetDirectory,
                    installed ? "[green]Sim[/]" : "[gray]Não[/]"
                ]);
            }

            if (_todosMsi.Count > 20)
            {
                rows.Add(["...", $"[gray]e mais {_todosMsi.Count - 20} MSI(s)[/]", "", ""]);
            }

            _ui.WriteTable(headers, rows);
            _ui.WriteMessage($"\n[cyan]{instaladosCount}/{_todosMsi.Count} MSI(s) já instalado(s).[/]");
            _ui.WriteEmptyLine();

            var generateLog = _ui.Confirm("\n[bold yellow]Gerar log verbose da instalação para diagnóstico?[/]", false);
            if (generateLog)
            {
                _ui.WriteMessage($" [gray]Os logs serão salvos em: {MsiLogHelper.GetLogDirectory().EscapeMarkup()}[/]");
            }

            foreach (var msi in _todosMsi)
                msi.GenerateLog = generateLog;

            SepararMsIs(paths);

            // =========================================
            // FASE 2: INSTALAÇÃO
            // =========================================
            _ui.WriteEmptyLine();
            _ui.WriteRule("FASE 2: Instalando MSIs", "yellow");
            _ui.WriteEmptyLine();

            if (_outros.Count > 0)
            {
                await TelaOutrosAsync();
            }

            if (_fabricantes.Count > 0)
            {
                await TelaFabricantesAsync();
            }
            else if (_outros.Count == 0)
            {
                _ui.WriteMessage("\n[yellow]Nenhum MSI encontrado.[/]");
            }
        }

        _ui.WriteEmptyLine();
        _ui.WriteRule("Fim da etapa de Instalação de MSIs", "cyan");
        _ui.WaitForEnter();
    }

    private void SepararMsIs(InstallationPaths paths)
    {
        var fabricantesPath = Path.Combine(paths.Controller, "Fabricantes");
        foreach (var msi in _todosMsi)
        {
            if (msi.TargetDirectory.StartsWith(fabricantesPath, StringComparison.OrdinalIgnoreCase))
                _fabricantes.Add(msi);
            else
                _outros.Add(msi);
        }
    }

    private async Task TelaOutrosAsync()
    {
        _ui.Clear();
        _ui.WriteRule("APLICAÇÕES GERAIS (MSIs)", "cyan");
        _ui.WriteEmptyLine();

        // Tabela
        var headers = new[] { "#", "MSI", "Destino", "Instalado?" };
        var rows = new List<string[]>();

        for (var i = 0; i < _outros.Count; i++)
        {
            var nome = Path.GetFileName(_outros[i].MsiPath) ?? "";
            var installed = MsiUninstaller.IsInstalled(_outros[i].TargetDirectory);
            rows.Add([
                $"{i + 1}",
                nome,
                _outros[i].TargetDirectory,
                installed ? "[green]Sim[/]" : "[gray]Não[/]"
            ]);
        }

        _ui.WriteTable(headers, rows);
        _ui.WriteEmptyLine();

        var opcao = _viewHelper.PromptSelection();

        switch (opcao)
        {
            case "T":
                await InstalarListaAsync(_outros, "MSIs Gerais");
                break;
            case "S":
                var indices = _viewHelper.AskIndices("Digite os números", _outros.Count);
                if (indices.Count > 0)
                {
                    var selecionados = indices.Select(i => _outros[i]).ToList();
                    await InstalarListaAsync(selecionados, "MSIs Gerais");
                }
                else
                {
                    _ui.WriteWarning("Nenhum índice válido.");
                }
                break;
            case "I":
                var reinstalar = _outros.Where(m => MsiUninstaller.IsInstalled(m.TargetDirectory)).ToList();
                if (reinstalar.Count == 0)
                {
                    _ui.WriteWarning("Nenhum MSI já instalado encontrado.");
                }
                else
                {
                    await InstalarListaAsync(reinstalar, "MSIs Gerais (reinstalar)");
                }
                break;
            case "N":
                _ui.WriteMessage("[gray]MSIs gerais ignorados.[/]");
                break;
        }
    }

    private async Task TelaFabricantesAsync()
    {
        _ui.Clear();
        _ui.WriteRule("FABRICANTES DISPONÍVEIS", "cyan");
        _ui.WriteEmptyLine();

        // Tabela de fabricantes
        var headers = new[] { "#", "MSI", "Destino", "Instalado?" };
        var rows = new List<string[]>();

        for (var i = 0; i < _fabricantes.Count; i++)
        {
            var nome = Path.GetFileName(_fabricantes[i].MsiPath) ?? "";
            var installed = MsiUninstaller.IsInstalled(_fabricantes[i].TargetDirectory);
            rows.Add([
                $"{i + 1}",
                nome,
                _fabricantes[i].TargetDirectory,
                installed ? "[green]Sim[/]" : "[gray]Não[/]"
            ]);
        }

        _ui.WriteTable(headers, rows);
        _ui.WriteEmptyLine();

        var opcao = _viewHelper.PromptSelection();

        switch (opcao)
        {
            case "T":
                await InstalarListaAsync(_fabricantes, "Fabricantes");
                break;
            case "S":
                var indices = _viewHelper.AskIndices("Digite os números dos fabricantes", _fabricantes.Count);
                if (indices.Count > 0)
                {
                    var selecionados = indices.Select(i => _fabricantes[i]).ToList();
                    await InstalarListaAsync(selecionados, "Fabricantes");
                }
                else
                {
                    _ui.WriteWarning("Nenhum índice válido.");
                }
                break;
            case "I":
                var reinstalar = _fabricantes.Where(m => MsiUninstaller.IsInstalled(m.TargetDirectory)).ToList();
                if (reinstalar.Count == 0)
                {
                    _ui.WriteWarning("Nenhum fabricante já instalado encontrado.");
                }
                else
                {
                    await InstalarListaAsync(reinstalar, "Fabricantes (reinstalar)");
                }
                break;
            case "N":
                _ui.WriteMessage("[gray]Fabricantes ignorados.[/]");
                break;
        }
    }

    private async Task InstalarListaAsync(List<MsiInstallationModel> lista, string grupo)
    {
        _ui.WriteMessage($"\n[cyan]Iniciando instalação de {lista.Count} MSI(s) - {grupo.EscapeMarkup()}[/]\n");

        var resultadosDaEtapa = new List<SummaryResult>();
        var sucessos = 0;

        await _ui.ShowProgress($"Instalando {grupo}...", async update =>
        {
            for (var i = 0; i < lista.Count; i++)
            {
                var model = lista[i];
                var nome = Path.GetFileName(model.MsiPath) ?? "";
                update((double)i / lista.Count * 100, $"Instalando: {nome}");

                var installed = MsiUninstaller.IsInstalled(model.TargetDirectory);
                string? backupPath = null;

                // Se já instalado: backup → desinstalar
                if (installed)
                {
                    _ui.WriteMessage($"   [cyan]{nome.EscapeMarkup()}[/] já instalado. Realizando backup e reinstalação...");

                    backupPath = ConfigBackupService.Backup(model.TargetDirectory, nome);

                    var uninstalled = await MsiUninstaller.UninstallByMsiPathAsync(model.MsiPath);
                    if (!uninstalled)
                    {
                        _ui.WriteWarning("   msiexec /x não concluiu, removendo diretório...");
                        MsiUninstaller.RemoveTargetDirectory(model.TargetDirectory);
                    }
                }

                // Instalar
                var ok = await _installer.InstallAsync(model);

                // Se foi reinstalação: restaurar configs
                if (ok && backupPath != null)
                {
                    ConfigBackupService.Restore(backupPath, model.TargetDirectory);
                    ConfigBackupService.Cleanup(backupPath);
                }
                else if (!ok && backupPath != null)
                {
                    // Se a instalação falhou, ainda assim restaura o backup
                    _ui.WriteWarning($"   Instalação falhou, restaurando backup em: {model.TargetDirectory.EscapeMarkup()}");
                    ConfigBackupService.Restore(backupPath, model.TargetDirectory);
                    ConfigBackupService.Cleanup(backupPath);
                }

                if (ok)
                {
                    _ui.WriteMessage($"   [green][[SUCESSO]] {nome.EscapeMarkup()}[/]");
                    resultadosDaEtapa.Add(SummaryStore.Add("Aplicações (MSIs)", nome, true));
                    sucessos++;
                }
                else
                {
                    _ui.WriteMessage($"   [red][[FALHA]] {nome.EscapeMarkup()}[/]");
                    resultadosDaEtapa.Add(SummaryStore.Add("Aplicações (MSIs)", nome, false, "Falha na instalação"));
                }
            }
        });

        _ui.WriteEmptyLine();
        _summaryView.ExibirEtapa("Aplicações (MSIs)", "📦", resultadosDaEtapa);
        _ui.WriteMessage($"\n[cyan]Instalação concluída. {sucessos}/{lista.Count} MSIs instalados com sucesso.[/]");
    }
}
