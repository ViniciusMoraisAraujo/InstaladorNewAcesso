using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Utils;
using Spectre.Console;

namespace InstaladorNewAcesso.Views;

public class MainMenuView
{
    private readonly ResourceView _resourceView = new();
    private readonly DirectoryView _directoryView = new();
    private readonly IisView _iisView = new();
    private readonly MsiView _msiView = new();
    private InstallationPaths? _paths;

    public async Task ExecuteAsync()
    {
        SummaryStore.Start();

        while (true)
        {
            Console.Clear();

            if (SummaryStore.HasResults)
            {
                SummaryPanelView.Exibir();
                AnsiConsole.WriteLine();
            }
            AnsiConsole.Write(
                new FigletText("NEW ACESSO")
                    .Centered()
                    .Color(Color.Cyan));

            AnsiConsole.Write(
                new FigletText("INSTALADOR")
                    .Centered()
                    .Color(Color.DodgerBlue1));

            AnsiConsole.WriteLine();

            var opcao = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold cyan]Selecione uma opção:[/]")
                    .PageSize(10)
                    .AddChoices([
                        "1 - Instalar Recursos do Windows",
                        "2 - Criar Diretórios",
                        "3 - Configurar IIS",
                        "4 - Instalar Aplicações (MSIs)",
                        "5 - Instalar WebApps (UI e DS)",
                        "6 - Editar Agendamento de Equipamentos Offline (ControleAcesso)",
                        "0 - Sair"
                    ]));

            switch (opcao[..1])
            {
                case "1":
                    await _resourceView.ExecuteAsync();
                    break;

                case "2":
                    if (!EnsurePaths()) break;
                    _directoryView.ExecuteAsync(_paths!);
                    AnsiConsole.Ask<string>("[gray]Pressione ENTER para continuar...[/]");
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
                    await new WebAppView().ExecuteAsync(_paths!);
                    break;

                case "6":
                    if (!EnsurePaths()) break;
                    AnsiConsole.Write(new Rule("[cyan]EDIÇÃO DE AGENDAMENTO DE EQUIPAMENTOS OFFLINE (ControleAcesso)[/]") { Style = Style.Parse("cyan") });
                    AnsiConsole.WriteLine();
                    ControleAcessoAgendamentoHelper.UpdateAgendamentoAfterInstall(_paths!.ControleAcesso);
                    AnsiConsole.WriteLine();
                    AnsiConsole.Ask<string>("[gray]Pressione ENTER para continuar...[/]");
                    break;

                case "0":
                    AnsiConsole.MarkupLine("\n[cyan]Encerrando instalador. Até logo![/]");
                    return;
            }
        }
    }

    private bool EnsurePaths()
    {
        if (_paths != null) return true;

        var basePath = AnsiConsole.Ask<string>(
            "\n[bold yellow]Digite o caminho base[/] ([gray]Ex: C:\\SoftPrime ou D:\\SoftPrime[/]):");

        if (string.IsNullOrWhiteSpace(basePath) || !Directory.Exists(Path.GetPathRoot(basePath)))
        {
            AnsiConsole.MarkupLine("\n[red][ERRO] Caminho inválido.[/]");
            AnsiConsole.Ask<string>("[gray]Pressione ENTER para continuar...[/]");
            return false;
        }

        _paths = new InstallationPaths(basePath);
        return true;
    }
}
