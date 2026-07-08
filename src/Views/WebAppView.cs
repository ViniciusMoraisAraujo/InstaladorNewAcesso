using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Services;
using InstaladorNewAcesso.Utils;
using Spectre.Console;

namespace InstaladorNewAcesso.Views;

public class WebAppView
{
    private readonly WebAppInstaller _installer = new();
    private List<WebAppModel> _webApps = new();

    public async Task ExecuteAsync(InstallationPaths paths)
    {
        AnsiConsole.Write(new Rule("[cyan]INSTALAÇÃO DE WEB APPS[/]") { Style = Style.Parse("cyan") });
        AnsiConsole.WriteLine();

        var msiRoot = AnsiConsole.Prompt(
            new TextPrompt<string>("[bold yellow]Diretório raiz dos instaladores WEBApp[/] ([gray]ENTER para usar o padrão[/]):")
                .AllowEmpty());

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

        var scanner = new WebAppScanner(paths, dbChoice, msiRoot);
        AnsiConsole.Markup("\n Escaneando Web Apps... ");
        _webApps = scanner.Scan();

        AnsiConsole.MarkupLine($"[green][{_webApps.Count} ENCONTRADOS][/]");

        if (_webApps.Count == 0)
        {
            AnsiConsole.MarkupLine("\n[yellow]Nenhum Web App encontrado (WebAppUI/WebAppDS).[/]");
        }
        else
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn(new TableColumn("[yellow]MSI[/]"))
                .AddColumn(new TableColumn("[yellow]Site[/]"))
                .AddColumn(new TableColumn("[yellow]Porta[/]"))
                .AddColumn(new TableColumn("[yellow]Destino[/]"));

            foreach (var app in _webApps)
            {
                string nome = Path.GetFileName(app.MsiPath);
                table.AddRow(
                    nome.EscapeMarkup(),
                    app.SiteName.EscapeMarkup(),
                    app.Port.ToString(),
                    app.TargetDirectory.EscapeMarkup());
            }

            AnsiConsole.Write(table);

            var generateLog = AnsiConsole.Confirm("\n[bold yellow]Gerar log verbose da instalação para diagnóstico?[/]", false);
            if (generateLog)
            {
                AnsiConsole.MarkupLine($" [gray]Os logs serão salvos em: {MsiLogHelper.GetLogDirectory().EscapeMarkup()}[/]");
            }

            foreach (var app in _webApps)
                app.GenerateLog = generateLog;

            if (AnsiConsole.Confirm("\n[bold yellow]Deseja instalar os Web Apps?[/]"))
            {
                await InstallAllAsync(paths);
            }
            else
            {
                AnsiConsole.MarkupLine("[gray]Instalação cancelada.[/]");
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[cyan]Fim da etapa de Instalação de Web Apps[/]") { Style = Style.Parse("cyan") });
        AnsiConsole.Ask<string>("[gray]Pressione ENTER para continuar...[/]");
    }

    private async Task InstallAllAsync(InstallationPaths paths)
    {
        var resultadosDaEtapa = new List<SummaryResult>();
        int sucessos = 0;

        foreach (var app in _webApps)
        {
            AnsiConsole.Markup($"\n [{app.SiteName.EscapeMarkup()}] Instalando... ");
            bool ok = await _installer.InstallAsync(app, paths);

            if (ok)
            {
                AnsiConsole.MarkupLine("[green][OK][/]");
                resultadosDaEtapa.Add(SummaryStore.Add("WebApps", $"{app.SiteName} (porta {app.Port})", true, $"Instalado em {app.TargetDirectory}"));
                sucessos++;
            }
            else
            {
                AnsiConsole.MarkupLine("[red][FALHA][/]");
                resultadosDaEtapa.Add(SummaryStore.Add("WebApps", $"{app.SiteName} (porta {app.Port})", false, "Falha na instalação"));
            }
        }

        AnsiConsole.WriteLine();
        SummaryPanelView.ExibirEtapa("WebApps", "🌍", resultadosDaEtapa);
        AnsiConsole.MarkupLine($"\n[cyan]Instalação concluída. {sucessos}/{_webApps.Count} Web Apps instalados.[/]");
    }
}
