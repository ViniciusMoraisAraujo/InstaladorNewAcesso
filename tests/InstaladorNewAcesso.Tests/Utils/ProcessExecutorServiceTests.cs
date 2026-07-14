using FluentAssertions;
using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Tests.Utils;

/// <summary>
/// Testes de integração para ProcessExecutorService.
/// Como a classe delega diretamente para ProcessExecutor estático
/// (métodos não-virtuais), testamos com comandos PowerShell reais.
/// </summary>
public class ProcessExecutorServiceTests
{
    private readonly ProcessExecutorService _service = new();

    // ============================================================
    //  Interface implementation
    // ============================================================

    [Fact]
    public void ImplementsIProcessExecutor()
    {
        _service.Should().BeAssignableTo<IProcessExecutor>();
    }

    // ============================================================
    //  RunPowerShellCommandAsync
    // ============================================================

    [Fact]
    public async Task RunPowerShellCommandAsync_SimpleCommand_ReturnsTrue()
    {
        var result = await _service.RunPowerShellCommandAsync(
            "Write-Output 'hello'", "TestCommand");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task RunPowerShellCommandAsync_ExitCode1_ReturnsFalse()
    {
        var result = await _service.RunPowerShellCommandAsync(
            "exit 1", "FailingCommand");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RunPowerShellCommandAsync_EmptyArguments_Succeeds()
    {
        var result = await _service.RunPowerShellCommandAsync(
            "-Command \"exit 0\"", "EmptyTest");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task RunPowerShellCommandAsync_InvalidCommand_ReturnsFalse()
    {
        var result = await _service.RunPowerShellCommandAsync(
            "Invalid-CommandThatDoesNotExist", "InvalidCmd");

        result.Should().BeFalse();
    }

    // ============================================================
    //  RunPowerShellWithOutputAsync
    // ============================================================

    [Fact]
    public async Task RunPowerShellWithOutputAsync_SimpleOutput_ReturnsTrimmedOutput()
    {
        var result = await _service.RunPowerShellWithOutputAsync(
            "-Command \"Write-Output 'Hello World'\"");

        result.Should().Be("Hello World");
    }

    [Fact]
    public async Task RunPowerShellWithOutputAsync_OutputWithWhitespace_IsTrimmed()
    {
        var result = await _service.RunPowerShellWithOutputAsync(
            "-Command \"Write-Output '  spaces  '\"");

        result.Should().Be("spaces");
    }

    [Fact]
    public async Task RunPowerShellWithOutputAsync_EmptyOutput_ReturnsEmpty()
    {
        var result = await _service.RunPowerShellWithOutputAsync(
            "-Command \"Write-Output ''\"");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RunPowerShellWithOutputAsync_FailingCommand_ReturnsEmpty()
    {
        var result = await _service.RunPowerShellWithOutputAsync(
            "-Command \"exit 1\"");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RunPowerShellWithOutputAsync_InvalidCommand_ReturnsEmpty()
    {
        var result = await _service.RunPowerShellWithOutputAsync(
            "-Command \"Invalid-CommandThatDoesNotExist\"");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RunPowerShellWithOutputAsync_MultiLineOutput_ReturnsTrimmedResult()
    {
        var result = await _service.RunPowerShellWithOutputAsync(
            "-Command \"Write-Output 'line1'; Write-Output 'line2'\"");

        // Output should contain both lines (trimmed)
        result.Should().Contain("line1");
        result.Should().Contain("line2");
    }

    [Fact]
    public async Task RunPowerShellWithOutputAsync_EnvironmentVariable_ReturnsValue()
    {
        var result = await _service.RunPowerShellWithOutputAsync(
            "-Command \"Write-Output $env:COMPUTERNAME\"");

        result.Should().NotBeNullOrWhiteSpace();
    }
}
