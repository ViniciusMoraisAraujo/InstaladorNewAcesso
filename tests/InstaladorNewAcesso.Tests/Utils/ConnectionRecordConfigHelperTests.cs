using System.Xml;
using FluentAssertions;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Tests.Utils;

public class ConnectionRecordConfigHelperTests : IDisposable
{
    private readonly string _tempRoot;

    public ConnectionRecordConfigHelperTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "ConnRecordTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    // ── Helpers ────────────────────────────────────────────────────

    private static void CreateMinimalConfig(string path)
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

    private static void CreateConfigWithKey(string path, string key, string value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var doc = new XmlDocument();
        var decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
        doc.AppendChild(decl);
        var config = doc.CreateElement("configuration");
        doc.AppendChild(config);
        var appSettings = doc.CreateElement("appSettings");
        config.AppendChild(appSettings);
        var add = doc.CreateElement("add");
        add.SetAttribute("key", key);
        add.SetAttribute("value", value);
        appSettings.AppendChild(add);
        doc.Save(path);
    }

    // ============================================================
    //  UpdateConfigAfterInstall — File not found
    // ============================================================

    [Fact]
    public void UpdateConfigAfterInstall_ConfigNotFound_ReturnsFalse()
    {
        var result = ConnectionRecordConfigHelper.UpdateConfigAfterInstall(_tempRoot);

        result.Should().BeFalse();
    }

    // ============================================================
    //  UpdateConfigAfterInstall — Adds PathDataSource key
    // ============================================================

    [Fact]
    public void UpdateConfigAfterInstall_NoExistingKey_AddsPathDataSource()
    {
        var configPath = Path.Combine(_tempRoot, "PrimeAcesso.ConnectionRecord.exe.config");
        CreateMinimalConfig(configPath);

        var result = ConnectionRecordConfigHelper.UpdateConfigAfterInstall(_tempRoot);

        result.Should().BeTrue();

        var doc = new XmlDocument();
        doc.Load(configPath);
        var node = doc.SelectSingleNode("//add[@key='PathDataSource']");
        node.Should().NotBeNull();
        node!.Attributes!["value"]!.Value.Should().Contain("NewAcessoConnection.s3db");
    }

    [Fact]
    public void UpdateConfigAfterInstall_NoExistingKey_PathDataSourceContainsDataBaseFolder()
    {
        var configPath = Path.Combine(_tempRoot, "PrimeAcesso.ConnectionRecord.exe.config");
        CreateMinimalConfig(configPath);

        ConnectionRecordConfigHelper.UpdateConfigAfterInstall(_tempRoot);

        var doc = new XmlDocument();
        doc.Load(configPath);
        var node = doc.SelectSingleNode("//add[@key='PathDataSource']");
        var value = node!.Attributes!["value"]!.Value;

        value.Should().EndWith(Path.Combine("DataBase", "NewAcessoConnection.s3db"));
        value.Should().StartWith(_tempRoot);
    }

    // ============================================================
    //  UpdateConfigAfterInstall — Updates existing key
    // ============================================================

    [Fact]
    public void UpdateConfigAfterInstall_ExistingKey_UpdatesValue()
    {
        var configPath = Path.Combine(_tempRoot, "PrimeAcesso.ConnectionRecord.exe.config");
        CreateConfigWithKey(configPath, "PathDataSource", @"old\path\s3db");

        var result = ConnectionRecordConfigHelper.UpdateConfigAfterInstall(_tempRoot);

        result.Should().BeTrue();

        var doc = new XmlDocument();
        doc.Load(configPath);
        var node = doc.SelectSingleNode("//add[@key='PathDataSource']");
        var value = node!.Attributes!["value"]!.Value;

        value.Should().Contain("NewAcessoConnection.s3db");
        value.Should().NotContain("old");
    }

    [Fact]
    public void UpdateConfigAfterInstall_ExistingKey_AlreadyCorrect_DoesNotChange()
    {
        var configPath = Path.Combine(_tempRoot, "PrimeAcesso.ConnectionRecord.exe.config");
        var expectedValue = Path.Combine(_tempRoot, "DataBase", "NewAcessoConnection.s3db");
        CreateConfigWithKey(configPath, "PathDataSource", expectedValue);

        var result = ConnectionRecordConfigHelper.UpdateConfigAfterInstall(_tempRoot);

        result.Should().BeTrue();

        var doc = new XmlDocument();
        doc.Load(configPath);
        var node = doc.SelectSingleNode("//add[@key='PathDataSource']");
        node!.Attributes!["value"]!.Value.Should().Be(expectedValue);
    }

    // ============================================================
    //  UpdateConfigAfterInstall — Preserves other keys
    // ============================================================

    [Fact]
    public void UpdateConfigAfterInstall_PreservesOtherKeys()
    {
        var configPath = Path.Combine(_tempRoot, "PrimeAcesso.ConnectionRecord.exe.config");
        CreateMinimalConfig(configPath);

        // Adiciona outra chave antes
        var doc = new XmlDocument();
        doc.Load(configPath);
        var appSettings = doc.SelectSingleNode("//appSettings")!;
        var add = doc.CreateElement("add");
        add.SetAttribute("key", "ExistingKey");
        add.SetAttribute("value", "ExistingValue");
        appSettings.AppendChild(add);
        doc.Save(configPath);

        ConnectionRecordConfigHelper.UpdateConfigAfterInstall(_tempRoot);

        var reloaded = new XmlDocument();
        reloaded.Load(configPath);
        reloaded.SelectSingleNode("//add[@key='ExistingKey']")!.Attributes!["value"]!.Value
            .Should().Be("ExistingValue");
        reloaded.SelectSingleNode("//add[@key='PathDataSource']").Should().NotBeNull();
    }

    // ============================================================
    //  UpdateConfigAfterInstall — Corrupted XML
    // ============================================================

    [Fact]
    public void UpdateConfigAfterInstall_CorruptedXml_ReturnsFalse()
    {
        var configPath = Path.Combine(_tempRoot, "PrimeAcesso.ConnectionRecord.exe.config");
        File.WriteAllText(configPath, "not valid xml content {{{");

        var result = ConnectionRecordConfigHelper.UpdateConfigAfterInstall(_tempRoot);

        result.Should().BeFalse();
    }

    // ============================================================
    //  UpdateConfigAfterInstall — Empty XML
    // ============================================================

    [Fact]
    public void UpdateConfigAfterInstall_EmptyXml_HandledGracefully()
    {
        var configPath = Path.Combine(_tempRoot, "PrimeAcesso.ConnectionRecord.exe.config");
        File.WriteAllText(configPath, "");

        var result = ConnectionRecordConfigHelper.UpdateConfigAfterInstall(_tempRoot);

        // XmlDocument.Load("") throws, so should return false
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
