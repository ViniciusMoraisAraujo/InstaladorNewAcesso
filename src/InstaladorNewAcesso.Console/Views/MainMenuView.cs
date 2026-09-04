using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Console.Views;

public class MainMenuView
{
    private readonly IUIService _ui;
    private readonly SummaryPanelView _summaryView;
    private readonly ResourceView _resourceView;
    private readonly DirectoryView _directoryView;
    private readonly IisView _iisView;
    private readonly MsiView _msiView;
    private readonly ConfigView _configView;
    private readonly UninstallMenuView _uninstallView;
    private InstallationPaths? _paths;

    public MainMenuView(IUIService ui)
    {
        _ui = ui;
        _summaryView = new SummaryPanelView(ui);
        _resourceView = new ResourceView(ui);
        _directoryView = new DirectoryView(ui);
        _iisView = new IisView(ui);
        _msiView = new MsiView(ui);
        _configView = new ConfigView(ui);
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
                "[bold cyan]Selecione uma opcao:[/]",
                [
                    "1 - Instalar Recursos do Windows",
                    "2 - Criar Diretorios",
                    "3 - Configurar IIS",
                    "4 - Instalar Aplicacoes (MSIs)",
                    "5 - Instalar WebApps (UI e DS)",
                    "6 - Editar Agendamento de Equipamentos Offline (ControleAcesso)",
                    "7 - Padronizar/Configurar Arquivos (.config, .ini, .json)",
                    "8 - Desinstalar NewAcesso",
                    "9 - Registrar Tarefa no Windows Task Scheduler",
                    "0 - Sair"
                ]);

            switch (opcao[..1])
            {
                case "1":
                    await _resourceView.ExecuteAsync();
                    break;

                case "2":
                    if (!EnsurePaths()) break;
                    _directoryView.Execute(_paths!);
                    _ui.WaitForEnter();
                    break;

                case "3":
                    if (!EnsurePaths()) break;
                    await _iisView.ExecuteAsync(_paths!);
                    break;

                case "4":
                    if (!EnsurePaths()) break;
                    await _msiView.ExecuteAsync(_paths!);
                    break;

                case "5":
                    if (!EnsurePaths()) break;
                    await new WebAppView(_ui).ExecuteAsync(_paths!);
                    break;

                case "6":
                    if (!EnsurePaths()) break;
                    _ui.WriteRule("EDICAO DE AGENDAMENTO DE EQUIPAMENTOS OFFLINE (ControleAcesso)", "cyan");
                    _ui.WriteEmptyLine();
                    ControleAcessoAgendamentoHelper.UpdateAgendamentoAfterInstall(_paths!.ControleAcesso);
                    _ui.WriteEmptyLine();
                    _ui.WaitForEnter();
                    break;

                case "7":
                    if (!EnsurePaths()) break;
                    await _configView.ExecuteAsync(_paths!);
                    break;

                case "8":
                    await _uninstallView.ExecuteAsync();
                    break;

                case "9":
                    if (!EnsurePaths()) break;
                    _ui.WriteRule("REGISTRAR TAREFA NO TASK SCHEDULER", "cyan");
                    var taskName = _ui.AskInput("Nome da Tarefa (ex: NewAcessoTask):");
                    var execPath = _ui.AskInput("Caminho completo do Executável:");
                    var interval = _ui.AskInput("Intervalo em minutos (ex: 5):");
                    
                    if (string.IsNullOrWhiteSpace(taskName) || string.IsNullOrWhiteSpace(execPath)) {
                        _ui.WriteError("Nome da tarefa e executável são obrigatórios.");
                    } else {
                        var taskInstaller = new InstaladorNewAcesso.Core.Implementations.WindowsTaskInstaller(new InstaladorNewAcesso.Core.Utils.ProcessExecutorService());
                        await taskInstaller.InstallTaskAsync(taskName, execPath, string.IsNullOrWhiteSpace(interval) ? "5" : interval);
                    }
                    _ui.WaitForEnter();
                    break;

                case "0":
                    _ui.WriteMessage("\n[cyan]Encerrando instalador. Ate logo![/]");
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
            _ui.WriteError("Caminho invalido.");
            _ui.WaitForEnter();
            return false;
        }

        basePath = basePath.Trim();
        var folderName = Path.GetFileName(basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        if (folderName.Equals("Installers", StringComparison.OrdinalIgnoreCase) ||
            folderName.Equals("Instaladores", StringComparison.OrdinalIgnoreCase))
        {
            var parent = Path.GetDirectoryName(basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) ?? basePath;
            _paths = new InstallationPaths(parent, basePath);
            return true;
        }

        _paths = new InstallationPaths(basePath);
        return true;
    }
}
