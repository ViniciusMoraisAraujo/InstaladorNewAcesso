using System.Globalization;
using System.Text;
using InstaladorNewAcesso.Abstractions.Models;

namespace InstaladorNewAcesso.Core.Utils;

/// <summary>
/// Logger estático de auditoria para operações de instalação, desinstalação e manutenção.
/// Thread-safe e formatado com CultureInfo.InvariantCulture.
/// Utiliza StreamWriter com buffer aberto durante a sessão para reduzir I/O.
/// </summary>
public static class AuditLogger
{
    private static string? _logFilePath;
    private static readonly object _lock = new();
    private static int _totalOps;
    private static int _successOps;
    private static int _failOps;
    private static StreamWriter? _writer;

    private static readonly Dictionary<AuditType, (string FilePrefix, string HeaderTitle)> s_auditTypeMap = new()
    {
        [AuditType.Install] = ("install_audit", "AUDITORIA DE INSTALAÇÃO - NEW ACESSO"),
        [AuditType.Uninstall] = ("uninstall_audit", "AUDITORIA DE DESINSTALAÇÃO - NEW ACESSO"),
        [AuditType.Maintenance] = ("maintenance_audit", "AUDITORIA DE MANUTENÇÃO - NEW ACESSO")
    };

    /// <summary>
    /// Inicializa o log de auditoria, criando o arquivo com cabeçalho dinâmico baseado no tipo de operação.
    /// </summary>
    public static void Start(string basePath, AuditType auditType = AuditType.Uninstall)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);

        lock (_lock)
        {
            // Fechar sessão anterior, se existir
            CloseWriter();

            var logDir = Path.Combine(Path.GetTempPath(), "InstaladorNewAcesso", "Logs");
            Directory.CreateDirectory(logDir);

            var (filePrefix, headerTitle) = s_auditTypeMap[auditType];
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            _logFilePath = Path.Combine(logDir, $"{filePrefix}_{timestamp}.txt");
            _totalOps = 0;
            _successOps = 0;
            _failOps = 0;

            var now = DateTime.Now;
            var header = new StringBuilder();
            header.AppendLine("═══════════════════════════════════════════════════════");
            header.AppendLine($"  {headerTitle}");
            header.AppendLine(string.Create(CultureInfo.InvariantCulture, $"  Data: {now:dd/MM/yyyy HH:mm:ss}"));
            header.AppendLine(string.Create(CultureInfo.InvariantCulture, $"  Caminho base: {basePath}"));
            header.AppendLine(string.Create(CultureInfo.InvariantCulture, $"  Máquina: {Environment.MachineName}"));
            header.AppendLine(string.Create(CultureInfo.InvariantCulture, $"  Usuário: {Environment.UserName}"));
            header.AppendLine("═══════════════════════════════════════════════════════");
            header.AppendLine();

            try
            {
                var fs = new FileStream(_logFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                _writer = new StreamWriter(fs, Encoding.UTF8)
                {
                    AutoFlush = false
                };
                _writer.Write(header.ToString());
                _writer.Flush();
            }
            catch
            {
                // Silently ignore initial write errors in non-writable environments
                CloseWriter();
            }
        }
    }

    /// <summary>
    /// Registra uma operação no log de auditoria.
    /// </summary>
    public static void Log(string operacao, string item, bool sucesso, string? detalhe = null)
    {
        lock (_lock)
        {
            if (_writer == null) return;

            _totalOps++;
            if (sucesso) _successOps++; else _failOps++;

            var timestamp = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            var status = sucesso ? "  ✅ OK  " : "  ❌ FALHA";
            var line = string.Create(CultureInfo.InvariantCulture, $"[{timestamp}] {status} | {operacao,-35} | {item}");

            if (!string.IsNullOrWhiteSpace(detalhe))
                line += $" | {detalhe}";

            try
            {
                _writer.WriteLine(line);
            }
            catch
            {
                // Silently ignore append errors
            }
        }
    }

    /// <summary>
    /// Registra uma linha de separação no log.
    /// </summary>
    public static void Separator(string titulo)
    {
        lock (_lock)
        {
            if (_writer == null) return;

            try
            {
                _writer.WriteLine();
                _writer.WriteLine($"--- {titulo} ---");
                _writer.WriteLine();
            }
            catch
            {
                // Silently ignore append errors
            }
        }
    }

    /// <summary>
    /// Registra o resumo final com base nos contadores internos, encerra o log e reseta o estado.
    /// </summary>
    public static void Finish()
    {
        lock (_lock)
        {
            if (_writer == null) return;

            var footer = new StringBuilder();
            footer.AppendLine();
            footer.AppendLine("═══════════════════════════════════════════════════════");
            footer.AppendLine("  RESUMO FINAL");
            footer.AppendLine(string.Create(CultureInfo.InvariantCulture, $"  Total: {_totalOps} | Sucessos: {_successOps} | Falhas: {_failOps}"));
            footer.AppendLine(string.Create(CultureInfo.InvariantCulture, $"  Término: {DateTime.Now:dd/MM/yyyy HH:mm:ss}"));
            footer.AppendLine(string.Create(CultureInfo.InvariantCulture, $"  Arquivo: {_logFilePath}"));
            footer.AppendLine("═══════════════════════════════════════════════════════");

            try
            {
                _writer.Write(footer.ToString());
            }
            catch
            {
                // Silently ignore append errors
            }

            CloseWriter();
            _totalOps = 0;
            _successOps = 0;
            _failOps = 0;
        }
    }

    /// <summary>
    /// Retorna o caminho do arquivo de log atual, ou null se não foi iniciado ou já finalizado.
    /// </summary>
    public static string? CurrentLogPath
    {
        get
        {
            lock (_lock)
            {
                return _logFilePath;
            }
        }
    }

    /// <summary>
    /// Fecha o StreamWriter e reseta o caminho do log. Safety net para garantir cleanup.
    /// </summary>
    private static void CloseWriter()
    {
        try
        {
            _writer?.Flush();
            _writer?.Dispose();
        }
        catch
        {
            // Best-effort cleanup
        }
        finally
        {
            _writer = null;
            _logFilePath = null;
        }
    }
}
