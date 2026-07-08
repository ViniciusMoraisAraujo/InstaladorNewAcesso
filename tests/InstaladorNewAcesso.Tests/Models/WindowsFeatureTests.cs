using FluentAssertions;
using InstaladorNewAcesso.Models;

namespace InstaladorNewAcesso.Tests.Models;

public class WindowsFeatureTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");

        feature.FriendlyName.Should().Be("ASP.NET 4.5");
        feature.ServerName.Should().Be("Web-Asp-Net45");
        feature.DesktopName.Should().Be("IIS-ASPNET45");
    }

    [Fact]
    public void TwoInstances_WithSameValues_ShouldBeEqual()
    {
        var feature1 = new WindowsFeature(".NET 3.5", "NET-Framework-Core", "NetFx3");
        var feature2 = new WindowsFeature(".NET 3.5", "NET-Framework-Core", "NetFx3");

        feature1.Should().Be(feature2);
        (feature1 == feature2).Should().BeTrue();
    }

    [Fact]
    public void TwoInstances_WithDifferentValues_ShouldNotBeEqual()
    {
        var feature1 = new WindowsFeature(".NET 3.5", "NET-Framework-Core", "NetFx3");
        var feature2 = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");

        feature1.Should().NotBe(feature2);
        (feature1 != feature2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        var feature = new WindowsFeature("Test", "Test-Server", "Test-Desktop");

        feature.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_TwoEqualInstances_ShouldBeSame()
    {
        var feature1 = new WindowsFeature("Test", "Test-Server", "Test-Desktop");
        var feature2 = new WindowsFeature("Test", "Test-Server", "Test-Desktop");

        feature1.GetHashCode().Should().Be(feature2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldContainAllProperties()
    {
        var feature = new WindowsFeature("ASP.NET 4.5", "Web-Asp-Net45", "IIS-ASPNET45");
        var str = feature.ToString();

        str.Should().Contain("ASP.NET 4.5");
        str.Should().Contain("Web-Asp-Net45");
        str.Should().Contain("IIS-ASPNET45");
    }

    [Fact]
    public void Deconstruct_ShouldReturnAllValues()
    {
        var feature = new WindowsFeature("MSMQ", "MSMQ-Services", "MSMQ-Container");
        var (friendlyName, serverName, desktopName) = feature;

        friendlyName.Should().Be("MSMQ");
        serverName.Should().Be("MSMQ-Services");
        desktopName.Should().Be("MSMQ-Container");
    }
}
