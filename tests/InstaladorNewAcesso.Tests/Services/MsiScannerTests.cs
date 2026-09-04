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
    //  PrimeAcesso 5.11 structure & Brazilian Portuguese Folder tests
    // ============================================================

    [Fact]
    public void Scan_ControladorFolder_MapsToController()
    {
        var controladorDir = Path.Combine(_tempRoot, "Controlador");
        Directory.CreateDirectory(controladorDir);
        File.Create(Path.Combine(controladorDir, "Controlador.msi")).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);
        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].TargetDirectory.Should().Be(_paths.Controller);
    }

    [Fact]
    public void Scan_OffLineFolder_MapsToOffLine()
    {
        var offlineDir = Path.Combine(_tempRoot, "OffLine");
        Directory.CreateDirectory(offlineDir);
        File.Create(Path.Combine(offlineDir, "OffLineService.msi")).Dispose();

        var scanner = new MsiScanner(_paths, DbChoice, _tempRoot);
        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].TargetDirectory.Should().Be(Path.Combine(_paths.NewAcessoRoot, "OffLine"));
    }

    [Fact]
    public void Scan_CompositeSqlServerFolder_WhenDbChoiceIsSqlServer_IncludesWinMsiAndSkipsWebApps()
    {
        var sqlServerDir = Path.Combine(_tempRoot, "SQLServer - Web - WebDataService - Win");
        Directory.CreateDirectory(sqlServerDir);
        File.Create(Path.Combine(sqlServerDir, "Win.msi")).Dispose();
        File.Create(Path.Combine(sqlServerDir, "WebAppDS.msi")).Dispose();
        File.Create(Path.Combine(sqlServerDir, "WebAppUI.msi")).Dispose();

        var scanner = new MsiScanner(_paths, "SQLServer", _tempRoot);
        var result = scanner.Scan();

        result.Should().ContainSingle();
        result[0].TargetDirectory.Should().Be(_paths.Win);
    }

    [Fact]
    public void Scan_CompositeOracleFolder_WhenDbChoiceIsSqlServer_IsSkipped()
    {
        var oracleDir = Path.Combine(_tempRoot, "Oracle - Web - WebDataService - Win");
        Directory.CreateDirectory(oracleDir);
        File.Create(Path.Combine(oracleDir, "Win.msi")).Dispose();

        var scanner = new MsiScanner(_paths, "SQLServer", _tempRoot);
        var result = scanner.Scan();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Scan_FullPrimeAcesso511Structure_ScansAllModulesCorrectly()
    {
        // Cria estrutura idêntica à versão 5.11
        var primeRoot = Path.Combine(_tempRoot, "PrimeAcesso_5.11");
        Directory.CreateDirectory(Path.Combine(primeRoot, "AutoAtendimento"));
        Directory.CreateDirectory(Path.Combine(primeRoot, "ConexBridge"));
        Directory.CreateDirectory(Path.Combine(primeRoot, "ConnectionRecord"));
        Directory.CreateDirectory(Path.Combine(primeRoot, "Controlador"));
        Directory.CreateDirectory(Path.Combine(primeRoot, "ControleAcesso"));
        Directory.CreateDirectory(Path.Combine(primeRoot, "Fabricantes"));
        Directory.CreateDirectory(Path.Combine(primeRoot, "OffLine"));
        Directory.CreateDirectory(Path.Combine(primeRoot, "Oracle - Web - WebDataService - Win"));
        Directory.CreateDirectory(Path.Combine(primeRoot, "SQLServer - Web - WebDataService - Win"));
        Directory.CreateDirectory(Path.Combine(primeRoot, "Task"));
        Directory.CreateDirectory(Path.Combine(primeRoot, "VisitAuthorization"));

        File.Create(Path.Combine(primeRoot, "AutoAtendimento", "AutoAtendimento.msi")).Dispose();
        File.Create(Path.Combine(primeRoot, "ConexBridge", "ConexBridge.msi")).Dispose();
        File.Create(Path.Combine(primeRoot, "ConnectionRecord", "ConnectionRecord.msi")).Dispose();
        File.Create(Path.Combine(primeRoot, "Controlador", "Controlador.msi")).Dispose();
        File.Create(Path.Combine(primeRoot, "ControleAcesso", "ControleAcesso.msi")).Dispose();
        File.Create(Path.Combine(primeRoot, "Fabricantes", "Fabricantes.msi")).Dispose();
        File.Create(Path.Combine(primeRoot, "OffLine", "StandAloneEx.msi")).Dispose();
        File.Create(Path.Combine(primeRoot, "Oracle - Web - WebDataService - Win", "Win.msi")).Dispose();
        File.Create(Path.Combine(primeRoot, "SQLServer - Web - WebDataService - Win", "Win.msi")).Dispose();
        File.Create(Path.Combine(primeRoot, "SQLServer - Web - WebDataService - Win", "WebAppDS.msi")).Dispose();
        File.Create(Path.Combine(primeRoot, "SQLServer - Web - WebDataService - Win", "WebAppUI.msi")).Dispose();
        File.Create(Path.Combine(primeRoot, "Task", "Task.msi")).Dispose();
        File.Create(Path.Combine(primeRoot, "VisitAuthorization", "VisitAuthorization.msi")).Dispose();

        var scanner = new MsiScanner(_paths, "SQLServer", primeRoot);
        var result = scanner.Scan();

        // 10 MSIs regulares (excluindo Oracle e os 2 WebApps)
        result.Should().HaveCount(10);
        result.Should().Contain(m => m.TargetDirectory == _paths.AutoAtendimento);
        result.Should().Contain(m => m.TargetDirectory == _paths.ConexBridge);
        result.Should().Contain(m => m.TargetDirectory == _paths.ConnectionRecord);
        result.Should().Contain(m => m.TargetDirectory == _paths.Controller);
        result.Should().Contain(m => m.TargetDirectory == _paths.ControleAcesso);
        result.Should().Contain(m => m.TargetDirectory == _paths.Fabricantes);
        result.Should().Contain(m => m.TargetDirectory == _paths.ControllerOfflineWinServiceEx);
        result.Should().Contain(m => m.TargetDirectory == _paths.Win);
        result.Should().Contain(m => m.TargetDirectory == _paths.Task);
        result.Should().Contain(m => m.TargetDirectory == _paths.VisitAuthorization);
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
