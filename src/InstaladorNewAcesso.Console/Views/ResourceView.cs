using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Configurations;
using InstaladorNewAcesso.Core.Factories;
using InstaladorNewAcesso.Core.Utils;
using Spectre.Console;

namespace InstaladorNewAcesso.Console.Views;

public class ResourceView
{
    private readonly IUIService _ui;
    private readonly SummaryPanelView _summaryView;
    private readonly ViewHelper _viewHelper;

    public ResourceView(IUIService ui)
    {
        _ui = ui;
        _summaryView = new SummaryPanelView(ui);
        _viewHelper = new ViewHelper(ui);
    }

    public async Task ExecuteAsync()
    {
        _ui.WriteRule("INSTALADOR NEW ACESSO: RECURSOS DO WINDOWS", "cyan");
        _ui.WriteEmptyLine();

        var installer = InstallerFactory.Create();
        var sxsPath = GetSxsPathFromMenu();

        if (sxsPath == "SAIR") return;

        var setup = new FeatureSetup();
        var features = setup.Features;
        var totalFeatures = features.Count;

        // =========================================
        // FASE 1: SCAN - Verificar recursos instalados
        // =========================================
        _ui.WriteEmptyLine();
        _ui.WriteRule("FASE 1: Verificando recursos instalados", "yellow");
        _ui.WriteEmptyLine();

        var scanResults = new List<(WindowsFeature Feature, bool IsInstalled)>();

        await _ui.ShowStatus($"Consultando status de {totalFeatures} recursos...", async update =>
        {
            scanResults = await installer.CheckFeaturesInstalledAsync(features);
        });

        // Exibir tabela de resultados do scan
        var installedCount = scanResults.Count(r => r.IsInstalled);
        var toInstallCount = scanResults.Count(r => !r.IsInstalled);

        var headers = new[] { "Recurso", "Status" };
        var rows = scanResults
            .OrderBy(r => r.Feature.FriendlyName)
            .Select(r => new[]
            {
                r.Feature.FriendlyName,
                r.IsInstalled ? "[green]Instalado[/]" : "[yellow]Pendente[/]"
            })
            .ToList();

        _ui.WriteTable(headers, rows);
        _ui.WriteEmptyLine();
        _ui.WriteMessage($"[green]{installedCount} já instalados.[/]  [yellow]{toInstallCount} pendentes.[/]");
        _ui.WriteEmptyLine();

        // Se todos já estiverem instalados, encerra
        if (toInstallCount == 0)
        {
            _ui.WriteMessage("[green]Todos os recursos já estão instalados. Nenhuma ação necessária.[/]");
            _ui.WriteEmptyLine();
            _ui.WriteRule("Fim da etapa de Recursos do Windows", "cyan");
            _ui.WaitForEnter();
            return;
        }

        // =========================================
        // CONFIRMAÇÃO
        // =========================================
        var confirm = _ui.Confirm(
            $"[bold yellow]Deseja instalar os {toInstallCount} recurso(s) pendentes?[/]",
            true);

        if (!confirm)
        {
            _ui.WriteMessage("[gray]Instalação cancelada pelo usuário.[/]");
            _ui.WriteEmptyLine();
            _ui.WriteRule("Fim da etapa de Recursos do Windows", "cyan");
            _ui.WaitForEnter();
            return;
        }

        // =========================================
        // FASE 2: INSTALAÇÃO (PARALELA)
        // =========================================
        _ui.WriteEmptyLine();
        _ui.WriteRule("FASE 2: Instalando recursos pendentes (paralelo)", "yellow");
        _ui.WriteEmptyLine();

        var resultadosDaEtapa = new List<SummaryResult>();
        var toInstall = scanResults.Where(r => !r.IsInstalled).Select(r => r.Feature).ToList();

        // Log dos já instalados
        foreach (var result in scanResults.Where(r => r.IsInstalled))
        {
            _ui.WriteMessage($" [cyan]{result.Feature.FriendlyName.EscapeMarkup().PadRight(30)}[/] [gray]IGNORADO[/]");
            resultadosDaEtapa.Add(SummaryStore.Add("Recursos do Windows", result.Feature.FriendlyName, true, "Já instalado"));
        }

        if (toInstall.Count > 0)
        {
            _ui.WriteMessage($"[yellow]Instalando {toInstall.Count} recurso(s) em paralelo (até 4 por vez)...[/]");
            _ui.WriteEmptyLine();

            // Instala todos os recursos em paralelo com limite de concorrência
            var installResults = await installer.InstallFeaturesAsync(toInstall, sxsPath);

            // Ordena por nome para exibição consistente
            foreach (var (feature, sucesso) in installResults.OrderBy(r => r.Feature.FriendlyName))
            {
                if (sucesso)
                {
                    _ui.WriteMessage($"   [green][[SUCESSO]] {feature.FriendlyName.EscapeMarkup()}[/]");
                    resultadosDaEtapa.Add(SummaryStore.Add("Recursos do Windows", feature.FriendlyName, true, "Instalado"));
                }
                else
                {
                    _ui.WriteMessage($"   [red][[FALHA]] {feature.FriendlyName.EscapeMarkup()}[/]");
                    resultadosDaEtapa.Add(SummaryStore.Add("Recursos do Windows", feature.FriendlyName, false, "Falha na instalação"));
                }
            }
        }

        _ui.WriteEmptyLine();
        _summaryView.ExibirEtapa("Recursos do Windows", "🌐", resultadosDaEtapa);
        _ui.WriteEmptyLine();
        _ui.WriteRule("Fim da etapa de Recursos do Windows", "cyan");
        _ui.WaitForEnter();
    }

    private string? GetSxsPathFromMenu()
    {
        var opcao = _ui.AskChoice(
            "Deseja realizar a instalação online ou offline?",
            [
                "Online (Padrão - Requer Internet)",
                "Offline (Utilizando pasta sxs/mídia do Windows)"
            ]);

        if (opcao.StartsWith("Offline", StringComparison.OrdinalIgnoreCase))
        {
            while (true)
            {
                var sxsPath = _ui.AskInput(
                    "\n[bold yellow]Digite o caminho completo da pasta sxs[/] ([gray]Ex: D:\\sources\\sxs[/]):\n" +
                    "Digite '2' para sair ou '3' para voltar ao Online:");

                if (sxsPath == "2") return "SAIR";
                if (sxsPath == "3") return null;

                if (!string.IsNullOrWhiteSpace(sxsPath) && Directory.Exists(sxsPath))
                    return sxsPath;

                _ui.WriteError("Caminho inválido ou inacessível.");
            }
        }

        return null;
    }
}
