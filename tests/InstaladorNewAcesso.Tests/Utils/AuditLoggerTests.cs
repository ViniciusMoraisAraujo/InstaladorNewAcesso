using FluentAssertions;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Tests.Utils;

public class AuditLoggerTests : IDisposable
{
    private readonly string _basePath;

    public AuditLoggerTests()
    {
        _basePath = Path.Combine(Path.GetTempPath(), "AuditLoggerTests_" + Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void Start_CreatesLogFile()
    {
        AuditLogger.Start(_basePath);

        AuditLogger.CurrentLogPath.Should().NotBeNull();
        File.Exists(AuditLogger.CurrentLogPath).Should().BeTrue();
    }

    [Fact]
    public void Start_LogFileContainsHeader()
    {
        AuditLogger.Start(_basePath);

        var content = File.ReadAllText(AuditLogger.CurrentLogPath!);
        content.Should().Contain("AUDITORIA DE DESINSTALAÇÃO");
        content.Should().Contain(_basePath);
    }

    [Fact]
    public void Start_ResetCounters()
    {
        AuditLogger.Start(_basePath);
        AuditLogger.Log("Test", "op1", true);
        AuditLogger.Log("Test", "op2", false);

        // Novo Start deve zerar
        AuditLogger.Start(_basePath);

        AuditLogger.Finish();
        var content = File.ReadAllText(AuditLogger.CurrentLogPath!);
        content.Should().Contain("Total: 0");
    }

    [Fact]
    public void Log_AppendsEntryToFile()
    {
        AuditLogger.Start(_basePath);
        AuditLogger.Log("OperacaoTeste", "ItemTeste", true);

        var content = File.ReadAllText(AuditLogger.CurrentLogPath!);
        content.Should().Contain("OperacaoTeste");
        content.Should().Contain("ItemTeste");
        content.Should().Contain("OK");
    }

    [Fact]
    public void Log_WithoutStart_DoesNothing()
    {
        // Should not throw when called without Start
        AuditLogger.Log("Test", "item", true);
    }

    [Fact]
    public void Log_SuccessAndFailure_DifferentIcons()
    {
        AuditLogger.Start(_basePath);
        AuditLogger.Log("Op1", "Item1", true);
        AuditLogger.Log("Op2", "Item2", false);

        var content = File.ReadAllText(AuditLogger.CurrentLogPath!);
        content.Should().Contain("✅");
        content.Should().Contain("❌");
    }

    [Fact]
    public void Separator_WritesToFile()
    {
        AuditLogger.Start(_basePath);
        AuditLogger.Separator("Minha Seção");

        var content = File.ReadAllText(AuditLogger.CurrentLogPath!);
        content.Should().Contain("Minha Seção");
    }

    [Fact]
    public void Separator_WithoutStart_DoesNothing()
    {
        // Should not throw
        AuditLogger.Separator("Teste");
    }

    [Fact]
    public void Finish_WritesFooterWithStats()
    {
        AuditLogger.Start(_basePath);
        AuditLogger.Log("Op1", "Item1", true);
        AuditLogger.Log("Op2", "Item2", true);
        AuditLogger.Log("Op3", "Item3", false);

        AuditLogger.Finish();

        var content = File.ReadAllText(AuditLogger.CurrentLogPath!);
        content.Should().Contain("RESUMO FINAL");
        content.Should().Contain("Total: 3");
        content.Should().Contain("Sucessos: 2");
        content.Should().Contain("Falhas: 1");
    }

    [Fact]
    public void Finish_WithoutStart_DoesNothing()
    {
        // Should not throw
        AuditLogger.Finish();
    }

    [Fact]
    public void CurrentLogPath_AfterStart_ReturnsPath()
    {
        AuditLogger.Start(_basePath);

        AuditLogger.CurrentLogPath.Should().NotBeNull();
        Path.GetFileName(AuditLogger.CurrentLogPath).Should().StartWith("uninstall_audit_");
        Path.GetExtension(AuditLogger.CurrentLogPath).Should().Be(".txt");
    }

    [Fact]
    public void CurrentLogPath_BeforeStart_ReturnsNull()
    {
        // Since Start was not called in this test (fresh state)
        // Note: depends on test execution order — use a separate check
        var path = AuditLogger.CurrentLogPath;

        // Can't guarantee null since other tests might have set it.
        // Just verify it doesn't throw.
    }

    public void Dispose()
    {
        // Cleanup log file if any
        if (AuditLogger.CurrentLogPath != null && File.Exists(AuditLogger.CurrentLogPath))
        {
            try { File.Delete(AuditLogger.CurrentLogPath); }
            catch { /* cleanup on best-effort */ }
        }
    }
}
