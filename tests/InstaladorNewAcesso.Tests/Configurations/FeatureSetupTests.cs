using FluentAssertions;
using InstaladorNewAcesso.Core.Configurations;

namespace InstaladorNewAcesso.Tests.Configurations;

public class FeatureSetupTests
{
    private readonly FeatureSetup _sut = new();

    [Fact]
    public void Constructor_ShouldPopulateFeatures()
    {
        _sut.Features.Should().NotBeNull();
        _sut.Features.Should().NotBeEmpty();
    }

    [Fact]
    public void Features_ShouldContainAtLeastTwentyFeatures()
    {
        _sut.Features.Should().HaveCountGreaterThanOrEqualTo(20);
    }

    [Fact]
    public void Features_ShouldHaveCorrectTotalCount()
    {
        // Verificar o número exato de features definidas
        // Contagem atual: 32 (31 + 1 = 32 features no FeatureSetup.cs)
        _sut.Features.Should().HaveCount(32);
    }

    [Fact]
    public void Features_ShouldContainExpectedEntries()
    {
        _sut.Features.Should().Contain(f => f.FriendlyName == "Extensibilidade .NET 3.5");
        _sut.Features.Should().Contain(f => f.FriendlyName == "ASP.NET 4.5/4.6+");
        _sut.Features.Should().Contain(f => f.FriendlyName == "Protocolo WebSocket");
        _sut.Features.Should().Contain(f => f.FriendlyName == ".NET Framework 3.5 Core");
        _sut.Features.Should().Contain(f => f.FriendlyName == "Cliente Telnet");
        _sut.Features.Should().Contain(f => f.FriendlyName == "Servidor Telnet");
    }

    [Fact]
    public void Features_ShouldHaveNonNullNames()
    {
        _sut.Features.Should().AllSatisfy(f =>
        {
            f.FriendlyName.Should().NotBeNullOrWhiteSpace();
            f.ServerName.Should().NotBeNullOrWhiteSpace();
            f.DesktopName.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void Features_ShouldContainIISRelatedFeatures()
    {
        var iisFeatures = _sut.Features
            .Where(f => f.FriendlyName.Contains("IIS", StringComparison.OrdinalIgnoreCase)
                        || f.DesktopName.StartsWith("IIS-", StringComparison.OrdinalIgnoreCase))
            .ToList();

        iisFeatures.Should().NotBeEmpty();
        iisFeatures.Should().Contain(f => f.DesktopName == "IIS-ManagementConsole");
        iisFeatures.Should().Contain(f => f.DesktopName == "IIS-WebSockets");
    }

    [Fact]
    public void Features_ShouldContainWCFRelatedFeatures()
    {
        var wcfFeatures = _sut.Features
            .Where(f => f.FriendlyName.Contains("WCF", StringComparison.OrdinalIgnoreCase))
            .ToList();

        wcfFeatures.Should().NotBeEmpty();
        wcfFeatures.Should().Contain(f => f.DesktopName == "WCF-HTTP-Activation45");
    }

    [Fact]
    public void Features_ShouldContainAllExpectedServerNames()
    {
        var serverNames = _sut.Features.Select(f => f.ServerName).ToList();

        serverNames.Should().Contain("Web-Net-Ext");
        serverNames.Should().Contain("Web-Asp-Net45");
        serverNames.Should().Contain("NET-Framework-Core");
        serverNames.Should().Contain("Telnet-Client");
        serverNames.Should().Contain("MSMQ-Services");
    }

    [Fact]
    public void Features_ShouldContainAllExpectedDesktopNames()
    {
        var desktopNames = _sut.Features.Select(f => f.DesktopName).ToList();

        desktopNames.Should().Contain("IIS-NetFxExtensibility");
        desktopNames.Should().Contain("IIS-ASPNET45");
        desktopNames.Should().Contain("NetFx3");
        desktopNames.Should().Contain("TelnetClient");
        desktopNames.Should().Contain("MSMQ-Container");
    }

    [Fact]
    public void Features_ShouldHaveIISFeatures_WithCorrectServerDesktopMapping()
    {
        var iisAsp = _sut.Features.First(f => f.FriendlyName == "ASP.NET 4.5/4.6+");

        iisAsp.ServerName.Should().Be("Web-Asp-Net45");
        iisAsp.DesktopName.Should().Be("IIS-ASPNET45");
    }

    [Fact]
    public void Features_ShouldHaveNoDuplicateFriendlyNames()
    {
        var friendlyNames = _sut.Features.Select(f => f.FriendlyName).ToList();

        friendlyNames.Should().OnlyHaveUniqueItems();
    }
}
