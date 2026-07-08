using FluentAssertions;
using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Utils;

namespace InstaladorNewAcesso.Tests.Utils;

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

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, true); }
            catch { /* cleanup on best-effort basis */ }
        }
    }
}
