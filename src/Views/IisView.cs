using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Utils;
using Spectre.Console;

namespace InstaladorNewAcesso.Views;

public class IisView
{
    private readonly IisInstaller _installer = new();

    public async Task ExecuteAsync(InstallationPaths paths)
    {
        AnsiConsole.Write(new Rule("[cyan]CONFIGURAÇÃO DO IIS[/]") { Style = Style.Parse("cyan") });
        AnsiConsole.WriteLine();

        var resultadosDaEtapa = new List<SummaryResult>();

        resultadosDaEtapa.Add(await ConfigureAppPool("WebAppDS", "v4.0", "Integrated"));
        resultadosDaEtapa.Add(await ConfigureAppPool("WebAppUI", "v4.0", "Integrated"));

        resultadosDaEtapa.Add(await ConfigureSite("WebAppDS", "WebAppDS", paths.WebAppDS, 8080));
        resultadosDaEtapa.Add(await ConfigureSite("WebAppUI", "WebAppUI", paths.WebAppUI, 8081));

        AnsiConsole.WriteLine();
        SummaryPanelView.ExibirEtapa("IIS", "⚙️", resultadosDaEtapa);
        AnsiConsole.Write(new Rule("[cyan]Fim da etapa de Configuração do IIS[/]") { Style = Style.Parse("cyan") });
        AnsiConsole.Ask<string>("[gray]Pressione ENTER para continuar...[/]");
    }

    private async Task<SummaryResult> ConfigureAppPool(string name, string runtime, string pipeline)
    {
        AnsiConsole.Markup($"\n Verificando AppPool: [yellow]{name.EscapeMarkup()}[/]... ");

        if (await _installer.AppPoolExistsAsync(name))
        {
            AnsiConsole.MarkupLine("[cyan][IGNORADO] Já existe.[/]");
            return SummaryStore.Add("IIS", $"AppPool {name}", true, "Já existe");
        }

        AnsiConsole.MarkupLine("[blue][CRIANDO][/]");
        bool sucesso = await _installer.CreateApplicationPoolAsync(name, runtime, pipeline);

        if (sucesso)
        {
            AnsiConsole.MarkupLine($" -> [green][SUCESSO] AppPool {name.EscapeMarkup()} criada.[/]");
            return SummaryStore.Add("IIS", $"AppPool {name}", true, "Criada");
        }
        else
        {
            AnsiConsole.MarkupLine($" -> [red][FALHA] Erro ao criar AppPool {name.EscapeMarkup()}.[/]");
            return SummaryStore.Add("IIS", $"AppPool {name}", false, "Falha ao criar");
        }
    }

    private async Task<SummaryResult> ConfigureSite(string name, string poolName, string physicalPath, int port)
    {
        AnsiConsole.Markup($"\n Verificando Site: [yellow]{name.EscapeMarkup()}[/]... ");

        if (await _installer.SiteExistsAsync(name))
        {
            AnsiConsole.MarkupLine("[cyan][IGNORADO] Já existe.[/]");
            return SummaryStore.Add("IIS", $"Site {name}", true, $"Já existe (porta {port})");
        }

        AnsiConsole.MarkupLine("[blue][CRIANDO][/]");
        bool sucesso = await _installer.CreateSiteAsync(name, poolName, physicalPath, port);

        if (sucesso)
        {
            AnsiConsole.MarkupLine($" -> [green][SUCESSO] Site {name.EscapeMarkup()} criado na porta {port}.[/]");
            return SummaryStore.Add("IIS", $"Site {name}", true, $"Criado (porta {port})");
        }
        else
        {
            AnsiConsole.MarkupLine($" -> [red][FALHA] Erro ao criar Site {name.EscapeMarkup()}.[/]");
            return SummaryStore.Add("IIS", $"Site {name}", false, "Falha ao criar");
        }
    }
}
