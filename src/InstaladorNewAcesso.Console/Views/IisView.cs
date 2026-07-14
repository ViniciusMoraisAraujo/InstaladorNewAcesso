using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Utils;
using Spectre.Console;

namespace InstaladorNewAcesso.Console.Views;

public class IisView
{
    private readonly IUIService _ui;
    private readonly IisInstaller _installer = new();
    private readonly SummaryPanelView _summaryView;

    public IisView(IUIService ui)
    {
        _ui = ui;
        _summaryView = new SummaryPanelView(ui);
    }

    public async Task ExecuteAsync(InstallationPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _ui.WriteRule("CONFIGURAÇÃO DO IIS", "cyan");
        _ui.WriteEmptyLine();

        var resultadosDaEtapa = new List<SummaryResult>();

        // =========================================
        // FASE 1: SCAN - Verificar recursos IIS existentes
        // =========================================
        _ui.WriteEmptyLine();
        _ui.WriteRule("FASE 1: Verificando IIS", "yellow");
        _ui.WriteEmptyLine();

        var appPoolNames = new[] { "WebAppDS", "WebAppUI" };
        var siteNames = new[] { "WebAppDS", "WebAppUI" };

        Dictionary<string, bool> appPoolsStatus = new();
        Dictionary<string, bool> sitesStatus = new();

        await _ui.ShowStatus("Verificando recursos do IIS...", async update =>
        {
            appPoolsStatus = await _installer.CheckAppPoolsExistAsync(appPoolNames);
            sitesStatus = await _installer.CheckSitesExistAsync(siteNames);
        });

        // =========================================
        // FASE 2: CONFIGURAÇÃO
        // =========================================
        _ui.WriteEmptyLine();
        _ui.WriteRule("FASE 2: Configurando IIS", "yellow");
        _ui.WriteEmptyLine();

        resultadosDaEtapa.Add(await ConfigureAppPool("WebAppDS", "v4.0", "Integrated", appPoolsStatus.GetValueOrDefault("WebAppDS", false)));
        resultadosDaEtapa.Add(await ConfigureAppPool("WebAppUI", "v4.0", "Integrated", appPoolsStatus.GetValueOrDefault("WebAppUI", false)));

        resultadosDaEtapa.Add(await ConfigureSite("WebAppDS", "WebAppDS", paths.WebAppDS, 8080, sitesStatus.GetValueOrDefault("WebAppDS", false)));
        resultadosDaEtapa.Add(await ConfigureSite("WebAppUI", "WebAppUI", paths.WebAppUI, 8081, sitesStatus.GetValueOrDefault("WebAppUI", false)));

        _ui.WriteEmptyLine();
        _summaryView.ExibirEtapa("IIS", "⚙️", resultadosDaEtapa);
        _ui.WriteRule("Fim da etapa de Configuração do IIS", "cyan");
        _ui.WaitForEnter();
    }

    private async Task<SummaryResult> ConfigureAppPool(string name, string runtime, string pipeline, bool alreadyExists)
    {
        _ui.WriteInline($"\n Verificando AppPool: [yellow]{name.EscapeMarkup()}[/]... ");

        if (alreadyExists)
        {
            _ui.WriteMessage("[cyan]IGNORADO Já existe.[/]");
            return SummaryStore.Add("IIS", $"AppPool {name}", true, "Já existe");
        }

        _ui.WriteMessage("[blue][[CRIANDO]][/]");
        var sucesso = await _installer.CreateApplicationPoolAsync(name, runtime, pipeline);

        if (sucesso)
        {
            _ui.WriteMessage($" -> [green][[SUCESSO]] AppPool {name.EscapeMarkup()} criada.[/]");
            return SummaryStore.Add("IIS", $"AppPool {name}", true, "Criada");
        }
        else
        {
            _ui.WriteMessage($" -> [red][[FALHA]] Erro ao criar AppPool {name.EscapeMarkup()}.[/]");
            return SummaryStore.Add("IIS", $"AppPool {name}", false, "Falha ao criar");
        }
    }

    private async Task<SummaryResult> ConfigureSite(string name, string poolName, string physicalPath, int port, bool alreadyExists)
    {
        _ui.WriteInline($"\n Verificando Site: [yellow]{name.EscapeMarkup()}[/]... ");

        if (alreadyExists)
        {
            _ui.WriteMessage("[cyan]IGNORADO Já existe.[/]");
            return SummaryStore.Add("IIS", $"Site {name}", true, $"Já existe (porta {port})");
        }

        _ui.WriteMessage("[blue][[CRIANDO]][/]");
        var sucesso = await _installer.CreateSiteAsync(name, poolName, physicalPath, port);

        if (sucesso)
        {
            _ui.WriteMessage($" -> [green][[SUCESSO]] Site {name.EscapeMarkup()} criado na porta {port}.[/]");
            return SummaryStore.Add("IIS", $"Site {name}", true, $"Criado (porta {port})");
        }
        else
        {
            _ui.WriteMessage($" -> [red][[FALHA]] Erro ao criar Site {name.EscapeMarkup()}.[/]");
            return SummaryStore.Add("IIS", $"Site {name}", false, "Falha ao criar");
        }
    }
}
