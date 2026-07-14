using FluentAssertions;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Tests.Utils;

public class ControleAcessoConfigHelperTests : IDisposable
{
    private readonly string _tempRoot;

    public ControleAcessoConfigHelperTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "ControleAcessoTests_" + Guid.NewGuid().ToString("N"));
    }

    // ── Helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Cria a estrutura de diretórios esperada:
    ///   {tempRoot}/NewAcesso/Controller/ControleAcesso/
    /// </summary>
    private string CreateControleAcessoDir()
    {
        var dir = Path.Combine(_tempRoot, "NewAcesso", "Controller", "ControleAcesso");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void CreateMinimalIni(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "; Test INI\n[Section]\nKey=Value\n");
    }

    private static void CreateIniWithKey(string path, string key, string value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, $"; Test INI\n[Section]\n{key} = '{value}'\n");
    }

    // ============================================================
    //  UpdateIniAfterInstall — File not found
    // ============================================================

    [Fact]
    public void UpdateIniAfterInstall_IniNotFound_ReturnsFalse()
    {
        var dir = CreateControleAcessoDir();

        var result = ControleAcessoConfigHelper.UpdateIniAfterInstall(dir);

        result.Should().BeFalse();
    }

    // ============================================================
    //  UpdateIniAfterInstall — Adds key
    // ============================================================

    [Fact]
    public void UpdateIniAfterInstall_NoExistingKey_AddsPathDataSouce()
    {
        var dir = CreateControleAcessoDir();
        var iniPath = Path.Combine(dir, "PrimeAcesso.ControleAcesso.ini");
        CreateMinimalIni(iniPath);

        var result = ControleAcessoConfigHelper.UpdateIniAfterInstall(dir);

        result.Should().BeTrue();

        var lines = File.ReadAllLines(iniPath);
        lines.Should().Contain(l => l.Contains("PathDataSouce_NewAcessoConnectionRecord"));
    }

    [Fact]
    public void UpdateIniAfterInstall_KeyValueContainsAbsoluteDbPath()
    {
        var dir = CreateControleAcessoDir();
        var iniPath = Path.Combine(dir, "PrimeAcesso.ControleAcesso.ini");
        CreateMinimalIni(iniPath);

        ControleAcessoConfigHelper.UpdateIniAfterInstall(dir);

        var lines = File.ReadAllLines(iniPath);
        var line = lines.First(l => l.Contains("PathDataSouce_NewAcessoConnectionRecord"));

        line.Should().Contain(Path.Combine("ConnectionRecord", "DataBase", "NewAcessoConnection.s3db"));
        // Deve ser caminho absoluto
        line.Should().Contain(_tempRoot);
    }

    // ============================================================
    //  UpdateIniAfterInstall — Updates existing key
    // ============================================================

    [Fact]
    public void UpdateIniAfterInstall_ExistingKey_UpdatesValue()
    {
        var dir = CreateControleAcessoDir();
        var iniPath = Path.Combine(dir, "PrimeAcesso.ControleAcesso.ini");
        CreateIniWithKey(iniPath, "PathDataSouce_NewAcessoConnectionRecord", @"old\path");

        var result = ControleAcessoConfigHelper.UpdateIniAfterInstall(dir);

        result.Should().BeTrue();

        var lines = File.ReadAllLines(iniPath);
        var line = lines.First(l => l.Contains("PathDataSouce_NewAcessoConnectionRecord"));
        line.Should().Contain(_tempRoot);
        line.Should().NotContain("old");
    }

    // ============================================================
    //  UpdateIniAfterInstall — Preserves other keys
    // ============================================================

    [Fact]
    public void UpdateIniAfterInstall_PreservesOtherKeys()
    {
        var dir = CreateControleAcessoDir();
        var iniPath = Path.Combine(dir, "PrimeAcesso.ControleAcesso.ini");
        File.WriteAllText(iniPath, "; Test\nOtherKey = 'Preserved'\nPathDataSouce_NewAcessoConnectionRecord = 'old'\n");

        ControleAcessoConfigHelper.UpdateIniAfterInstall(dir);

        var lines = File.ReadAllLines(iniPath);
        lines.Should().Contain(l => l.Contains("OtherKey") && l.Contains("Preserved"));
    }

    // ============================================================
    //  UpdateIniAfterInstall — Invalid directory structure
    // ============================================================

    [Fact]
    public void UpdateIniAfterInstall_SingleLevelDeep_StillNavigatesUp()
    {
        // Um diretório com apenas 1 nível acima:
        // Path.GetDirectoryName("{tempRoot}/ControleAcesso") = "{tempRoot}"
        // Path.GetDirectoryName("{tempRoot}") = raiz do drive (ex: C:\)
        // Isso NÃO lança null, então o helper tenta navegar e calcula paths.
        // O importante é que não lance exceção.
        var singleLevel = Path.Combine(_tempRoot, "ControleAcesso");
        Directory.CreateDirectory(singleLevel);
        var iniPath = Path.Combine(singleLevel, "PrimeAcesso.ControleAcesso.ini");
        CreateMinimalIni(iniPath);

        var act = () => ControleAcessoConfigHelper.UpdateIniAfterInstall(singleLevel);
        act.Should().NotThrow();
    }

    // ============================================================
    //  UpdateIniAfterInstall — Path navigation test
    // ============================================================

    [Fact]
    public void UpdateIniAfterInstall_CorrectDirectoryStructure_PathNavigatesCorrectly()
    {
        // Estrutura: {tempRoot}/NewAcesso/Controller/ControleAcesso
        // Path.GetDirectoryName两次: ControleAcesso -> Controller -> NewAcesso
        var dir = CreateControleAcessoDir();
        var iniPath = Path.Combine(dir, "PrimeAcesso.ControleAcesso.ini");
        CreateMinimalIni(iniPath);

        ControleAcessoConfigHelper.UpdateIniAfterInstall(dir);

        var lines = File.ReadAllLines(iniPath);
        var line = lines.First(l => l.Contains("PathDataSouce_NewAcessoConnectionRecord"));

        // Should reference {tempRoot}/NewAcesso/ConnectionRecord/DataBase/NewAcessoConnection.s3db
        var expectedDbPath = Path.Combine(_tempRoot, "NewAcesso", "ConnectionRecord", "DataBase", "NewAcessoConnection.s3db");
        line.Should().Contain(expectedDbPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, true); }
            catch { }
        }
    }
}
