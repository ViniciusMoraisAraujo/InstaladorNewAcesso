using FluentAssertions;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Services;

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

    // ============================================================
    //  Edge cases — deep nested subfolders
    // ============================================================

    [Fact]
    public void Scan_ShouldResolveFolderMapping_InDeepNestedSubfolder()
    {
        // Controller/Apps/DeepDir/ — last folder is DeepDir (no mapping),
        // but first folder is Controller (has mapping).
        var deepDir = Path.Combine(_tempRoot, "Controller", "Apps", "DeepDir");
        Directory.CreateDirectory(deepDir);
        var msiPath = Path.Combine(deepDir, "App.msi");
        File.Create(msiPath).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        // LastFolder "DeepDir" not in mapping, so fallback to firstFolder "Controller"
        result.Should().ContainSingle();
        result[0].TargetDirectory.Should().Be(_paths.Controller);
    }

    [Fact]
    public void Scan_FallsBackToFirstFolder_WhenLastFolderNotMapped()
    {
        // ConnectionRecord/SubDir/ — last folder SubDir not in mapping,
        // first folder ConnectionRecord has mapping.
        var subDir = Path.Combine(_tempRoot, "ConnectionRecord", "SubDir");
        Directory.CreateDirectory(subDir);
        var msiPath = Path.Combine(subDir, "DataApp.msi");
        File.Create(msiPath).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].TargetDirectory.Should().Be(_paths.ConnectionRecord);
    }

    // ============================================================
    //  Edge cases — multiple MSIs in same folder
    // ============================================================

    [Fact]
    public void Scan_ShouldFindAllMsIs_InSameFolder()
    {
        var dir = Path.Combine(_tempRoot, "Controller");
        Directory.CreateDirectory(dir);
        var msi1 = Path.Combine(dir, "App1.msi");
        var msi2 = Path.Combine(dir, "App2.msi");
        File.Create(msi1).Dispose();
        File.Create(msi2).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().HaveCount(2);
        result.Should().Contain(r => r.MsiPath == msi1);
        result.Should().Contain(r => r.MsiPath == msi2);
    }

    // ============================================================
    //  Edge cases — mixed db and non-db folders
    // ============================================================

    [Fact]
    public void Scan_MixedDbAndNonDbFolders_ProcessesOnlyMatching()
    {
        var sqlDir = Path.Combine(_tempRoot, "SQLServer");
        Directory.CreateDirectory(sqlDir);
        File.Create(Path.Combine(sqlDir, "DbApp.msi")).Dispose();

        var oracleDir = Path.Combine(_tempRoot, "Oracle");
        Directory.CreateDirectory(oracleDir);
        File.Create(Path.Combine(oracleDir, "OracleApp.msi")).Dispose();

        var winDir = Path.Combine(_tempRoot, "Win");
        Directory.CreateDirectory(winDir);
        File.Create(Path.Combine(winDir, "WinApp.msi")).Dispose();

        var scanner = new MsiScanner(_paths, "SQLServer", _tempRoot);

        var result = scanner.Scan();

        // SQLServer included (matches), Oracle skipped, Win always included
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.MsiPath.Contains("DbApp"));
        result.Should().Contain(r => r.MsiPath.Contains("WinApp"));
    }

    // ============================================================
    //  Edge cases — non-MSI files ignored
    // ============================================================

    [Fact]
    public void Scan_ShouldIgnoreNonMsiFiles()
    {
        Directory.CreateDirectory(_tempRoot);
        File.Create(Path.Combine(_tempRoot, "readme.txt")).Dispose();
        File.Create(Path.Combine(_tempRoot, "setup.exe")).Dispose();
        File.Create(Path.Combine(_tempRoot, "data.zip")).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Scan_ShouldIgnoreNonMsiFiles_InSubfolders()
    {
        var subDir = Path.Combine(_tempRoot, "Controller");
        Directory.CreateDirectory(subDir);
        File.Create(Path.Combine(subDir, "readme.txt")).Dispose();
        File.Create(Path.Combine(subDir, "setup.exe")).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().BeEmpty();
    }

    // ============================================================
    //  Edge cases — case-insensitive folder matching
    // ============================================================

    [Fact]
    public void Scan_ShouldMatchFolderMapping_CaseInsensitive()
    {
        // "controller" (lowercase) should match "Controller" mapping
        var dir = Path.Combine(_tempRoot, "controller");
        Directory.CreateDirectory(dir);
        var msiPath = Path.Combine(dir, "App.msi");
        File.Create(msiPath).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].TargetDirectory.Should().Be(_paths.Controller);
    }

    [Fact]
    public void Scan_ShouldMatchFileNameMapping_CaseInsensitive()
    {
        Directory.CreateDirectory(_tempRoot);
        var msiPath = Path.Combine(_tempRoot, "CONTROLLERAPP.msi");
        File.Create(msiPath).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].TargetDirectory.Should().Be(_paths.Controller);
    }

    // ============================================================
    //  Edge cases — OffLine folder mapping
    // ============================================================

    [Fact]
    public void Scan_OffLineFolder_MapsToNewAcessoRootOffLine()
    {
        var offlineDir = Path.Combine(_tempRoot, "OffLine");
        Directory.CreateDirectory(offlineDir);
        var msiPath = Path.Combine(offlineDir, "OfflineApp.msi");
        File.Create(msiPath).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].TargetDirectory.Should().Be(Path.Combine(_paths.NewAcessoRoot, "OffLine"));
    }

    // ============================================================
    //  Edge cases — root MSIs with UI/DS in subfolder names
    // ============================================================

    [Fact]
    public void Scan_RootMsi_ContainingUI_IsSkipped()
    {
        Directory.CreateDirectory(_tempRoot);
        var msiPath = Path.Combine(_tempRoot, "MyWebAppUI.msi");
        File.Create(msiPath).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Scan_RootMsi_ContainingDS_IsSkipped()
    {
        Directory.CreateDirectory(_tempRoot);
        var msiPath = Path.Combine(_tempRoot, "MyWebAppDS.msi");
        File.Create(msiPath).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().BeEmpty();
    }

    // ============================================================
    //  Edge cases — fileNameMapping keys in order
    // ============================================================

    [Fact]
    public void Scan_StandAloneEx_MapsToControllerOfflineWinServiceEx()
    {
        Directory.CreateDirectory(_tempRoot);
        var msiPath = Path.Combine(_tempRoot, "StandAloneEx.msi");
        File.Create(msiPath).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].TargetDirectory.Should().Be(_paths.ControllerOfflineWinServiceEx);
    }

    [Fact]
    public void Scan_StandAloneIn_MapsToControllerOfflineWinServiceIn()
    {
        Directory.CreateDirectory(_tempRoot);
        var msiPath = Path.Combine(_tempRoot, "StandAloneIn.msi");
        File.Create(msiPath).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].TargetDirectory.Should().Be(_paths.ControllerOfflineWinServiceIn);
    }

    // ============================================================
    //  Edge cases — UI/DS MSIs in subfolders are also skipped
    // ============================================================

    [Fact]
    public void Scan_ShouldSkipUIandDSMsIs_InSubfolders()
    {
        var subDir = Path.Combine(_tempRoot, "WebApps");
        Directory.CreateDirectory(subDir);
        File.Create(Path.Combine(subDir, "WebAppUI.msi")).Dispose();
        File.Create(Path.Combine(subDir, "WebAppDS.msi")).Dispose();
        // Add a valid one
        File.Create(Path.Combine(subDir, "NormalApp.msi")).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);

        var result = scanner.Scan();

        result.Should().ContainSingle();
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
