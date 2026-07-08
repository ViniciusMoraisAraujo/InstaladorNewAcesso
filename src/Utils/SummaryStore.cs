using System.Diagnostics;
using InstaladorNewAcesso.Models;

namespace InstaladorNewAcesso.Utils;

public static class SummaryStore
{
    private static readonly List<SummaryResult> _results = new();
    private static readonly Stopwatch _stopwatch = new();

    public static void Start()
    {
        _results.Clear();
        _stopwatch.Restart();
    }

    public static SummaryResult Add(string etapa, string item, bool sucesso, string? detalhe = null)
    {
        var result = new SummaryResult
        {
            Etapa = etapa,
            Item = item,
            Sucesso = sucesso,
            Detalhe = detalhe,
            Timestamp = DateTime.Now
        };
        _results.Add(result);
        return result;
    }

    public static (int total, int sucessos, int falhas, TimeSpan elapsed) GetStats()
    {
        var total = _results.Count;
        var sucessos = _results.Count(r => r.Sucesso);
        var falhas = total - sucessos;
        var elapsed = _stopwatch.Elapsed;
        return (total, sucessos, falhas, elapsed);
    }

    public static IReadOnlyList<SummaryResult> GetResults() => _results.AsReadOnly();

    public static string ElapsedFormatted()
    {
        var elapsed = _stopwatch.Elapsed;
        if (elapsed.TotalHours >= 1)
            return $"{elapsed.Hours}h {elapsed.Minutes}min {elapsed.Seconds}seg";
        if (elapsed.TotalMinutes >= 1)
            return $"{elapsed.Minutes}min {elapsed.Seconds}seg";
        return $"{elapsed.Seconds}seg";
    }

    public static bool HasResults => _results.Count > 0;
}
