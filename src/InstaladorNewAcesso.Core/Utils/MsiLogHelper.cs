using System.Globalization;
using System.Text.RegularExpressions;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class MsiLogHelper
{
    private const string SubDirectory = "InstaladorNewAcesso";

    /// <summary>
    /// Obtém o diretório onde os logs de instalação serão armazenados.
    /// Cria o diretório se não existir.
    /// </summary>
    public static string GetLogDirectory()
    {
        var logDir = Path.Combine(Path.GetTempPath(), SubDirectory, "Logs");
        Directory.CreateDirectory(logDir);
        return logDir;
    }

    /// <summary>
    /// Gera um caminho completo para o arquivo de log baseado no nome do MSI e timestamp.
    /// </summary>
    public static string GenerateLogFilePath(string msiPath)
    {
        var msiName = Path.GetFileNameWithoutExtension(msiPath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        return Path.Combine(GetLogDirectory(), $"{msiName}_{timestamp}.log");
    }

    /// <summary>
    /// Analisa o log do MSI em busca da causa do erro 1603.
    /// Retorna um relatório com as informações encontradas.
    /// </summary>
    public static MsiLogAnalysisResult AnalyzeLog(string logFilePath)
    {
        var result = new MsiLogAnalysisResult
        {
            LogFilePath = logFilePath,
            HasCriticalError = false,
            ReturnValue3Line = null,
            ErrorSummary = null
        };

        if (!File.Exists(logFilePath))
        {
            result.ErrorSummary = "Arquivo de log não encontrado.";
            return result;
        }

        try
        {
            var lines = File.ReadAllLines(logFilePath);
            var returnValue3Index = -1;

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("Return value 3", StringComparison.OrdinalIgnoreCase))
                {
                    returnValue3Index = i;
                    result.HasCriticalError = true;
                    result.ReturnValue3Line = i + 1;
                    break;
                }

                if (lines[i].Contains("Error 1603", StringComparison.OrdinalIgnoreCase) ||
                    lines[i].Contains("Fatal error during installation", StringComparison.OrdinalIgnoreCase))
                {
                    result.HasCriticalError = true;
                }
            }

            if (returnValue3Index >= 0)
            {
                var startIndex = Math.Max(0, returnValue3Index - 5);
                var endIndex = Math.Min(lines.Length - 1, returnValue3Index + 3);
                var contextLines = new List<string>();

                for (var i = startIndex; i <= endIndex; i++)
                {
                    var prefix = i == returnValue3Index ? ">>> " : "    ";
                    contextLines.Add($"{prefix}[L{i + 1}] {lines[i]}");
                }

                result.ErrorContext = contextLines.ToArray();
            }

            var customActionMatch = Regex.Match(
                string.Join(Environment.NewLine, lines),
                @"CustomAction\s+(\w+)\s+returned\s+actual\s+error\s+code\s+\d+",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);

            if (customActionMatch.Success)
            {
                result.FailedCustomAction = customActionMatch.Groups[1].Value;
            }

            var propertyLines = lines
                .Where(l => l.Contains("Property(S)", StringComparison.OrdinalIgnoreCase) &&
                            (l.Contains("TARGETDIR", StringComparison.OrdinalIgnoreCase) ||
                             l.Contains("TARGETSITE", StringComparison.OrdinalIgnoreCase) ||
                             l.Contains("TARGETAPPPOOL", StringComparison.OrdinalIgnoreCase) ||
                             l.Contains("TARGETVDIR", StringComparison.OrdinalIgnoreCase) ||
                             l.Contains("INSTALLDIR", StringComparison.OrdinalIgnoreCase) ||
                             l.Contains("CustomActionData", StringComparison.OrdinalIgnoreCase)))
                .Take(20)
                .ToArray();

            if (propertyLines.Length > 0)
            {
                result.RelevantProperties = propertyLines;
            }
        }
        catch (Exception ex)
        {
            result.ErrorSummary = $"Erro ao analisar o log: {ex.Message}";
            return result;
        }

        var summary = new List<string>();

        if (result.HasCriticalError)
        {
            summary.Add("\u26a0\ufe0f  ERRO CR\u00cdTICO detectado (Return value 3).");

            if (result.FailedCustomAction != null)
            {
                summary.Add($"   Custom Action que falhou: {result.FailedCustomAction}");
            }

            if (result.ReturnValue3Line.HasValue)
            {
                summary.Add($"   Linha do erro: {result.ReturnValue3Line.Value}");
            }
        }
        else
        {
            summary.Add("Nenhum 'Return value 3' encontrado no log.");
            summary.Add("O erro pode ser anterior \u00e0 execu\u00e7\u00e3o das Custom Actions.");
        }

        summary.Add($"   Log completo: {logFilePath}");

        if (result.RelevantProperties?.Length > 0)
        {
            summary.Add("   Propriedades relevantes encontradas no log:");
            foreach (var prop in result.RelevantProperties)
            {
                summary.Add($"     {prop.Trim()}");
            }
        }

        summary.Add("");
        summary.Add("\U0001f4cc Para an\u00e1lise aprofundada, abra o .log no Orca (Windows SDK)");
        summary.Add("   ou utilize um editor de texto e procure por 'Return value 3'.");

        result.ErrorSummary = string.Join(Environment.NewLine, summary);

        return result;
    }

    /// <summary>
    /// Exibe o diagnóstico da instalação no console com cores.
    /// </summary>
    public static void DisplayLogAnalysis(string logFilePath)
    {
        var analysis = AnalyzeLog(logFilePath);

        if (UIScope.Current != null)
        {
            UIScope.Current.WritePanel(
                analysis.ErrorSummary ?? "Nenhuma análise disponível.",
                "DIAGNÓSTICO DA INSTALAÇÃO", "yellow");
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("=== DIAGNÓSTICO DA INSTALAÇÃO ===");
            Console.WriteLine(analysis.ErrorSummary ?? "Nenhuma análise disponível.");
            Console.WriteLine("==================================");
        }

        if (analysis.ErrorContext != null && analysis.ErrorContext.Length > 0)
        {
            UIScope.WriteMessage("[gray]\nContexto do erro:[/]");
            foreach (var line in analysis.ErrorContext)
            {
                UIScope.WriteMessage($"[gray]{line}[/]");
            }
        }
    }
}

public class MsiLogAnalysisResult
{
    public string LogFilePath { get; set; } = string.Empty;
    public bool HasCriticalError { get; set; }
    public int? ReturnValue3Line { get; set; }
    public string? FailedCustomAction { get; set; }
    public string? ErrorSummary { get; set; }
    public string[]? ErrorContext { get; set; }
    public string[]? RelevantProperties { get; set; }
}
