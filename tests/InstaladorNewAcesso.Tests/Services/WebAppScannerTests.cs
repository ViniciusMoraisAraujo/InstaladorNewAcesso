using FluentAssertions;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Tests.Services;

public class WebAppScannerTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly InstallationPaths _paths;
    private const string DbChoice = "SQLServer";

    public WebAppScannerTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "WebAppScannerTests_" + Guid.NewGuid().ToString("N"));
        _paths = new InstallationPaths(Path.Combine(_tempRoot, "SoftPrime"));
    }

    [Fact]
    public void Scan_WhenRootNotFound_ShouldReturnEmptyList()
    {
        var nonExistentDir = Path.Combine(_tempRoot, "NonExistent");
        var scanner = new WebAppScanner(_paths, DbChoice, nonExistentDir);

        var result = scanner.Scan();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Scan_WhenRootIsEmpty_ShouldReturnEmptyList()
    {
        Directory.CreateDirectory(_tempRoot);
        var scanner = new WebAppScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Scan_ShouldDetectWebAppDS_WhenMsiContainsDS()
    {
        Directory.CreateDirectory(_tempRoot);
        var msiPath = Path.Combine(_tempRoot, "WebAppDS.msi");
        File.Create(msiPath).Dispose();

        var scanner = new WebAppScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].SiteName.Should().Be("WebAppDS");
        result[0].AppPoolName.Should().Be("WebAppDS");
        result[0].Port.Should().Be(8080);
        result[0].TargetDirectory.Should().Be(_paths.WebAppDS);
        result[0].MsiPath.Should().Be(msiPath);
    }

    [Fact]
    public void Scan_ShouldDetectWebAppUI_WhenMsiContainsUI()
    {
        Directory.CreateDirectory(_tempRoot);
        var msiPath = Path.Combine(_tempRoot, "WebAppUI.msi");
        File.Create(msiPath).Dispose();

        var scanner = new WebAppScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].SiteName.Should().Be("WebAppUI");
        result[0].AppPoolName.Should().Be("WebAppUI");
        result[0].Port.Should().Be(8081);
        result[0].TargetDirectory.Should().Be(_paths.WebAppUI);
    }

    [Fact]
    public void Scan_DS_ShouldBeDetectedBeforeUI_WhenNameContainsBoth()
    {
        Directory.CreateDirectory(_tempRoot);
        // DS verificado antes de UI, então deve detectar como WebAppDS
        var msiPath = Path.Combine(_tempRoot, "MyDS_UI.msi");
        File.Create(msiPath).Dispose();

        var scanner = new WebAppScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].SiteName.Should().Be("WebAppDS");
    }

    [Fact]
    public void Scan_ShouldIgnoreNonWebAppMSIs()
    {
        Directory.CreateDirectory(_tempRoot);
        var msiPath = Path.Combine(_tempRoot, "RegularApp.msi");
        File.Create(msiPath).Dispose();

        var scanner = new WebAppScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Scan_ShouldFindWebAppsInRootAndSubfolders()
    {
        var subDir = Path.Combine(_tempRoot, "SubFolder");
        Directory.CreateDirectory(subDir);
        var msiPath = Path.Combine(_tempRoot, "WebAppDS.msi");
        File.Create(msiPath).Dispose();

        // Arquivo separado em subpasta (caminho diferente, portanto não é duplicata)
        var dupPath = Path.Combine(subDir, "WebAppUI.msi");
        File.Create(dupPath).Dispose();

        var scanner = new WebAppScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        // Cada arquivo em caminho diferente é encontrado separadamente
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.SiteName == "WebAppDS");
        result.Should().Contain(r => r.SiteName == "WebAppUI");
    }

    [Fact]
    public void Scan_ShouldRespectDbChoiceForSQLServerFolder()
    {
        var sqlDir = Path.Combine(_tempRoot, "SQLServer");
        Directory.CreateDirectory(sqlDir);
        var msiPath = Path.Combine(sqlDir, "WebAppDS.msi");
        File.Create(msiPath).Dispose();

        var scanner = new WebAppScanner(_paths, "SQLServer", _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].SiteName.Should().Be("WebAppDS");
    }

    [Fact]
    public void Scan_ShouldIgnoreSQLServerFolder_WhenDbChoiceIsOracle()
    {
        var sqlDir = Path.Combine(_tempRoot, "SQLServer");
        Directory.CreateDirectory(sqlDir);
        var msiPath = Path.Combine(sqlDir, "WebAppDS.msi");
        File.Create(msiPath).Dispose();

        var scanner = new WebAppScanner(_paths, "Oracle", _tempRoot);

        var result = scanner.Scan();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Scan_ShouldDetectMultipleWebApps()
    {
        Directory.CreateDirectory(_tempRoot);
        var msi1 = Path.Combine(_tempRoot, "WebAppDS.msi");
        File.Create(msi1).Dispose();
        var msi2 = Path.Combine(_tempRoot, "WebAppUI.msi");
        File.Create(msi2).Dispose();

        var scanner = new WebAppScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().HaveCount(2);
        result.Should().Contain(r => r.SiteName == "WebAppDS");
        result.Should().Contain(r => r.SiteName == "WebAppUI");
    }

    [Fact]
    public void Scan_DS_WithNameContainingDSInMiddle_ShouldBeDetected()
    {
        Directory.CreateDirectory(_tempRoot);
        var msiPath = Path.Combine(_tempRoot, "SomethingDSomething.msi");
        File.Create(msiPath).Dispose();

        var scanner = new WebAppScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].SiteName.Should().Be("WebAppDS");
    }

    [Fact]
    public void Scan_ShouldSetCorrectForcedInstallPathForDS()
    {
        Directory.CreateDirectory(_tempRoot);
        var msiPath = Path.Combine(_tempRoot, "WebAppDS.msi");
        File.Create(msiPath).Dispose();

        var scanner = new WebAppScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].ForcedInstallPath.Should().Be(@"C:\inetpub\wwwroot\WebAppDS");
    }

    [Fact]
    public void Scan_ShouldSetCorrectForcedInstallPathForUI()
    {
        Directory.CreateDirectory(_tempRoot);
        var msiPath = Path.Combine(_tempRoot, "WebAppUI.msi");
        File.Create(msiPath).Dispose();

        var scanner = new WebAppScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].ForcedInstallPath.Should().Be(@"C:\inetpub\wwwroot\WebAppUI");
    }

    // ============================================================
    //  Edge cases — duplicate MSI path deduplication
    // ============================================================

    [Fact]
    public void Scan_DifferentPaths_AreFoundSeparately()
    {
        // Two different files at different paths are both found (no dedup by content)
        var subDir = Path.Combine(_tempRoot, "SubFolder");
        Directory.CreateDirectory(subDir);
        File.Create(Path.Combine(subDir, "WebAppDS.msi")).Dispose();
        File.Create(Path.Combine(_tempRoot, "WebAppDS.msi")).Dispose();

        var scanner = new WebAppScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        // Different paths = different results
        result.Should().HaveCount(2);
    }

    // ============================================================
    //  Edge cases — deep nested subfolders
    // ============================================================

    [Fact]
    public void Scan_ShouldFindWebApps_InDeepNestedSubfolders()
    {
        var deepDir = Path.Combine(_tempRoot, "Level1", "Level2", "Level3");
        Directory.CreateDirectory(deepDir);
        var msiPath = Path.Combine(deepDir, "WebAppUI.msi");
        File.Create(msiPath).Dispose();

        var scanner = new WebAppScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].SiteName.Should().Be("WebAppUI");
        result[0].MsiPath.Should().Be(msiPath);
    }

    // ============================================================
    //  Edge cases — Oracle folder with matching dbChoice
    // ============================================================

    [Fact]
    public void Scan_OracleFolder_WhenDbChoiceIsOracle_ShouldBeScanned()
    {
        var oracleDir = Path.Combine(_tempRoot, "Oracle");
        Directory.CreateDirectory(oracleDir);
        var msiPath = Path.Combine(oracleDir, "WebAppDS.msi");
        File.Create(msiPath).Dispose();

        var scanner = new WebAppScanner(_paths, "Oracle", _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].SiteName.Should().Be("WebAppDS");
    }

    // ============================================================
    //  Edge cases — non-matching names in subfolders ignored
    // ============================================================

    [Fact]
    public void Scan_SubfolderWithOnlyNonWebAppMSIs_ShouldReturnEmpty()
    {
        var subDir = Path.Combine(_tempRoot, "RegularApps");
        Directory.CreateDirectory(subDir);
        File.Create(Path.Combine(subDir, "ServerApp.msi")).Dispose();
        File.Create(Path.Combine(subDir, "ClientTool.msi")).Dispose();

        var scanner = new WebAppScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().BeEmpty();
    }

    // ============================================================
    //  Edge cases — mixed webapp and non-webapp MSIs
    // ============================================================

    [Fact]
    public void Scan_MixedWebAppAndNonWebAppMSIs_OnlyReturnsWebApps()
    {
        Directory.CreateDirectory(_tempRoot);
        File.Create(Path.Combine(_tempRoot, "WebAppDS.msi")).Dispose();
        File.Create(Path.Combine(_tempRoot, "WebAppUI.msi")).Dispose();
        File.Create(Path.Combine(_tempRoot, "ServerTool.msi")).Dispose();
        File.Create(Path.Combine(_tempRoot, "readme.txt")).Dispose();

        var scanner = new WebAppScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().HaveCount(2);
        result.Should().Contain(r => r.SiteName == "WebAppDS");
        result.Should().Contain(r => r.SiteName == "WebAppUI");
    }

    // ============================================================
    //  Edge cases — DS priority over UI
    // ============================================================

    [Fact]
    public void Scan_NameWithDSAndUI_DSIsDetectedFirst()
    {
        Directory.CreateDirectory(_tempRoot);
        // DS checked before UI in the code, so "DS" match wins
        var msiPath = Path.Combine(_tempRoot, "AppDS_Final.msi");
        File.Create(msiPath).Dispose();

        var scanner = new WebAppScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].SiteName.Should().Be("WebAppDS");
    }

    // ============================================================
    //  Edge cases — case insensitive DS/UI detection
    // ============================================================

    [Fact]
    public void Scan_CaseInsensitiveDS_DetectedAsWebAppDS()
    {
        Directory.CreateDirectory(_tempRoot);
        var msiPath = Path.Combine(_tempRoot, "webappds.msi");
        File.Create(msiPath).Dispose();

        var scanner = new WebAppScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].SiteName.Should().Be("WebAppDS");
    }

    [Fact]
    public void Scan_CaseInsensitiveUI_DetectedAsWebAppUI()
    {
        Directory.CreateDirectory(_tempRoot);
        var msiPath = Path.Combine(_tempRoot, "webappui.msi");
        File.Create(msiPath).Dispose();

        var scanner = new WebAppScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].SiteName.Should().Be("WebAppUI");
    }

    // ============================================================
    //  Edge cases — non-MSI files completely ignored
    // ============================================================

    [Fact]
    public void Scan_NonMsiFiles_InRoot_Ignored()
    {
        Directory.CreateDirectory(_tempRoot);
        File.Create(Path.Combine(_tempRoot, "setup.exe")).Dispose();
        File.Create(Path.Combine(_tempRoot, "config.xml")).Dispose();
        File.Create(Path.Combine(_tempRoot, "readme.md")).Dispose();

        var scanner = new WebAppScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().BeEmpty();
    }

    // ============================================================
    //  Edge cases — composite DB folder names (PrimeAcesso 5.11)
    // ============================================================

    [Fact]
    public void Scan_CompositeDbFolders_ScansOnlyMatchingDatabaseWebApps()
    {
        var sqlDir = Path.Combine(_tempRoot, "SQLServer - Web - WebDataService - Win");
        Directory.CreateDirectory(sqlDir);
        File.Create(Path.Combine(sqlDir, "WebAppDS.msi")).Dispose();
        File.Create(Path.Combine(sqlDir, "WebAppUI.msi")).Dispose();
        File.Create(Path.Combine(sqlDir, "Win.msi")).Dispose();

        var oracleDir = Path.Combine(_tempRoot, "Oracle - Web - WebDataService - Win");
        Directory.CreateDirectory(oracleDir);
        File.Create(Path.Combine(oracleDir, "WebAppDS.msi")).Dispose();
        File.Create(Path.Combine(oracleDir, "WebAppUI.msi")).Dispose();

        var scanner = new WebAppScanner(_paths, "SQLServer", _tempRoot);

        var result = scanner.Scan();

        result.Should().HaveCount(2);
        result.Should().Contain(r => r.SiteName == "WebAppDS" && r.MsiPath.Contains("SQLServer"));
        result.Should().Contain(r => r.SiteName == "WebAppUI" && r.MsiPath.Contains("SQLServer"));
        result.Should().NotContain(r => r.MsiPath.Contains("Oracle"));
    }

    [Fact]
    public void Scan_WebDataServiceNaming_DetectedAsWebAppDS()
    {
        var sqlDir = Path.Combine(_tempRoot, "SQLServer - Web - WebDataService - Win");
        Directory.CreateDirectory(sqlDir);
        File.Create(Path.Combine(sqlDir, "WebDataService.msi")).Dispose();

        var scanner = new WebAppScanner(_paths, "SQLServer", _tempRoot);
        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].SiteName.Should().Be("WebAppDS");
        result[0].TargetDirectory.Should().Be(_paths.WebAppDS);
        result[0].Port.Should().Be(8080);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, true); }
            catch { /* cleanup on best-effort basis */ }
        }
    }
}
