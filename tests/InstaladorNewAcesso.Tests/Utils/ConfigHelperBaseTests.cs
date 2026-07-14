using System.Xml;
using FluentAssertions;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Tests.Utils;

public class ConfigHelperBaseTests
{
    // ── EnsureAppSettings ──────────────────────────────────────────

    [Fact]
    public void EnsureAppSettings_OnEmptyDoc_CreatesConfigurationAndAppSettings()
    {
        var doc = new XmlDocument();

        var result = ConfigHelperBase.EnsureAppSettings(doc);

        Assert.NotNull(result);
        result.Name.Should().Be("appSettings");
        Assert.NotNull(doc.DocumentElement);
        doc.DocumentElement.Name.Should().Be("configuration");
    }

    [Fact]
    public void EnsureAppSettings_OnDocWithConfiguration_AddsAppSettings()
    {
        var doc = new XmlDocument();
        doc.AppendChild(doc.CreateElement("configuration"));

        var result = ConfigHelperBase.EnsureAppSettings(doc);

        Assert.NotNull(result);
        result.Name.Should().Be("appSettings");
        Assert.NotNull(doc.DocumentElement);
        doc.DocumentElement.ChildNodes.Count.Should().Be(1);
        Assert.NotNull(doc.DocumentElement.FirstChild);
        doc.DocumentElement.FirstChild.Name.Should().Be("appSettings");
    }

    [Fact]
    public void EnsureAppSettings_OnDocWithBoth_ReturnsExistingAppSettings()
    {
        var doc = new XmlDocument();
        var config = doc.CreateElement("configuration");
        doc.AppendChild(config);
        var existing = doc.CreateElement("appSettings");
        config.AppendChild(existing);

        var result = ConfigHelperBase.EnsureAppSettings(doc);

        result.Should().BeSameAs(existing);
        Assert.NotNull(doc.DocumentElement);
        doc.DocumentElement.ChildNodes.Count.Should().Be(1); // não duplica
    }

    // ── SetKey ─────────────────────────────────────────────────────

    [Fact]
    public void SetKey_OnEmptyAppSettings_AddsNewKey()
    {
        var doc = CreateDocWithAppSettings();
        var appSettings = ConfigHelperBase.EnsureAppSettings(doc);

        ConfigHelperBase.SetKey(appSettings, "TestKey", "TestValue");

        var add = appSettings.SelectSingleNode("add[@key='TestKey']") as XmlElement;
        Assert.NotNull(add);
        add.GetAttribute("value").Should().Be("TestValue");
    }

    [Fact]
    public void SetKey_OnExistingKey_UpdatesValue()
    {
        var doc = CreateDocWithAppSettings();
        var appSettings = ConfigHelperBase.EnsureAppSettings(doc);
        ConfigHelperBase.SetKey(appSettings, "TestKey", "OldValue");

        ConfigHelperBase.SetKey(appSettings, "TestKey", "NewValue");

        var add = appSettings.SelectSingleNode("add[@key='TestKey']") as XmlElement;
        Assert.NotNull(add);
        add.GetAttribute("value").Should().Be("NewValue");
    }

    [Fact]
    public void SetKey_OnExistingKey_SameValue_DoesNotDuplicate()
    {
        var doc = CreateDocWithAppSettings();
        var appSettings = ConfigHelperBase.EnsureAppSettings(doc);
        ConfigHelperBase.SetKey(appSettings, "TestKey", "SameValue");

        ConfigHelperBase.SetKey(appSettings, "TestKey", "SameValue");

        var adds = appSettings.SelectNodes("add[@key='TestKey']");
        Assert.NotNull(adds);
        adds.Count.Should().Be(1); // não duplica
    }

    [Fact]
    public void SetKey_KeyMatch_IsCaseInsensitive()
    {
        var doc = CreateDocWithAppSettings();
        var appSettings = ConfigHelperBase.EnsureAppSettings(doc);
        ConfigHelperBase.SetKey(appSettings, "TESTKEY", "Original");

        ConfigHelperBase.SetKey(appSettings, "testkey", "Updated");

        var add = appSettings.SelectSingleNode("add[@key='TESTKEY']") as XmlElement;
        Assert.NotNull(add);
        add.GetAttribute("value").Should().Be("Updated");
    }

    [Fact]
    public void SetKey_MultipleKeys_AllPersist()
    {
        var doc = CreateDocWithAppSettings();
        var appSettings = ConfigHelperBase.EnsureAppSettings(doc);

        ConfigHelperBase.SetKey(appSettings, "Key1", "Value1");
        ConfigHelperBase.SetKey(appSettings, "Key2", "Value2");
        ConfigHelperBase.SetKey(appSettings, "Key3", "Value3");

        appSettings.ChildNodes.Count.Should().Be(3);
    }

    // ── Helpers ────────────────────────────────────────────────────

    private static XmlDocument CreateDocWithAppSettings()
    {
        var doc = new XmlDocument();
        ConfigHelperBase.EnsureAppSettings(doc);
        return doc;
    }
}
