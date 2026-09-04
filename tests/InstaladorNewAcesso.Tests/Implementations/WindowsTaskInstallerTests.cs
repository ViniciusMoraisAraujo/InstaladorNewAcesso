using FluentAssertions;
using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Core.Implementations;
using NSubstitute;
using Xunit;

namespace InstaladorNewAcesso.Tests.Implementations;

public class WindowsTaskInstallerTests : IDisposable
{
    private readonly IProcessExecutor _executor;
    private readonly WindowsTaskInstaller _installer;
    private readonly string _tempFile;

    public WindowsTaskInstallerTests()
    {
        _executor = Substitute.For<IProcessExecutor>();
        _installer = new WindowsTaskInstaller(_executor);
        _tempFile = Path.Combine(Path.GetTempPath(), $"TaskTest_{Guid.NewGuid():N}.exe");
        File.WriteAllText(_tempFile, "fake binary content");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            try { File.Delete(_tempFile); } catch { }
        }
    }

    [Fact]
    public async Task InstallTaskAsync_WhenExecutableDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "does_not_exist_12345.exe");

        // Act
        var result = await _installer.InstallTaskAsync("MinhaTarefa", nonExistentPath, "10");

        // Assert
        result.Should().BeFalse();
        await _executor.DidNotReceive().RunPowerShellCommandAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task InstallTaskAsync_WhenSchtasksSucceeds_ReturnsTrue()
    {
        // Arrange
        _executor.RunPowerShellCommandAsync(
            Arg.Is<string>(args => args.Contains("/delete")),
            Arg.Any<string>()).Returns(true);

        _executor.RunPowerShellCommandAsync(
            Arg.Is<string>(args => args.Contains("/create")),
            Arg.Any<string>()).Returns(true);

        // Act
        var result = await _installer.InstallTaskAsync("MinhaTarefa", _tempFile, "5");

        // Assert
        result.Should().BeTrue();
        await _executor.Received(1).RunPowerShellCommandAsync(
            Arg.Is<string>(args => args.Contains("schtasks.exe /delete /tn \"MinhaTarefa\" /f")),
            Arg.Is<string>(f => f.Contains("Remover Tarefa")));

        await _executor.Received(1).RunPowerShellCommandAsync(
            Arg.Is<string>(args => args.Contains("schtasks.exe /create /tn \"MinhaTarefa\"") && args.Contains("/sc minute /mo 5")),
            Arg.Is<string>(f => f.Contains("Criar Tarefa")));
    }

    [Fact]
    public async Task InstallTaskAsync_WhenSchtasksFails_ReturnsFalse()
    {
        // Arrange
        _executor.RunPowerShellCommandAsync(
            Arg.Is<string>(args => args.Contains("/create")),
            Arg.Any<string>()).Returns(false);

        // Act
        var result = await _installer.InstallTaskAsync("MinhaTarefa", _tempFile, "5");

        // Assert
        result.Should().BeFalse();
    }
}
