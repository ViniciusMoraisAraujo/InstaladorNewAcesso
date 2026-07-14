using FluentAssertions;
using InstaladorNewAcesso.Core.Implementations;
using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InstaladorNewAcesso.Tests.Implementations;

public class WindowsDesktopInstallerTests
{
    private readonly IProcessExecutor _executor;
    private readonly WindowsDesktopInstaller _installer;

    public WindowsDesktopInstallerTests()
    {
        _executor = Substitute.For<IProcessExecutor>();
        _installer = new WindowsDesktopInstaller(_executor);
    }

    // ============================================================
    //  IsFeatureInstalledAsync
    // ============================================================

    [Fact]
    public async Task IsFeatureInstalledAsync_OutputEnabled_ReturnsTrue()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("Enabled");

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        var result = await _installer.IsFeatureInstalledAsync(feature);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFeatureInstalledAsync_OutputDisabled_ReturnsFalse()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("Disabled");

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        var result = await _installer.IsFeatureInstalledAsync(feature);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsFeatureInstalledAsync_OutputEnabledCaseInsensitive_ReturnsTrue()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("enabled");

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        var result = await _installer.IsFeatureInstalledAsync(feature);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFeatureInstalledAsync_OutputWithWhitespace_ReturnsTrue()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("  Enabled  ");

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
    public async Task IsFeatureInstalledAsync_PassesDesktopNameInCommand()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("Disabled");

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        await _installer.IsFeatureInstalledAsync(feature);

        await _executor.Received(1).RunPowerShellWithOutputAsync(
            Arg.Is<string>(s => s.Contains("IIS-ASPNET45")));
    }

    [Fact]
    public async Task IsFeatureInstalledAsync_DoesNotUseServerName()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("Disabled");

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        await _installer.IsFeatureInstalledAsync(feature);

        await _executor.Received(1).RunPowerShellWithOutputAsync(
            Arg.Is<string>(s => !s.Contains("Web-Asp-Net45")));
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
    public async Task CheckFeaturesInstalledAsync_SingleEnabled_ReturnsCorrectResult()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>())
            .Returns("IIS-ASPNET45|Enabled");

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
    public async Task CheckFeaturesInstalledAsync_SingleDisabled_ReturnsCorrectResult()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>())
            .Returns("IIS-ASPNET45|Disabled");

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
            .Returns("IIS-ASPNET45|Enabled\nIIS-NetFx45-AdvSrvs|Disabled\nMSMQ-Container|Enabled");

        var features = new List<WindowsFeature>
        {
            new("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45"),
            new(".NET 4.5 Advanced Services", "NET-Framework-45-AdvSrvs", "IIS-NetFx45-AdvSrvs"),
            new("MSMQ Container", "MSMQ-Container", "MSMQ-Container")
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
            .Returns("IIS-Other|Enabled");

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
            .Returns("  IIS-ASPNET45  |  Enabled  ");

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
            .Returns("IIS-ASPNET45|Enabled\r\nIIS-Other|Disabled\r\n");

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
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("Enabled\n");

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        var result = await _installer.IsFeatureInstalledAsync(feature);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFeatureInstalledAsync_OutputWithWindowsNewline_ReturnsTrue()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("Enabled\r\n");

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        var result = await _installer.IsFeatureInstalledAsync(feature);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckFeaturesInstalledAsync_PassesDesktopNamesInCommand()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns(string.Empty);

        var features = new List<WindowsFeature>
        {
            new("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45"),
            new("MSMQ Container", "MSMQ-Container", "MSMQ-Container")
        };

        await _installer.CheckFeaturesInstalledAsync(features);

        await _executor.Received(1).RunPowerShellWithOutputAsync(
            Arg.Is<string>(s => s.Contains("'IIS-ASPNET45'") && s.Contains("'MSMQ-Container'")));
    }

    [Fact]
    public async Task CheckFeaturesInstalledAsync_CaseInsensitiveComparison_EnabledMatches()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>())
            .Returns("iis-aspnet45|ENABLED");

        var features = new List<WindowsFeature>
        {
            new("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45")
        };

        var result = await _installer.CheckFeaturesInstalledAsync(features);

        result[0].IsInstalled.Should().BeTrue();
    }

    [Fact]
    public async Task CheckFeaturesInstalledAsync_OnlyEnabledStates_AreTrue()
    {
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>())
            .Returns("IIS-ASPNET45|Enabled\nIIS-Other|Disabled\nMSMQ-Container|SomethingElse");

        var features = new List<WindowsFeature>
        {
            new("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45"),
            new("Other", "Web-Other", "IIS-Other"),
            new("MSMQ", "MSMQ-Container", "MSMQ-Container")
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
                s.Contains("Enable-WindowsOptionalFeature") &&
                s.Contains("IIS-ASPNET45") &&
                s.Contains("-All") &&
                s.Contains("-NoRestart")),
            Arg.Is<string>(n => n == "ASP.NET 4.5"));
    }

    [Fact]
    public async Task InstallFeatureAsync_WithSxs_IncludesSourceAndLimitAccess()
    {
        _executor.RunPowerShellCommandAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        var result = await _installer.InstallFeatureAsync(feature, @"C:\sources\sxs");

        result.Should().BeTrue();
        await _executor.Received(1).RunPowerShellCommandAsync(
            Arg.Is<string>(s =>
                s.Contains("-Source") &&
                s.Contains(@"C:\sources\sxs") &&
                s.Contains("-LimitAccess")),
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
}
