using FluentAssertions;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Tests.Utils;

public class ConfigBackupServiceTests : IDisposable
{
    private readonly string _tempRoot;

    public ConfigBackupServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "ConfigBackupTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    // ── Backup ──────────────────────────────

    [Fact]
    public void Backup_TargetDirectoryNotExists_ReturnsNull()
    {
        var nonExistent = Path.Combine(_tempRoot, "NonExistent");

        var result = ConfigBackupService.Backup(nonExistent, "TestApp");

        result.Should().BeNull();
    }

    [Fact]
    public void Backup_NoConfigFiles_ReturnsNull()
    {
        File.WriteAllText(Path.Combine(_tempRoot, "readme.txt"), "hello");

        var result = ConfigBackupService.Backup(_tempRoot, "TestApp");

        result.Should().BeNull();
    }

    [Fact]
    public void Backup_HasConfigFiles_ReturnsBackupPath()
    {
        File.WriteAllText(Path.Combine(_tempRoot, "app.exe.config"), "<xml/>");
        File.WriteAllText(Path.Combine(_tempRoot, "settings.ini"), "key=value");
        File.WriteAllText(Path.Combine(_tempRoot, "data.xml"), "<data/>");

        var result = ConfigBackupService.Backup(_tempRoot, "TestApp");

        result.Should().NotBeNull();
        Directory.Exists(result).Should().BeTrue();
        Directory.GetFiles(result).Should().HaveCount(3);
    }

    [Fact]
    public void Backup_CopiesOnlyConfigFiles_NotOtherFiles()
    {
        File.WriteAllText(Path.Combine(_tempRoot, "app.exe.config"), "<xml/>");
        File.WriteAllText(Path.Combine(_tempRoot, "data.dll"), "binary");
        File.WriteAllText(Path.Combine(_tempRoot, "data.txt"), "text");

        var result = ConfigBackupService.Backup(_tempRoot, "TestApp");

        result.Should().NotBeNull();
        Directory.GetFiles(result).Should().ContainSingle(f => f.EndsWith(".config"));
    }

    // ── Restore ─────────────────────────────

    [Fact]
    public void Restore_NullBackupPath_DoesNothing()
    {
        var targetDir = Path.Combine(_tempRoot, "Target");
        Directory.CreateDirectory(targetDir);

        // Should not throw
        ConfigBackupService.Restore(null, targetDir);
    }

    [Fact]
    public void Restore_NonExistentBackupPath_DoesNothing()
    {
        var targetDir = Path.Combine(_tempRoot, "Target");
        Directory.CreateDirectory(targetDir);

        ConfigBackupService.Restore(Path.Combine(_tempRoot, "NonExistent"), targetDir);
    }

    [Fact]
    public void Restore_CopiesFilesToTarget()
    {
        // Setup: backup
        var backupDir = Path.Combine(_tempRoot, "Backup");
        Directory.CreateDirectory(backupDir);
        File.WriteAllText(Path.Combine(backupDir, "app.exe.config"), "config content");

        // Setup: target (empty)
        var targetDir = Path.Combine(_tempRoot, "Target");
        Directory.CreateDirectory(targetDir);

        ConfigBackupService.Restore(backupDir, targetDir);

        File.ReadAllText(Path.Combine(targetDir, "app.exe.config")).Should().Be("config content");
    }

    [Fact]
    public void Restore_CreatesTargetDirectory_IfMissing()
    {
        var backupDir = Path.Combine(_tempRoot, "Backup");
        Directory.CreateDirectory(backupDir);
        File.WriteAllText(Path.Combine(backupDir, "app.exe.config"), "content");

        var targetDir = Path.Combine(_tempRoot, "NewTarget");

        ConfigBackupService.Restore(backupDir, targetDir);

        Directory.Exists(targetDir).Should().BeTrue();
        File.Exists(Path.Combine(targetDir, "app.exe.config")).Should().BeTrue();
    }

    [Fact]
    public void Restore_OverwritesExistingFiles()
    {
        var backupDir = Path.Combine(_tempRoot, "Backup");
        Directory.CreateDirectory(backupDir);
        File.WriteAllText(Path.Combine(backupDir, "app.exe.config"), "new content");

        var targetDir = Path.Combine(_tempRoot, "Target");
        Directory.CreateDirectory(targetDir);
        File.WriteAllText(Path.Combine(targetDir, "app.exe.config"), "old content");

        ConfigBackupService.Restore(backupDir, targetDir);

        File.ReadAllText(Path.Combine(targetDir, "app.exe.config")).Should().Be("new content");
    }

    // ── Cleanup ─────────────────────────────

    [Fact]
    public void Cleanup_NullPath_DoesNothing()
    {
        // Should not throw
        ConfigBackupService.Cleanup(null);
    }

    [Fact]
    public void Cleanup_NonExistentPath_DoesNothing()
    {
        ConfigBackupService.Cleanup(Path.Combine(_tempRoot, "NonExistent"));
    }

    [Fact]
    public void Cleanup_RemovesDirectory()
    {
        var backupDir = Path.Combine(_tempRoot, "ToDelete");
        Directory.CreateDirectory(backupDir);
        File.WriteAllText(Path.Combine(backupDir, "file.config"), "content");

        ConfigBackupService.Cleanup(backupDir);

        Directory.Exists(backupDir).Should().BeFalse();
    }

    // ── Integration: Backup → Restore → Cleanup ──

    [Fact]
    public void BackupRestoreCleanup_FullFlow()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempRoot, "app.exe.config"), "my config");
        var restoreDir = Path.Combine(_tempRoot, "Restored");

        // Act: backup
        var backupPath = ConfigBackupService.Backup(_tempRoot, "IntegrationTest");
        backupPath.Should().NotBeNull();
        Directory.GetFiles(backupPath).Should().ContainSingle(f => f.EndsWith(".config"));

        // Delete original to simulate uninstall
        File.Delete(Path.Combine(_tempRoot, "app.exe.config"));

        // Act: restore
        ConfigBackupService.Restore(backupPath, restoreDir);
        File.ReadAllText(Path.Combine(restoreDir, "app.exe.config")).Should().Be("my config");

        // Act: cleanup
        ConfigBackupService.Cleanup(backupPath);
        Directory.Exists(backupPath).Should().BeFalse();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, true); }
            catch { /* cleanup on best-effort */ }
        }
    }
}
