using System.Globalization;
using System.Text;

namespace InstaladorNewAcesso.Core.Utils;

public static class AuditLogger
{
    private static string? _logFilePath;
    private static readonly object _lock = new();
    private static int _totalOps;
    private static int _successOps;
    private static int _failOps;

    /// <summary>
    /// Inicializa o log de auditoria, criando o arquivo com cabeçalho.
    /// </summary>
    public static void Start(string basePath)
    {
        var logDir = Path.Combine(Path.GetTempPath(), "InstaladorNewAcesso", "Logs");
        Directory.CreateDirectory(logDir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        _logFilePath = Path.Combine(logDir, $"uninstall_audit_{timestamp}.txt");
        _totalOps = 0;
        _successOps = 0;
        _failOps = 0;

        var now = DateTime.Now;
        var header = new StringBuilder();
        header.AppendLine("═══════════════════════════════════════════════════════");
        header.AppendLine("  AUDITORIA DE DESINSTALAÇÃO - NEW ACESSO");
        header.AppendLine($"  Data: {now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture)}");
        header.AppendLine($"  Caminho base: {basePath}");
        header.AppendLine($"  Máquina: {Environment.MachineName}");
        header.AppendLine($"  Usuário: {Environment.UserName}");
        header.AppendLine("═══════════════════════════════════════════════════════");
        header.AppendLine();

        File.WriteAllText(_logFilePath, header.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// Registra uma operação no log de auditoria.
    /// </summary>
    public static void Log(string operacao, string item, bool sucesso, string? detalhe = null)
    {
        if (_logFilePath == null) return;

        _totalOps++;
        if (sucesso) _successOps++; else _failOps++;

        var timestamp = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        var status = sucesso ? "  ✅ OK  " : "  ❌ FALHA";
        var line = $"[{timestamp}] {status} | {operacao,-35} | {item}";

        if (!string.IsNullOrWhiteSpace(detalhe))
            line += $" | {detalhe}";

        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Silently ignore write errors
            }
        }
    }

    /// <summary>
    /// Registra uma linha de separação no log.
    /// </summary>
    public static void Separator(string titulo)
    {
        if (_logFilePath == null) return;

        var line = $"\n--- {titulo} ---\n";

        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logFilePath, line, Encoding.UTF8);
            }
            catch { }
        }
    }

    /// <summary>
    /// Registra o resumo final com base nos contadores internos e encerra o log.
    /// </summary>
    public static void Finish()
    {
        if (_logFilePath == null) return;

        var footer = new StringBuilder();
        footer.AppendLine();
        footer.AppendLine("═══════════════════════════════════════════════════════");
        footer.AppendLine("  RESUMO FINAL");
        footer.AppendLine($"  Total: {_totalOps} | Sucessos: {_successOps} | Falhas: {_failOps}");
        footer.AppendLine($"  Término: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture)}");
        footer.AppendLine($"  Arquivo: {_logFilePath}");
        footer.AppendLine("═══════════════════════════════════════════════════════");

        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logFilePath, footer.ToString(), Encoding.UTF8);
            }
            catch { }
        }
    }

    /// <summary>
    /// Retorna o caminho do arquivo de log atual, ou null se não foi iniciado.
    /// </summary>
    public static string? CurrentLogPath => _logFilePath;
}
