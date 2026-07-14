using FluentAssertions;
using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Core.Services;
using NSubstitute;

namespace InstaladorNewAcesso.Tests.Services;

public class MsiUninstallerTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly IProcessExecutor _executor;
    private readonly MsiUninstaller _uninstaller;

    public MsiUninstallerTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "MsiUninstallerTests_" + Guid.NewGuid().ToString("N"));
        _executor = Substitute.For<IProcessExecutor>();
        _uninstaller = new MsiUninstaller(_executor);
    }

    // ============================================================
    //  IsInstalled
    // ============================================================

    [Fact]
    public void IsInstalled_DirectoryNotExists_ReturnsFalse()
    {
        var result = MsiUninstaller.IsInstalled(Path.Combine(_tempRoot, "NonExistent"));

        result.Should().BeFalse();
    }

    [Fact]
    public void IsInstalled_DirectoryExistsButEmpty_ReturnsFalse()
    {
        Directory.CreateDirectory(_tempRoot);

        var result = MsiUninstaller.IsInstalled(_tempRoot);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsInstalled_DirectoryExistsWithFiles_ReturnsTrue()
    {
        Directory.CreateDirectory(_tempRoot);
        File.WriteAllText(Path.Combine(_tempRoot, "file.txt"), "content");

        var result = MsiUninstaller.IsInstalled(_tempRoot);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsInstalled_DirectoryExistsWithSubdirectories_ReturnsTrue()
    {
        Directory.CreateDirectory(Path.Combine(_tempRoot, "subdir"));

        var result = MsiUninstaller.IsInstalled(_tempRoot);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsInstalled_EmptyString_ReturnsFalse()
    {
        MsiUninstaller.IsInstalled("").Should().BeFalse();
    }

    [Fact]
    public void IsInstalled_Null_ReturnsFalse()
    {
        MsiUninstaller.IsInstalled(null!).Should().BeFalse();
    }

    // ============================================================
    //  IsRegisteredAsync
    // ============================================================

    [Fact]
    public async Task IsRegisteredAsync_EmptyPath_ReturnsFalse()
    {
        var result = await _uninstaller.IsRegisteredAsync("");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRegisteredAsync_WhitespacePath_ReturnsFalse()
    {
        var result = await _uninstaller.IsRegisteredAsync("   ");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRegisteredAsync_NullPath_ReturnsFalse()
    {
        var result = await _uninstaller.IsRegisteredAsync(null!);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRegisteredAsync_WhenPowerShellReturnsOutput_ReturnsTrue()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>())
            .Returns("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{ABC}");

        var result = await _uninstaller.IsRegisteredAsync(@"C:\NewAcesso\Something");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsRegisteredAsync_WhenPowerShellReturnsEmpty_ReturnsFalse()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>())
            .Returns(string.Empty);

        var result = await _uninstaller.IsRegisteredAsync(@"C:\NewAcesso\Something");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRegisteredAsync_WhenPowerShellThrows_ThrowsException()
    {
        // MsiUninstaller.IsRegisteredAsync does NOT have try/catch,
        // so exceptions from the executor propagate to the caller.
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>())
            .Returns(Task.FromException<string>(new InvalidOperationException("PowerShell error")));

        var act = async () => await _uninstaller.IsRegisteredAsync(@"C:\NewAcesso\Something");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task IsRegisteredAsync_PassesCommandWithRegistryPath()
    {
        var targetDir = @"C:\NewAcesso\MyApp";
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns(string.Empty);

        await _uninstaller.IsRegisteredAsync(targetDir);

        await _executor.Received(1).RunPowerShellWithOutputAsync(
            Arg.Is<string>(s =>
                s.Contains("HKLM:") &&
                s.Contains("Uninstall") &&
                s.Contains("InstallLocation")));
    }

    [Fact]
    public async Task IsRegisteredAsync_DoesNotDoubleBackslashesInCommand()
    {
        // BUG FIX: In single-quoted PowerShell strings, backslashes are literal.
        // C:\NewAcesso must NOT be escaped to C:\\NewAcesso.
        var targetDir = @"C:\NewAcesso\MyApp";
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns(string.Empty);

        await _uninstaller.IsRegisteredAsync(targetDir);

        await _executor.Received(1).RunPowerShellWithOutputAsync(
            Arg.Is<string>(s =>
                s.Contains(@"C:\NewAcesso") &&
                !s.Contains(@"C:\\NewAcesso")));
    }

    // ============================================================
    //  UninstallByMsiPathAsync
    // ============================================================

    [Fact]
    public async Task UninstallByMsiPathAsync_NonExistentMsi_ReturnsFalse()
    {
        var result = await MsiUninstaller.UninstallByMsiPathAsync(@"C:\NonExistent\app.msi");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UninstallByMsiPathAsync_EmptyPath_ReturnsFalse()
    {
        var result = await MsiUninstaller.UninstallByMsiPathAsync("");

        result.Should().BeFalse();
    }

    // ============================================================
    //  RemoveTargetDirectory
    // ============================================================

    [Fact]
    public void RemoveTargetDirectory_DirectoryNotExists_ReturnsFalse()
    {
        var result = MsiUninstaller.RemoveTargetDirectory(Path.Combine(_tempRoot, "NonExistent"));

        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveTargetDirectory_DirectoryExists_RemovesAndReturnsTrue()
    {
        Directory.CreateDirectory(_tempRoot);
        File.WriteAllText(Path.Combine(_tempRoot, "file.txt"), "content");

        var result = MsiUninstaller.RemoveTargetDirectory(_tempRoot);

        result.Should().BeTrue();
        Directory.Exists(_tempRoot).Should().BeFalse();
    }

    [Fact]
    public void RemoveTargetDirectory_DirectoryWithSubdirectories_RemovesRecursively()
    {
        var subDir = Path.Combine(_tempRoot, "sub", "deep");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "deep.txt"), "content");
        File.WriteAllText(Path.Combine(_tempRoot, "root.txt"), "content");

        var result = MsiUninstaller.RemoveTargetDirectory(_tempRoot);

        result.Should().BeTrue();
        Directory.Exists(_tempRoot).Should().BeFalse();
    }

    [Fact]
    public void RemoveTargetDirectory_EmptyDirectory_ReturnsTrue()
    {
        Directory.CreateDirectory(_tempRoot);

        var result = MsiUninstaller.RemoveTargetDirectory(_tempRoot);

        result.Should().BeTrue();
        Directory.Exists(_tempRoot).Should().BeFalse();
    }

    [Fact]
    public void RemoveTargetDirectory_EmptyString_ReturnsFalse()
    {
        MsiUninstaller.RemoveTargetDirectory("").Should().BeFalse();
    }

    [Fact]
    public void RemoveTargetDirectory_Null_ReturnsFalse()
    {
        MsiUninstaller.RemoveTargetDirectory(null!).Should().BeFalse();
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
