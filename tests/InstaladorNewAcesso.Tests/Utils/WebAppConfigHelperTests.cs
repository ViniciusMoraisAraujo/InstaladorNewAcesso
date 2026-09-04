using System.Xml;
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

    private static void CreateMinimalWebConfig(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var doc = new XmlDocument();
        var decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
        doc.AppendChild(decl);
        var config = doc.CreateElement("configuration");
        doc.AppendChild(config);
        var appSettings = doc.CreateElement("appSettings");
        config.AppendChild(appSettings);
        doc.Save(path);
    }

    // ============================================================
    //  UpdateWebAppDSConfig
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

    [Fact]
    public void UpdateWebAppDSConfig_WithTrailingSlash_UpdatesSuccessfully()
    {
        var dir = Path.Combine(_tempRoot, "NewAcesso", "WebAppDS");
        Directory.CreateDirectory(dir);
        var configPath = Path.Combine(dir, "web.config");
        CreateMinimalWebConfig(configPath);

        var result = WebAppConfigHelper.UpdateWebAppDSConfig(dir + @"\", idConexao: "5");

        result.Should().BeTrue();

        var doc = new XmlDocument();
        doc.Load(configPath);
        var idNode = doc.SelectSingleNode("//add[@key=\'ID_Conexao_NewAcessoConnectionRecord\']") as XmlElement;
        idNode.Should().NotBeNull();
        idNode!.GetAttribute("value").Should().Be("5");

        var dbNode = doc.SelectSingleNode("//add[@key=\'PathDataSource_NewAcessoConnectionRecord\']") as XmlElement;
        dbNode.Should().NotBeNull();
        var expectedDb = Path.Combine(_tempRoot, "NewAcesso", "ConnectionRecord", "DataBase", "NewAcessoConnection.s3db");
        dbNode!.GetAttribute("value").Should().Be(expectedDb);
    }

    // ============================================================
    //  UpdateWebAppUIConfig
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

    [Fact]
    public void UpdateWebAppUIConfig_WithTrailingSlash_UpdatesSuccessfully()
    {
        var dir = Path.Combine(_tempRoot, "NewAcesso", "WebAppUI");
        Directory.CreateDirectory(dir);
        var configPath = Path.Combine(dir, "web.config");
        CreateMinimalWebConfig(configPath);

        var result = WebAppConfigHelper.UpdateWebAppUIConfig(dir + @"\", idConexao: "1", serviceUri: "http://myserver:8080/DSPrimeAcesso.svc");

        result.Should().BeTrue();

        var doc = new XmlDocument();
        doc.Load(configPath);
        var uriNode = doc.SelectSingleNode("//add[@key=\'ServiceURI_PrimeAcesso\']") as XmlElement;
        uriNode.Should().NotBeNull();
        uriNode!.GetAttribute("value").Should().Be("http://myserver:8080/DSPrimeAcesso.svc");

        var fabNode = doc.SelectSingleNode("//add[@key=\'CaminhoDasDllsDeFabricantes\']") as XmlElement;
        fabNode.Should().NotBeNull();
        var expectedFab = Path.Combine(_tempRoot, "NewAcesso", "Controller", "Fabricantes");
        fabNode!.GetAttribute("value").Should().Be(expectedFab);
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