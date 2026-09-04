using FluentAssertions;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Tests.Utils;

[Collection("AuditLoggerTests")]
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
        AuditLogger.Start(_basePath, AuditType.Uninstall);
        var logPath = AuditLogger.CurrentLogPath!;
        AuditLogger.Finish();

        var content = File.ReadAllText(logPath);
        content.Should().Contain("AUDITORIA DE DESINSTALAÇÃO");
        content.Should().Contain(_basePath);
    }

    [Fact]
    public void Start_WithDifferentAuditTypes_GeneratesCorrectPrefixAndHeader()
    {
        // Install
        AuditLogger.Start(_basePath, AuditType.Install);
        var installPath = AuditLogger.CurrentLogPath!;
        AuditLogger.Finish();
        Path.GetFileName(installPath).Should().StartWith("install_audit_");
        File.ReadAllText(installPath).Should().Contain("AUDITORIA DE INSTALAÇÃO");

        // Maintenance
        AuditLogger.Start(_basePath, AuditType.Maintenance);
        var maintPath = AuditLogger.CurrentLogPath!;
        AuditLogger.Finish();
        Path.GetFileName(maintPath).Should().StartWith("maintenance_audit_");
        File.ReadAllText(maintPath).Should().Contain("AUDITORIA DE MANUTENÇÃO");
    }

    [Fact]
    public void Start_ResetCounters()
    {
        AuditLogger.Start(_basePath);
        AuditLogger.Log("Test", "op1", true);
        AuditLogger.Log("Test", "op2", false);

        // Novo Start deve fechar e zerar
        AuditLogger.Start(_basePath);

        var logPath = AuditLogger.CurrentLogPath!;
        AuditLogger.Finish();
        var content = File.ReadAllText(logPath);
        content.Should().Contain("Total: 0");
    }

    [Fact]
    public void Log_AppendsEntryToFile()
    {
        AuditLogger.Start(_basePath);
        AuditLogger.Log("OperacaoTeste", "ItemTeste", true);
        var logPath = AuditLogger.CurrentLogPath!;
        AuditLogger.Finish();

        var content = File.ReadAllText(logPath);
        content.Should().Contain("OperacaoTeste");
        content.Should().Contain("ItemTeste");
        content.Should().Contain("OK");
    }

    [Fact]
    public void Log_WithoutStart_DoesNothing()
    {
        // Should not throw when called without Start
        AuditLogger.Finish();
        AuditLogger.Log("Test", "item", true);
    }

    [Fact]
    public void Log_SuccessAndFailure_DifferentIcons()
    {
        AuditLogger.Start(_basePath);
        AuditLogger.Log("Op1", "Item1", true);
        AuditLogger.Log("Op2", "Item2", false);
        var logPath = AuditLogger.CurrentLogPath!;
        AuditLogger.Finish();

        var content = File.ReadAllText(logPath);
        content.Should().Contain("✅");
        content.Should().Contain("❌");
    }

    [Fact]
    public void Separator_WritesToFile()
    {
        AuditLogger.Start(_basePath);
        AuditLogger.Separator("Minha Seção");
        var logPath = AuditLogger.CurrentLogPath!;
        AuditLogger.Finish();

        var content = File.ReadAllText(logPath);
        content.Should().Contain("Minha Seção");
    }

    [Fact]
    public void Separator_WithoutStart_DoesNothing()
    {
        // Should not throw
        AuditLogger.Finish();
        AuditLogger.Separator("Teste");
    }

    [Fact]
    public void Finish_WritesFooterWithStatsAndResetsState()
    {
        AuditLogger.Start(_basePath);
        AuditLogger.Log("Op1", "Item1", true);
        AuditLogger.Log("Op2", "Item2", true);
        AuditLogger.Log("Op3", "Item3", false);

        var logPath = AuditLogger.CurrentLogPath;
        AuditLogger.Finish();

        // Após Finish, CurrentLogPath deve ser null
        AuditLogger.CurrentLogPath.Should().BeNull();

        var content = File.ReadAllText(logPath!);
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
        AuditLogger.Finish();
    }

    public void Dispose()
    {
        AuditLogger.Finish();
    }
}
