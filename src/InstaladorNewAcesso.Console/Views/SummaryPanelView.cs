using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Utils;
using Spectre.Console;

namespace InstaladorNewAcesso.Console.Views;

public class SummaryPanelView
{
    private readonly IUIService _ui;

    private static readonly Dictionary<string, string> EtapaEmojis = new()
    {
        ["Recursos do Windows"] = "🌐",
        ["Diretórios"] = "📂",
        ["IIS"] = "⚙️",
        ["Aplicações (MSIs)"] = "📦",
        ["WebApps"] = "🌍",
    };

    public SummaryPanelView(IUIService ui)
    {
        _ui = ui;
    }

    /// <summary>
    /// Exibe o painel completo com todas as etapas realizadas até o momento.
    /// </summary>
    public void Exibir()
    {
        if (!SummaryStore.HasResults) return;

        var stats = SummaryStore.GetStats();
        var results = SummaryStore.GetResults();

        var content = new List<string>();
        content.Add(string.Empty);

        // Agrupa por etapa
        var grupos = results.GroupBy(r => r.Etapa);

        foreach (var grupo in grupos)
        {
            var emoji = EtapaEmojis.GetValueOrDefault(grupo.Key, "📋");
            content.Add($"  {emoji} [bold]{grupo.Key.EscapeMarkup()}[/]");

            foreach (var item in grupo)
            {
                var icon = item.Sucesso ? "[green]✅[/]" : "[red]❌[/]";
                var detalhe = item.Detalhe ?? (item.Sucesso ? "OK" : "Falhou");
                var corDetalhe = item.Sucesso ? "green" : "red";

                // Alinha a descrição em colunas (40 chars para o item, resto para detalhe)
                var itemNome = item.Item.Length > 55
                    ? item.Item[..52] + "..."
                    : item.Item;

                content.Add($"    {icon} [gray]{itemNome.EscapeMarkup().PadRight(55)}[/] [{corDetalhe}]{detalhe.EscapeMarkup()}[/]");
            }

            content.Add(string.Empty);
        }

        // Linha separadora
        content.Add($"  ───────────────────────────────────────────────────");

        // Totais
        var corGeral = stats.falhas > 0 ? "red" : "green";
        content.Add(string.Empty);
        content.Add($"  [{corGeral}]✅ {stats.sucessos}/{stats.total} itens concluídos com sucesso[/]");
        content.Add($"  [gray]⏱️  Tempo total: {SummaryStore.ElapsedFormatted()}[/]");
        content.Add(string.Empty);

        if (stats.falhas > 0)
        {
            content.Add($"  [red]❌ {stats.falhas} falha(s) encontrada(s). Verifique os itens em vermelho acima.[/]");
            content.Add(string.Empty);
        }

        _ui.WritePanel(
            string.Join(Environment.NewLine, content),
            "📊 RESUMO DA INSTALAÇÃO",
            "yellow");
    }

    /// <summary>
    /// Exibe um painel compacto para uma etapa específica (exibido ao final da etapa).
    /// </summary>
    public void ExibirEtapa(string etapa, string emoji, List<SummaryResult> resultadosDaEtapa)
    {
        ArgumentNullException.ThrowIfNull(resultadosDaEtapa);
        if (resultadosDaEtapa.Count == 0) return;

        var sucessos = resultadosDaEtapa.Count(r => r.Sucesso);
        var falhas = resultadosDaEtapa.Count - sucessos;
        var total = resultadosDaEtapa.Count;

        var lines = new List<string>();
        lines.Add(string.Empty);

        foreach (var item in resultadosDaEtapa)
        {
            var icon = item.Sucesso ? "[green]✅[/]" : "[red]❌[/]";
            var detalhe = item.Detalhe ?? (item.Sucesso ? "OK" : "Falhou");
            var corDetalhe = item.Sucesso ? "green" : "red";

            var itemNome = item.Item.Length > 50
                ? item.Item[..47] + "..."
                : item.Item;

            lines.Add($"  {icon} [gray]{itemNome.EscapeMarkup().PadRight(50)}[/] [{corDetalhe}]{detalhe.EscapeMarkup()}[/]");
        }

        lines.Add(string.Empty);
        lines.Add($"  ─────────────────────────────────────");

        var corTotal = falhas > 0 ? "red" : "green";
        lines.Add($"  [{corTotal}]✅ {sucessos}/{total} concluídos[/]");

        if (falhas > 0)
            lines.Add($"  [red]❌ {falhas} falha(s)[/]");

        _ui.WritePanel(
            string.Join(Environment.NewLine, lines),
            $"{emoji} {etapa}",
            "cyan");
    }
}
