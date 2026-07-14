using FluentAssertions;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Tests.Utils;

public class WebAppConfigHelperTests : IDisposable
{
    private readonly string _tempRoot;

    public WebAppConfigHelperTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "WebAppConfigTests_" + Guid.NewGuid().ToString("N"));
    }

    // ============================================================
    //  UpdateWebAppDSConfig — File not found
    // ============================================================

    [Fact]
    public void UpdateWebAppDSConfig_WebConfigNotFound_ReturnsFalse()
    {
        var dir = Path.Combine(_tempRoot, "WebAppDS");
        Directory.CreateDirectory(dir);

        var result = WebAppConfigHelper.UpdateWebAppDSConfig(dir);

        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateWebAppDSConfig_NonExistentDirectory_ReturnsFalse()
    {
        var result = WebAppConfigHelper.UpdateWebAppDSConfig(Path.Combine(_tempRoot, "NonExistent"));

        result.Should().BeFalse();
    }

    // ============================================================
    //  UpdateWebAppUIConfig — File not found
    // ============================================================

    [Fact]
    public void UpdateWebAppUIConfig_WebConfigNotFound_ReturnsFalse()
    {
        var dir = Path.Combine(_tempRoot, "WebAppUI");
        Directory.CreateDirectory(dir);

        var result = WebAppConfigHelper.UpdateWebAppUIConfig(dir);

        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateWebAppUIConfig_NonExistentDirectory_ReturnsFalse()
    {
        var result = WebAppConfigHelper.UpdateWebAppUIConfig(Path.Combine(_tempRoot, "NonExistent"));

        result.Should().BeFalse();
    }

    // ============================================================
    //  Shared UpdateConfig method — Corrupted XML
    // ============================================================

    [Fact]
    public void UpdateWebAppDSConfig_CorruptedXml_ReturnsFalse()
    {
        var dir = Path.Combine(_tempRoot, "WebAppDS");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "web.config"), "invalid xml {{{");

        var result = WebAppConfigHelper.UpdateWebAppDSConfig(dir);

        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateWebAppUIConfig_CorruptedXml_ReturnsFalse()
    {
        var dir = Path.Combine(_tempRoot, "WebAppUI");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "web.config"), "invalid xml {{{");

        var result = WebAppConfigHelper.UpdateWebAppUIConfig(dir);

        result.Should().BeFalse();
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
