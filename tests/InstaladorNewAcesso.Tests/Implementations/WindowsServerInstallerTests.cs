using FluentAssertions;
using InstaladorNewAcesso.Core.Implementations;
using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InstaladorNewAcesso.Tests.Implementations;

public class WindowsServerInstallerTests
{
    private readonly IProcessExecutor _executor;
    private readonly WindowsServerInstaller _installer;

    public WindowsServerInstallerTests()
    {
        _executor = Substitute.For<IProcessExecutor>();
        _installer = new WindowsServerInstaller(_executor);
    }

    // ============================================================
    //  IsFeatureInstalledAsync
    // ============================================================

    [Fact]
    public async Task IsFeatureInstalledAsync_OutputTrue_ReturnsTrue()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("True");

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        var result = await _installer.IsFeatureInstalledAsync(feature);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFeatureInstalledAsync_OutputFalse_ReturnsFalse()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("False");

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        var result = await _installer.IsFeatureInstalledAsync(feature);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsFeatureInstalledAsync_OutputTrueCaseInsensitive_ReturnsTrue()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("true");

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        var result = await _installer.IsFeatureInstalledAsync(feature);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFeatureInstalledAsync_OutputWithWhitespace_ReturnsTrue()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("  True  ");

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        var result = await _installer.IsFeatureInstalledAsync(feature);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFeatureInstalledAsync_EmptyOutput_ReturnsFalse()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns(string.Empty);

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        var result = await _installer.IsFeatureInstalledAsync(feature);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsFeatureInstalledAsync_PassesServerNameInCommand()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("False");

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        await _installer.IsFeatureInstalledAsync(feature);

        await _executor.Received(1).RunPowerShellWithOutputAsync(
            Arg.Is<string>(s => s.Contains("Web-Asp-Net45")));
    }

    [Fact]
    public async Task IsFeatureInstalledAsync_DoesNotUseDesktopName()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("False");

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        await _installer.IsFeatureInstalledAsync(feature);

        await _executor.Received(1).RunPowerShellWithOutputAsync(
            Arg.Is<string>(s => !s.Contains("IIS-ASPNET45")));
    }

    [Fact]
    public async Task IsFeatureInstalledAsync_PowerShellThrows_PropagatesException()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("PowerShell failed"));

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        var act = async () => await _installer.IsFeatureInstalledAsync(feature);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ============================================================
    //  CheckFeaturesInstalledAsync
    // ============================================================

    [Fact]
    public async Task CheckFeaturesInstalledAsync_SingleInstalled_ReturnsCorrectResult()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>())
            .Returns("Web-Asp-Net45|True");

        var features = new List<WindowsFeature>
        {
            new("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45")
        };

        var result = await _installer.CheckFeaturesInstalledAsync(features);

        result.Should().HaveCount(1);
        result[0].Feature.Should().Be(features[0]);
        result[0].IsInstalled.Should().BeTrue();
    }

    [Fact]
    public async Task CheckFeaturesInstalledAsync_SingleNotInstalled_ReturnsCorrectResult()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>())
            .Returns("Web-Asp-Net45|False");

        var features = new List<WindowsFeature>
        {
            new("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45")
        };

        var result = await _installer.CheckFeaturesInstalledAsync(features);

        result.Should().HaveCount(1);
        result[0].IsInstalled.Should().BeFalse();
    }

    [Fact]
    public async Task CheckFeaturesInstalledAsync_MultipleFeatures_ParsesAll()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>())
            .Returns("Web-Asp-Net45|True\nNET-Framework-45-AdvSrvs|False\nMSMQ-Services|True");

        var features = new List<WindowsFeature>
        {
            new("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45"),
            new(".NET 4.5 Advanced Services", "NET-Framework-45-AdvSrvs", "IIS-NetFx45-AdvSrvs"),
            new("MSMQ Services", "MSMQ-Services", "MSMQ-Container")
        };

        var result = await _installer.CheckFeaturesInstalledAsync(features);

        result.Should().HaveCount(3);
        result[0].IsInstalled.Should().BeTrue();
        result[1].IsInstalled.Should().BeFalse();
        result[2].IsInstalled.Should().BeTrue();
    }

    [Fact]
    public async Task CheckFeaturesInstalledAsync_EmptyOutput_AllFalse()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns(string.Empty);

        var features = new List<WindowsFeature>
        {
            new("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45")
        };

        var result = await _installer.CheckFeaturesInstalledAsync(features);

        result.Should().HaveCount(1);
        result[0].IsInstalled.Should().BeFalse();
    }

    [Fact]
    public async Task CheckFeaturesInstalledAsync_NullOutput_AllFalse()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns((string)null!);

        var features = new List<WindowsFeature>
        {
            new("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45")
        };

        var result = await _installer.CheckFeaturesInstalledAsync(features);

        result.Should().HaveCount(1);
        result[0].IsInstalled.Should().BeFalse();
    }

    [Fact]
    public async Task CheckFeaturesInstalledAsync_FeatureNotInOutput_DefaultsFalse()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>())
            .Returns("Web-Other|True");

        var features = new List<WindowsFeature>
        {
            new("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45")
        };

        var result = await _installer.CheckFeaturesInstalledAsync(features);

        result.Should().HaveCount(1);
        result[0].IsInstalled.Should().BeFalse();
    }

    [Fact]
    public async Task CheckFeaturesInstalledAsync_EmptyList_ReturnsEmpty()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns(string.Empty);

        var result = await _installer.CheckFeaturesInstalledAsync(new List<WindowsFeature>());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckFeaturesInstalledAsync_OutputWithExtraWhitespace_ParsesCorrectly()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>())
            .Returns("  Web-Asp-Net45  |  True  ");

        var features = new List<WindowsFeature>
        {
            new("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45")
        };

        var result = await _installer.CheckFeaturesInstalledAsync(features);

        result[0].IsInstalled.Should().BeTrue();
    }

    [Fact]
    public async Task CheckFeaturesInstalledAsync_WindowsLineEndings_ParsesCorrectly()
    {
        // PowerShell on Windows outputs \r\n line endings
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>())
            .Returns("Web-Asp-Net45|True\r\nWeb-Other|False\r\n");

        var features = new List<WindowsFeature>
        {
            new("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45"),
            new("Other", "Web-Other", "IIS-Other")
        };

        var result = await _installer.CheckFeaturesInstalledAsync(features);

        result.Should().HaveCount(2);
        result[0].IsInstalled.Should().BeTrue();
        result[1].IsInstalled.Should().BeFalse();
    }

    [Fact]
    public async Task IsFeatureInstalledAsync_OutputWithTrailingNewline_ReturnsTrue()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("True\n");

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        var result = await _installer.IsFeatureInstalledAsync(feature);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFeatureInstalledAsync_OutputWithWindowsNewline_ReturnsTrue()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("True\r\n");

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        var result = await _installer.IsFeatureInstalledAsync(feature);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckFeaturesInstalledAsync_PassesServerNamesInCommand()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns(string.Empty);

        var features = new List<WindowsFeature>
        {
            new("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45"),
            new("MSMQ Services", "MSMQ-Services", "MSMQ-Container")
        };

        await _installer.CheckFeaturesInstalledAsync(features);

        await _executor.Received(1).RunPowerShellWithOutputAsync(
            Arg.Is<string>(s => s.Contains("'Web-Asp-Net45'") && s.Contains("'MSMQ-Services'")));
    }

    [Fact]
    public async Task CheckFeaturesInstalledAsync_CaseInsensitiveComparison_TrueMatches()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>())
            .Returns("Web-Asp-Net45|TRUE");

        var features = new List<WindowsFeature>
        {
            new("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45")
        };

        var result = await _installer.CheckFeaturesInstalledAsync(features);

        result[0].IsInstalled.Should().BeTrue();
    }

    [Fact]
    public async Task CheckFeaturesInstalledAsync_OnlyTrueStates_AreInstalled()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>())
            .Returns("Web-Asp-Net45|True\nWeb-Other|False\nMSMQ-Services|SomethingElse");

        var features = new List<WindowsFeature>
        {
            new("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45"),
            new("Other", "Web-Other", "IIS-Other"),
            new("MSMQ", "MSMQ-Services", "MSMQ-Container")
        };

        var result = await _installer.CheckFeaturesInstalledAsync(features);

        result[0].IsInstalled.Should().BeTrue();
        result[1].IsInstalled.Should().BeFalse();
        result[2].IsInstalled.Should().BeFalse();
    }

    // ============================================================
    //  InstallFeatureAsync
    // ============================================================

    [Fact]
    public async Task InstallFeatureAsync_WithoutSxs_CallsPowerShellWithCorrectCommand()
    {
        _executor.RunPowerShellCommandAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        var result = await _installer.InstallFeatureAsync(feature);

        result.Should().BeTrue();
        await _executor.Received(1).RunPowerShellCommandAsync(
            Arg.Is<string>(s =>
                s.Contains("Install-WindowsFeature") &&
                s.Contains("Web-Asp-Net45")),
            Arg.Is<string>(n => n == "ASP.NET 4.5"));
    }

    [Fact]
    public async Task InstallFeatureAsync_WithSxs_IncludesSource()
    {
        _executor.RunPowerShellCommandAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        var result = await _installer.InstallFeatureAsync(feature, @"C:\sources\sxs");

        result.Should().BeTrue();
        await _executor.Received(1).RunPowerShellCommandAsync(
            Arg.Is<string>(s =>
                s.Contains("-Source") &&
                s.Contains(@"C:\sources\sxs")),
            Arg.Any<string>());
    }

    [Fact]
    public async Task InstallFeatureAsync_WithEmptySxs_DoesNotIncludeSource()
    {
        _executor.RunPowerShellCommandAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        await _installer.InstallFeatureAsync(feature, "");

        await _executor.Received(1).RunPowerShellCommandAsync(
            Arg.Is<string>(s => !s.Contains("-Source")),
            Arg.Any<string>());
    }

    [Fact]
    public async Task InstallFeatureAsync_WithNullSxs_DoesNotIncludeSource()
    {
        _executor.RunPowerShellCommandAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        await _installer.InstallFeatureAsync(feature, null);

        await _executor.Received(1).RunPowerShellCommandAsync(
            Arg.Is<string>(s => !s.Contains("-Source")),
            Arg.Any<string>());
    }

    [Fact]
    public async Task InstallFeatureAsync_PowerShellReturnsFalse_ReturnsFalse()
    {
        _executor.RunPowerShellCommandAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        var result = await _installer.InstallFeatureAsync(feature);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task InstallFeatureAsync_PowerShellThrows_PropagatesException()
    {
        _executor.RunPowerShellCommandAsync(Arg.Any<string>(), Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("PowerShell failed"));

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        var act = async () => await _installer.InstallFeatureAsync(feature);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task InstallFeatureAsync_PassesFriendlyName()
    {
        _executor.RunPowerShellCommandAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        await _installer.InstallFeatureAsync(feature);

        await _executor.Received(1).RunPowerShellCommandAsync(
            Arg.Any<string>(),
            Arg.Is<string>(n => n == "ASP.NET 4.5"));
    }

    [Fact]
    public async Task InstallFeatureAsync_WithSxs_DoesNotIncludeLimitAccess()
    {
        // Server installer uses Install-WindowsFeature which doesn't have -LimitAccess
        _executor.RunPowerShellCommandAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        await _installer.InstallFeatureAsync(feature, @"C:\sources\sxs");

        await _executor.Received(1).RunPowerShellCommandAsync(
            Arg.Is<string>(s => !s.Contains("-LimitAccess")),
            Arg.Any<string>());
    }
}
