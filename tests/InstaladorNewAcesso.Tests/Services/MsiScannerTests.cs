using FluentAssertions;
using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Services;

namespace InstaladorNewAcesso.Tests.Services;

public class MsiScannerTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly InstallationPaths _paths;
    private const string DbChoice = "SQLServer";

    public MsiScannerTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "MsiScannerTests_" + Guid.NewGuid().ToString("N"));
        _paths = new InstallationPaths(Path.Combine(_tempRoot, "SoftPrime"));
    }

    [Fact]
    public void Scan_WhenRootNotFound_ShouldThrowDirectoryNotFoundException()
    {
        var nonExistentDir = Path.Combine(_tempRoot, "NonExistent");
        var scanner = new MsiScanner(_paths, DbChoice, nonExistentDir);

        var act = () => scanner.Scan();

        act.Should().Throw<DirectoryNotFoundException>()
            .WithMessage($"*{nonExistentDir}*");
    }

    [Fact]
    public void Scan_WhenRootIsEmpty_ShouldReturnEmptyList()
    {
        Directory.CreateDirectory(_tempRoot);
        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Scan_ShouldSkipSQLServerFolder_WhenDbChoiceIsOracle()
    {
        var sqlServerDir = Path.Combine(_tempRoot, "SQLServer");
        Directory.CreateDirectory(sqlServerDir);
        var msiPath = Path.Combine(sqlServerDir, "Database.msi");
        File.Create(msiPath).Dispose();

        var scanner = new MsiScanner(_paths, "Oracle", _tempRoot);

        var result = scanner.Scan();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Scan_ShouldIncludeSQLServerFolder_WhenDbChoiceMatches()
    {
        var sqlServerDir = Path.Combine(_tempRoot, "SQLServer");
        Directory.CreateDirectory(sqlServerDir);
        var msiPath = Path.Combine(sqlServerDir, "Database.msi");
        File.Create(msiPath).Dispose();

        var scanner = new MsiScanner(_paths, "SQLServer", _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].MsiPath.Should().Be(msiPath);
    }

    [Fact]
    public void Scan_ShouldSkipOracleFolder_WhenDbChoiceIsSQLServer()
    {
        var oracleDir = Path.Combine(_tempRoot, "Oracle");
        Directory.CreateDirectory(oracleDir);
        var msiPath = Path.Combine(oracleDir, "Database.msi");
        File.Create(msiPath).Dispose();

        var scanner = new MsiScanner(_paths, "SQLServer", _tempRoot);

        var result = scanner.Scan();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Scan_ShouldUseFolderMapping_WhenFolderNameMatches()
    {
        var controllerDir = Path.Combine(_tempRoot, "Controller");
        Directory.CreateDirectory(controllerDir);
        var msiPath = Path.Combine(controllerDir, "App.msi");
        File.Create(msiPath).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].TargetDirectory.Should().Be(_paths.Controller);
    }

    [Fact]
    public void Scan_ShouldUseFileNameMapping_WhenNameContainsKey()
    {
        Directory.CreateDirectory(_tempRoot);
        var msiPath = Path.Combine(_tempRoot, "ControllerApp.msi");
        File.Create(msiPath).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].TargetDirectory.Should().Be(_paths.Controller);
    }

    [Fact]
    public void Scan_FileNameMapping_ShouldTakePriorityOverFolderMapping()
    {
        var controllerDir = Path.Combine(_tempRoot, "Controller");
        Directory.CreateDirectory(controllerDir);
        // Arquivo na pasta Controller, mas nome contém "StandAloneEx"
        var msiPath = Path.Combine(controllerDir, "StandAloneEx.msi");
        File.Create(msiPath).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        // Deve usar fileNameMapping (StandAloneEx -> ControllerOfflineWinServiceEx)
        // em vez de folderMapping (Controller -> Controller)
        result.Should().ContainSingle();
        result[0].TargetDirectory.Should().Be(_paths.ControllerOfflineWinServiceEx);
    }

    [Fact]
    public void Scan_ShouldSkipMsIContainingUI()
    {
        Directory.CreateDirectory(_tempRoot);
        var msiPath = Path.Combine(_tempRoot, "WebAppUI.msi");
        File.Create(msiPath).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Scan_ShouldSkipMsIContainingDS()
    {
        Directory.CreateDirectory(_tempRoot);
        var msiPath = Path.Combine(_tempRoot, "WebAppDS.msi");
        File.Create(msiPath).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Scan_ShouldProcessMsIsInSubfolders_WhenNameDoesNotMatchUIOrDS()
    {
        var subDir = Path.Combine(_tempRoot, "AutoAtendimento");
        Directory.CreateDirectory(subDir);
        var msiPath = Path.Combine(subDir, "App.msi");
        File.Create(msiPath).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].TargetDirectory.Should().Be(_paths.AutoAtendimento);
    }

    [Fact]
    public void Scan_ShouldFallbackToDefaultPath_WhenNoMappingFound()
    {
        Directory.CreateDirectory(_tempRoot);
        var msiPath = Path.Combine(_tempRoot, "UnknownApp.msi");
        File.Create(msiPath).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].TargetDirectory.Should().Be(Path.Combine(_paths.NewAcessoRoot, "UnknownApp"));
    }

    [Fact]
    public void Scan_ShouldProcessAllNonDbSubfolders()
    {
        var winDir = Path.Combine(_tempRoot, "Win");
        Directory.CreateDirectory(winDir);
        var msi1 = Path.Combine(winDir, "WinApp.msi");
        File.Create(msi1).Dispose();

        var conexDir = Path.Combine(_tempRoot, "ConexBridge");
        Directory.CreateDirectory(conexDir);
        var msi2 = Path.Combine(conexDir, "ConexApp.msi");
        File.Create(msi2).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().HaveCount(2);
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
