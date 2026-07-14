using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Console.Views;

public class MainMenuView
{
    private readonly IUIService _ui;
    private readonly SummaryPanelView _summaryView;
    private readonly DownloadView _downloadView;
    private readonly ResourceView _resourceView;
    private readonly DirectoryView _directoryView;
    private readonly IisView _iisView;
    private readonly MsiView _msiView;
    private readonly UninstallMenuView _uninstallView;
    private InstallationPaths? _paths;

    public MainMenuView(IUIService ui)
    {
        _ui = ui;
        _summaryView = new SummaryPanelView(ui);
        _downloadView = new DownloadView(ui);
        _resourceView = new ResourceView(ui);
        _directoryView = new DirectoryView(ui);
        _iisView = new IisView(ui);
        _msiView = new MsiView(ui);
        _uninstallView = new UninstallMenuView(ui);
    }

    public async Task ExecuteAsync()
    {
        SummaryStore.Start();

        while (true)
        {
            _ui.Clear();

            if (SummaryStore.HasResults)
            {
                _summaryView.Exibir();
                _ui.WriteEmptyLine();
            }
            _ui.WriteFiglet("NEW ACESSO", "cyan");
            _ui.WriteFiglet("INSTALADOR", "blue");
            _ui.WriteEmptyLine();

            var opcao = _ui.AskChoice(
                "[bold cyan]Selecione uma opção:[/]",
                [
                    "1 - Baixar Instaladores do Google Drive",
                    "2 - Instalar Recursos do Windows",
                    "3 - Criar Diretórios",
                    "4 - Configurar IIS",
                    "5 - Instalar Aplicações (MSIs)",
                    "6 - Instalar WebApps (UI e DS)",
                    "7 - Editar Agendamento de Equipamentos Offline (ControleAcesso)",
                    "8 - Desinstalar NewAcesso",
                    "0 - Sair"
                ]);

            switch (opcao[..1])
            {
                case "1":
                    if (!EnsurePaths()) break;
                    await _downloadView.ExecuteAsync(_paths!);
                    break;

                case "2":
                    await _resourceView.ExecuteAsync();
                    break;

                case "3":
                    if (!EnsurePaths()) break;
                    _directoryView.Execute(_paths!);
                    _ui.WaitForEnter();
                    break;

                case "4":
                    if (!EnsurePaths()) break;
                    await _iisView.ExecuteAsync(_paths!);
                    break;

                case "5":
                    if (!EnsurePaths()) break;
                    await _msiView.ExecuteAsync(_paths!);
                    break;

                case "6":
                    if (!EnsurePaths()) break;
                    await new WebAppView(_ui).ExecuteAsync(_paths!);
                    break;

                case "7":
                    if (!EnsurePaths()) break;
                    _ui.WriteRule("EDIÇÃO DE AGENDAMENTO DE EQUIPAMENTOS OFFLINE (ControleAcesso)", "cyan");
                    _ui.WriteEmptyLine();
                    ControleAcessoAgendamentoHelper.UpdateAgendamentoAfterInstall(_paths!.ControleAcesso);
                    _ui.WriteEmptyLine();
                    _ui.WaitForEnter();
                    break;

                case "8":
                    await _uninstallView.ExecuteAsync();
                    break;

                case "0":
                    _ui.WriteMessage("\n[cyan]Encerrando instalador. Até logo![/]");
                    return;
            }
        }
    }

    private bool EnsurePaths()
    {
        if (_paths != null) return true;

        var basePath = _ui.AskInput(
            "\n[bold yellow]Digite o caminho base[/] ([gray]Ex: C:\\SoftPrime ou D:\\SoftPrime[/]):");

        if (string.IsNullOrWhiteSpace(basePath) || !Directory.Exists(Path.GetPathRoot(basePath)))
        {
            _ui.WriteError("Caminho inválido.");
            _ui.WaitForEnter();
            return false;
        }

        _paths = new InstallationPaths(basePath);
        return true;
    }
}
