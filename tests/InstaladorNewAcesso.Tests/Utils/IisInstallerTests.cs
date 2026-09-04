using FluentAssertions;
using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Core.Utils;
using NSubstitute;

namespace InstaladorNewAcesso.Tests.Utils;

public class IisInstallerTests
{
    private readonly IProcessExecutor _executor;
    private readonly IisInstaller _installer;

    public IisInstallerTests()
    {
        _executor = Substitute.For<IProcessExecutor>();
        _installer = new IisInstaller(_executor);
    }

    // ============================================================
    //  CreateApplicationPoolAsync
    // ============================================================

    [Fact]
    public async Task CreateApplicationPoolAsync_WhenSucceeds_ReturnsTrue()
    {
        // Arrange
        _executor.RunPowerShellCommandAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        // Act
        var result = await _installer.CreateApplicationPoolAsync("MyPool", "v4.0", "Integrated");

        // Assert
        result.Should().BeTrue();
        await _executor.Received(1).RunPowerShellCommandAsync(
            Arg.Is<string>(s => s.Contains("New-WebAppPool") && s.Contains("MyPool")),
            Arg.Is<string>(s => s.Contains("MyPool")));
    }

    [Fact]
    public async Task CreateApplicationPoolAsync_WhenFails_ReturnsFalse()
    {
        // Arrange
        _executor.RunPowerShellCommandAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        // Act
        var result = await _installer.CreateApplicationPoolAsync("FailPool", "v2.0", "Classic");

        // Assert
        result.Should().BeFalse();
    }

    // ============================================================
    //  CreateSiteAsync
    // ============================================================

    [Fact]
    public async Task CreateSiteAsync_WhenSucceeds_ReturnsTrue()
    {
        // Arrange
        _executor.RunPowerShellCommandAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        // Act
        var result = await _installer.CreateSiteAsync("MySite", "MyPool", @"C:\inetpub\wwwroot", 80);

        // Assert
        result.Should().BeTrue();
        await _executor.Received(1).RunPowerShellCommandAsync(
            Arg.Is<string>(s => s.Contains("New-Website") && s.Contains("MySite") && s.Contains("80")),
            Arg.Is<string>(s => s.Contains("MySite")));
    }

    [Fact]
    public async Task CreateSiteAsync_WhenFails_ReturnsFalse()
    {
        // Arrange
        _executor.RunPowerShellCommandAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        // Act
        var result = await _installer.CreateSiteAsync("FailSite", "Pool", @"C:\path", 8080);

        // Assert
        result.Should().BeFalse();
    }

    // ============================================================
    //  SiteExistsAsync
    // ============================================================

    [Fact]
    public async Task SiteExistsAsync_WhenOutputIsTrue_ReturnsTrue()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("True");

        // Act
        var result = await _installer.SiteExistsAsync("ExistingSite");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SiteExistsAsync_WhenOutputIsFalse_ReturnsFalse()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("False");

        // Act
        var result = await _installer.SiteExistsAsync("NonExistentSite");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SiteExistsAsync_WhenOutputEmpty_ReturnsFalse()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns(string.Empty);

        // Act
        var result = await _installer.SiteExistsAsync("UnknownSite");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SiteExistsAsync_OutputIsCaseInsensitive()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("true");

        // Act
        var result = await _installer.SiteExistsAsync("CaseSite");

        // Assert
        result.Should().BeTrue();
    }

    // ============================================================
    //  AppPoolExistsAsync
    // ============================================================

    [Fact]
    public async Task AppPoolExistsAsync_WhenOutputIsTrue_ReturnsTrue()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("True");

        // Act
        var result = await _installer.AppPoolExistsAsync("ExistingPool");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AppPoolExistsAsync_WhenOutputIsFalse_ReturnsFalse()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("False");

        // Act
        var result = await _installer.AppPoolExistsAsync("MissingPool");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AppPoolExistsAsync_WhenOutputEmpty_ReturnsFalse()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns(string.Empty);

        // Act
        var result = await _installer.AppPoolExistsAsync("EmptyPool");

        // Assert
        result.Should().BeFalse();
    }

    // ============================================================
    //  UpdateSitePhysicalPathAsync
    // ============================================================

    [Fact]
    public async Task UpdateSitePhysicalPathAsync_WhenSucceeds_ReturnsTrue()
    {
        // Arrange
        _executor.RunPowerShellCommandAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        // Act
        var result = await _installer.UpdateSitePhysicalPathAsync("MySite", @"D:\NewPath\App");

        // Assert
        result.Should().BeTrue();
        await _executor.Received(1).RunPowerShellCommandAsync(
            Arg.Is<string>(s => s.Contains("Set-ItemProperty") && s.Contains("D:\\NewPath\\App")),
            Arg.Is<string>(s => s.Contains("MySite")));
    }

    [Fact]
    public async Task UpdateSitePhysicalPathAsync_WhenFails_ReturnsFalse()
    {
        // Arrange
        _executor.RunPowerShellCommandAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        // Act
        var result = await _installer.UpdateSitePhysicalPathAsync("FailSite", @"C:\path");

        // Assert
        result.Should().BeFalse();
    }

    // ============================================================
    //  CheckAppPoolsExistAsync
    // ============================================================

    [Fact]
    public async Task CheckAppPoolsExistAsync_EmptyArray_ReturnsEmptyDictionary()
    {
        // Act
        var result = await _installer.CheckAppPoolsExistAsync([]);

        // Assert
        result.Should().BeEmpty();
        // Nenhuma chamada ao executor deve ter sido feita
        await _executor.DidNotReceive().RunPowerShellWithOutputAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task CheckAppPoolsExistAsync_SinglePoolExists_ReturnsTrue()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("MyPool|True");

        // Act
        var result = await _installer.CheckAppPoolsExistAsync(["MyPool"]);

        // Assert
        result.Should().ContainKey("MyPool").WhoseValue.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAppPoolsExistAsync_SinglePoolNotExists_ReturnsFalse()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("GhostPool|False");

        // Act
        var result = await _installer.CheckAppPoolsExistAsync(["GhostPool"]);

        // Assert
        result.Should().ContainKey("GhostPool").WhoseValue.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAppPoolsExistAsync_MultiplePools_AllExist()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>())
            .Returns("PoolA|True\nPoolB|True\nPoolC|True");

        // Act
        var result = await _installer.CheckAppPoolsExistAsync(["PoolA", "PoolB", "PoolC"]);

        // Assert
        result.Should().HaveCount(3);
        result["PoolA"].Should().BeTrue();
        result["PoolB"].Should().BeTrue();
        result["PoolC"].Should().BeTrue();
    }

    [Fact]
    public async Task CheckAppPoolsExistAsync_MixedResults_ReturnsCorrectValues()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>())
            .Returns("PoolA|True\nPoolB|False");

        // Act
        var result = await _installer.CheckAppPoolsExistAsync(["PoolA", "PoolB", "PoolC"]);

        // Assert
        result.Should().HaveCount(3);
        result["PoolA"].Should().BeTrue();
        result["PoolB"].Should().BeFalse();
        result["PoolC"].Should().BeFalse(); // não listado → default false
    }

    [Fact]
    public async Task CheckAppPoolsExistAsync_CaseInsensitiveNameMatching()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("mypool|True");

        // Act
        var result = await _installer.CheckAppPoolsExistAsync(["MyPool"]);

        // Assert
        result.Should().ContainKey("MyPool").WhoseValue.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAppPoolsExistAsync_WhitespaceInOutput_IsTrimmed()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("  MyPool  |  True  ");

        // Act
        var result = await _installer.CheckAppPoolsExistAsync(["MyPool"]);

        // Assert
        result.Should().ContainKey("MyPool").WhoseValue.Should().BeTrue();
    }

    // ============================================================
    //  CheckSitesExistAsync
    // ============================================================

    [Fact]
    public async Task CheckSitesExistAsync_EmptyArray_ReturnsEmptyDictionary()
    {
        // Act
        var result = await _installer.CheckSitesExistAsync([]);

        // Assert
        result.Should().BeEmpty();
        await _executor.DidNotReceive().RunPowerShellWithOutputAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task CheckSitesExistAsync_SingleSiteExists_ReturnsTrue()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("MySite|True");

        // Act
        var result = await _installer.CheckSitesExistAsync(["MySite"]);

        // Assert
        result.Should().ContainKey("MySite").WhoseValue.Should().BeTrue();
    }

    [Fact]
    public async Task CheckSitesExistAsync_MultipleSites_AllFound()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>())
            .Returns("Site1|True\nSite2|True");

        // Act
        var result = await _installer.CheckSitesExistAsync(["Site1", "Site2"]);

        // Assert
        result.Should().HaveCount(2);
        result["Site1"].Should().BeTrue();
        result["Site2"].Should().BeTrue();
    }

    [Fact]
    public async Task CheckSitesExistAsync_MissingSiteInOutput_DefaultsToFalse()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("Site1|True");

        // Act
        var result = await _installer.CheckSitesExistAsync(["Site1", "MissingSite"]);

        // Assert
        result.Should().HaveCount(2);
        result["Site1"].Should().BeTrue();
        result["MissingSite"].Should().BeFalse();
    }

    // ============================================================
    //  RemoveSiteAsync
    // ============================================================

    [Fact]
    public async Task RemoveSiteAsync_WhenOutputOk_ReturnsTrue()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("OK");

        // Act
        var result = await _installer.RemoveSiteAsync("SiteToRemove");

        // Assert
        result.Should().BeTrue();
        await _executor.Received(1).RunPowerShellWithOutputAsync(
            Arg.Is<string>(s => s.Contains("Remove-Website") && s.Contains("SiteToRemove")));
    }

    [Fact]
    public async Task RemoveSiteAsync_WhenOutputNotOk_ReturnsFalse()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("not-ok");

        // Act
        var result = await _installer.RemoveSiteAsync("AnySite");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveSiteAsync_WhenOutputEmpty_ReturnsFalse()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns(string.Empty);

        // Act
        var result = await _installer.RemoveSiteAsync("EmptySite");

        // Assert
        result.Should().BeFalse();
    }

    // ============================================================
    //  RemoveAppPoolAsync
    // ============================================================

    [Fact]
    public async Task RemoveAppPoolAsync_WhenOutputOk_ReturnsTrue()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("OK");

        // Act
        var result = await _installer.RemoveAppPoolAsync("PoolToRemove");

        // Assert
        result.Should().BeTrue();
        await _executor.Received(1).RunPowerShellWithOutputAsync(
            Arg.Is<string>(s => s.Contains("Remove-WebAppPool") && s.Contains("PoolToRemove")));
    }

    [Fact]
    public async Task RemoveAppPoolAsync_WhenOutputNotOk_ReturnsFalse()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns("fail");

        // Act
        var result = await _installer.RemoveAppPoolAsync("AnyPool");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAppPoolAsync_WhenOutputEmpty_ReturnsFalse()
    {
        // Arrange
        _executor.RunPowerShellWithOutputAsync(Arg.Any<string>()).Returns(string.Empty);

        // Act
        var result = await _installer.RemoveAppPoolAsync("EmptyPool");

        // Assert
        result.Should().BeFalse();
    }

    // ============================================================
    //  GrantDirectoryPermissionsAsync
    // ============================================================

    [Fact]
    public async Task GrantDirectoryPermissionsAsync_WhenSucceeds_ReturnsTrue()
    {
        _executor.RunPowerShellCommandAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var result = await _installer.GrantDirectoryPermissionsAsync(@"C:\NewAcesso\WebAppDS");

        result.Should().BeTrue();
        await _executor.Received(1).RunPowerShellCommandAsync(
            Arg.Is<string>(s => s.Contains("icacls") && s.Contains("IIS_IUSRS")),
            Arg.Any<string>());
    }

    [Fact]
    public async Task GrantDirectoryPermissionsAsync_WhenFails_ReturnsFalse()
    {
        _executor.RunPowerShellCommandAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await _installer.GrantDirectoryPermissionsAsync(@"C:\NewAcesso\WebAppDS");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GrantDirectoryPermissionsAsync_WhenPathEmpty_ThrowsArgumentException()
    {
        var act = async () => await _installer.GrantDirectoryPermissionsAsync("");

        await act.Should().ThrowAsync<ArgumentException>();
    }
}

