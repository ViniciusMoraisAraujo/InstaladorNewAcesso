using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Services;
using InstaladorNewAcesso.Utils;
using Spectre.Console;

namespace InstaladorNewAcesso.Views;

public class MsiView
{
    private readonly MsiInstaller _installer = new();
    private List<MsiInstallationModel> _todosMsi = new();
    private readonly List<MsiInstallationModel> _outros = new();
    private readonly List<MsiInstallationModel> _fabricantes = new();

    public async Task ExecuteAsync(InstallationPaths paths)
    {
        AnsiConsole.Write(new Rule("[cyan]INSTALAÇÃO DE APLICAÇÕES (MSIs)[/]") { Style = Style.Parse("cyan") });
        AnsiConsole.WriteLine();

        var msiRoot = AnsiConsole.Prompt(
            new TextPrompt<string>("[bold yellow]Diretório raiz dos instaladores MSI[/] ([gray]ENTER para padrão: Installers\\PrimeAcesso V5.9[/]):")
                .AllowEmpty()
                .DefaultValueStyle(new Style().Foreground(Color.Grey)));

        if (string.IsNullOrWhiteSpace(msiRoot))
        {
            msiRoot = Path.Combine(paths.InstallationPath, "PrimeAcesso V5.9");
            AnsiConsole.MarkupLine($" [gray]Usando diretório padrão: {msiRoot.EscapeMarkup()}[/]");
        }

        if (!Directory.Exists(msiRoot))
        {
            AnsiConsole.MarkupLine($"[red][ERRO] Diretório não encontrado: {msiRoot.EscapeMarkup()}[/]");
            AnsiConsole.Ask<string>("[gray]Pressione ENTER para continuar...[/]");
            return;
        }

        var dbChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold yellow]Banco de dados:[/]")
                .AddChoices(["SQLServer", "Oracle"]));

        var scanner = new MsiScanner(paths, dbChoice, msiRoot);
        await AnsiConsole.Status()
            .StartAsync("Escaneando MSIs...", async ctx =>
            {
                try
                {
                    _todosMsi = scanner.Scan();
                    AnsiConsole.MarkupLine($" [green][{_todosMsi.Count} ENCONTRADOS][/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red][FALHA] {ex.Message.EscapeMarkup()}[/]");
                    AnsiConsole.Ask<string>("[gray]Pressione ENTER para continuar...[/]");
                    return;
                }
            });

        if (_todosMsi.Count == 0)
        {
            AnsiConsole.MarkupLine("\n[yellow]Nenhum MSI regular encontrado (WebApps são ignorados).[/]");
        }
        else
        {
            var generateLog = AnsiConsole.Confirm("\n[bold yellow]Gerar log verbose da instalação para diagnóstico?[/]", false);
            if (generateLog)
            {
                AnsiConsole.MarkupLine($" [gray]Os logs serão salvos em: {MsiLogHelper.GetLogDirectory().EscapeMarkup()}[/]");
            }

            foreach (var msi in _todosMsi)
                msi.GenerateLog = generateLog;

            SepararMsIs(paths);

            if (_outros.Count > 0)
            {
                AnsiConsole.MarkupLine($"\n[cyan]{_outros.Count} MSI(s) gerais encontrados[/] (não fabricantes).");
                if (AnsiConsole.Confirm("Deseja instalar todos eles?"))
                {
                    await InstalarListaAsync(_outros);
                }
                else
                {
                    AnsiConsole.MarkupLine("[gray]Instalação dos MSIs gerais cancelada.[/]");
                }
            }

            if (_fabricantes.Count > 0)
            {
                await TelaFabricantesAsync();
            }
            else if (_outros.Count == 0)
            {
                AnsiConsole.MarkupLine("\n[yellow]Nenhum MSI encontrado.[/]");
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[cyan]Fim da etapa de Instalação de MSIs[/]") { Style = Style.Parse("cyan") });
        AnsiConsole.Ask<string>("[gray]Pressione ENTER para continuar...[/]");
    }

    private void SepararMsIs(InstallationPaths paths)
    {
        string fabricantesPath = Path.Combine(paths.Controller, "Fabricantes");
        foreach (var msi in _todosMsi)
        {
            if (msi.TargetDirectory.StartsWith(fabricantesPath, StringComparison.OrdinalIgnoreCase))
                _fabricantes.Add(msi);
            else
                _outros.Add(msi);
        }
    }

    private async Task TelaFabricantesAsync()
    {
        Console.Clear();
        AnsiConsole.Write(new Rule("[cyan]FABRICANTES DISPONÍVEIS[/]") { Style = Style.Parse("cyan") });
        AnsiConsole.WriteLine();

        // Tabela de fabricantes
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[yellow]#[/]").Centered())
            .AddColumn(new TableColumn("[yellow]MSI[/]"))
            .AddColumn(new TableColumn("[yellow]Destino[/]"));

        for (int i = 0; i < _fabricantes.Count; i++)
        {
            string nome = Path.GetFileName(_fabricantes[i].MsiPath) ?? "";
            table.AddRow(
                $"{i + 1}",
                nome.EscapeMarkup(),
                _fabricantes[i].TargetDirectory.EscapeMarkup());
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        var opcao = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold yellow]Opções de instalação:[/]")
                .AddChoices([
                    "T - Instalar TODOS os fabricantes",
                    "S - Selecionar manualmente",
                    "N - Não instalar fabricantes"
                ]));

        switch (opcao[..1])
        {
            case "T":
                await InstalarListaAsync(_fabricantes);
                break;
            case "S":
                var input = AnsiConsole.Ask<string>("[bold yellow]Digite os números dos fabricantes[/] ([gray]ex: 1,3[/]):");
                var indicesLocais = ParseIndices(input, _fabricantes.Count);
                if (indicesLocais.Count > 0)
                {
                    var selecionados = indicesLocais.Select(i => _fabricantes[i]).ToList();
                    await InstalarListaAsync(selecionados);
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]Nenhum índice válido. Nenhum fabricante será instalado.[/]");
                }
                break;
            case "N":
                AnsiConsole.MarkupLine("[gray]Fabricantes ignorados.[/]");
                break;
        }
    }

    private List<int> ParseIndices(string? input, int max)
    {
        var indices = new List<int>();
        if (string.IsNullOrWhiteSpace(input))
            return indices;

        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (int.TryParse(part.Trim(), out int num) && num >= 1 && num <= max)
            {
                indices.Add(num - 1);
            }
        }
        return indices.Distinct().OrderBy(i => i).ToList();
    }

    private async Task InstalarListaAsync(List<MsiInstallationModel> lista)
    {
        AnsiConsole.MarkupLine($"\n[cyan]Iniciando instalação de {lista.Count} MSI(s)...[/]\n");

        var resultadosDaEtapa = new List<SummaryResult>();
        int sucessos = 0;

        for (int i = 0; i < lista.Count; i++)
        {
            var model = lista[i];
            string nome = Path.GetFileName(model.MsiPath) ?? "";

            AnsiConsole.Markup($" [{i + 1}/{lista.Count}] {nome.EscapeMarkup().PadRight(50)}... ");

            bool ok = await _installer.InstallAsync(model);

            if (ok)
            {
                AnsiConsole.MarkupLine("[green][SUCESSO][/]");
                resultadosDaEtapa.Add(SummaryStore.Add("Aplicações (MSIs)", nome, true));
                sucessos++;
            }
            else
            {
                AnsiConsole.MarkupLine("[red][FALHA][/]");
                resultadosDaEtapa.Add(SummaryStore.Add("Aplicações (MSIs)", nome, false, "Falha na instalação"));
            }
        }

        AnsiConsole.WriteLine();
        SummaryPanelView.ExibirEtapa("Aplicações (MSIs)", "📦", resultadosDaEtapa);
        AnsiConsole.MarkupLine($"\n[cyan]Instalação concluída. {sucessos}/{lista.Count} MSIs instalados com sucesso.[/]");
    }
}
