using InstaladorNewAcesso.Configurations;
using InstaladorNewAcesso.Factories;
using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Utils;
using Spectre.Console;

namespace InstaladorNewAcesso.Views;

public class ResourceView
{
    public async Task ExecuteAsync()
    {
        AnsiConsole.Write(new Rule("[cyan]INSTALADOR NEW ACESSO: RECURSOS DO WINDOWS[/]") { Style = Style.Parse("cyan") });
        AnsiConsole.WriteLine();

        var installer = InstallerFactory.Create();
        string? sxsPath = GetSxsPathFromMenu();

        if (sxsPath == "SAIR") return;

        await AnsiConsole.Status()
            .StartAsync("Carregando Recursos do Sistema...", async ctx =>
            {
                var setup = new FeatureSetup();

                ctx.Status = "Verificando recursos instalados...";
                var checkTasks = setup.Features
                    .Select(async feature => new
                    {
                        Feature = feature,
                        IsInstalled = await installer.IsFeatureInstalledAsync(feature)
                    });

                var results = await Task.WhenAll(checkTasks);

                var toInstall = results
                    .Where(r => !r.IsInstalled)
                    .Select(r => r.Feature)
                    .ToList();

                var resultadosDaEtapa = new List<SummaryResult>();

                foreach (var installed in results.Where(r => r.IsInstalled))
                {
                    AnsiConsole.MarkupLine($" [cyan]{installed.Feature.FriendlyName.EscapeMarkup().PadRight(30)} [gray][IGNORADO][/][/]");
                    resultadosDaEtapa.Add(SummaryStore.Add("Recursos do Windows", installed.Feature.FriendlyName, true, "Já instalado"));
                }

                AnsiConsole.MarkupLine($"\n [gray]{results.Count(r => r.IsInstalled)} já instalados. {toInstall.Count} para instalar.[/]");

                foreach (var feature in toInstall)
                {
                    ctx.Status = $"Instalando: {feature.FriendlyName}...";
                    AnsiConsole.Markup($"\n [yellow]Instalando:[/] {feature.FriendlyName.EscapeMarkup().PadRight(30)}... ");

                    bool sucesso = await installer.InstallFeatureAsync(feature, sxsPath);

                    if (sucesso)
                    {
                        AnsiConsole.MarkupLine("[green][SUCESSO][/]");
                        resultadosDaEtapa.Add(SummaryStore.Add("Recursos do Windows", feature.FriendlyName, true, "Instalado"));
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red][FALHA][/]");
                        resultadosDaEtapa.Add(SummaryStore.Add("Recursos do Windows", feature.FriendlyName, false, "Falha na instalação"));
                    }
                }

                AnsiConsole.WriteLine();
                SummaryPanelView.ExibirEtapa("Recursos do Windows", "🌐", resultadosDaEtapa);
            });

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[cyan]Fim da etapa de Recursos do Windows[/]") { Style = Style.Parse("cyan") });
        AnsiConsole.Ask<string>("[gray]Pressione ENTER para continuar...[/]");
    }

    private string? GetSxsPathFromMenu()
    {
        var opcao = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Deseja realizar a instalação online ou offline?")
                .AddChoices([
                    "Online (Padrão - Requer Internet)",
                    "Offline (Utilizando pasta sxs/mídia do Windows)"
                ]));

        if (opcao.StartsWith("Offline"))
        {
            while (true)
            {
                var sxsPath = AnsiConsole.Ask<string>(
                    "\n[bold yellow]Digite o caminho completo da pasta sxs[/] ([gray]Ex: D:\\sources\\sxs[/]):\n" +
                    "Digite '2' para sair ou '3' para voltar ao Online:");

                if (sxsPath == "2") return "SAIR";
                if (sxsPath == "3") return null;

                if (!string.IsNullOrWhiteSpace(sxsPath) && Directory.Exists(sxsPath))
                    return sxsPath;

                AnsiConsole.MarkupLine("[red]Caminho inválido ou inacessível.[/]");
            }
        }

        return null;
    }
}
