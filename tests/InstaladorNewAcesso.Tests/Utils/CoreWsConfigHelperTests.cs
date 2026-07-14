using System.Xml;
using FluentAssertions;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Tests.Utils;

public class CoreWsConfigHelperTests : IDisposable
{
    private readonly string _tempRoot;

    public CoreWsConfigHelperTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "CoreWsTests_" + Guid.NewGuid().ToString("N"));
    }

    // ── Helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Cria a estrutura: {tempRoot}/NewAcesso/Controller/CoreWs/
    /// </summary>
    private string CreateCoreWsDir()
    {
        var dir = Path.Combine(_tempRoot, "NewAcesso", "Controller", "CoreWs");
        Directory.CreateDirectory(dir);
        return dir;
    }

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

    // ============================================================
    //  No config files
    // ============================================================

    [Fact]
    public void UpdateConfigsAfterInstall_NoConfigFiles_ReturnsFalse()
    {
        var dir = CreateCoreWsDir();

        var result = CoreWsConfigHelper.UpdateConfigsAfterInstall(dir);

        result.Should().BeFalse();
    }

    // ============================================================
    //  Watchdog config only
    // ============================================================

    [Fact]
    public void UpdateConfigsAfterInstall_OnlyWatchdogConfig_ReturnsFalse()
    {
        // BUG FIX: return watchdogOk && wsOk — both must succeed.
        // When only Watchdog exists, wsOk is false, so overall is false.
        var dir = CreateCoreWsDir();
        CreateMinimalConfig(Path.Combine(dir, "NewAcesso.Controlador.Watchdog.exe.config"));

        var result = CoreWsConfigHelper.UpdateConfigsAfterInstall(dir);

        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateConfigsAfterInstall_WatchdogConfig_AddsCaminhoDosLogs()
    {
        var dir = CreateCoreWsDir();
        var configPath = Path.Combine(dir, "NewAcesso.Controlador.Watchdog.exe.config");
        CreateMinimalConfig(configPath);

        CoreWsConfigHelper.UpdateConfigsAfterInstall(dir);

        var doc = new XmlDocument();
        doc.Load(configPath);
        var node = doc.SelectSingleNode("//add[@key='caminhoDosLogs']");
        node.Should().NotBeNull();
        node!.Attributes!["value"]!.Value.Should().Be(Path.Combine(dir, "Logs"));
    }

    // ============================================================
    //  Ws config only
    // ============================================================

    [Fact]
    public void UpdateConfigsAfterInstall_OnlyWsConfig_ReturnsFalse()
    {
        // BUG FIX: return watchdogOk && wsOk — both must succeed.
        // When only Ws exists, watchdogOk is false, so overall is false.
        var dir = CreateCoreWsDir();
        CreateMinimalConfig(Path.Combine(dir, "NewAcesso.Controlador.Ws.exe.config"));

        var result = CoreWsConfigHelper.UpdateConfigsAfterInstall(dir);

        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateConfigsAfterInstall_WsConfig_AddsAllRequiredKeys()
    {
        var dir = CreateCoreWsDir();
        var configPath = Path.Combine(dir, "NewAcesso.Controlador.Ws.exe.config");
        CreateMinimalConfig(configPath);

        CoreWsConfigHelper.UpdateConfigsAfterInstall(dir);

        var doc = new XmlDocument();
        doc.Load(configPath);

        var controllerDir = Path.GetDirectoryName(dir)!;
        var newAcessoRoot = Path.GetDirectoryName(controllerDir)!;

        // caminhoDasDllsDoControleDeAcesso
        var node1 = doc.SelectSingleNode("//add[@key='caminhoDasDllsDoControleDeAcesso']");
        node1.Should().NotBeNull();
        node1!.Attributes!["value"]!.Value.Should().Be(Path.Combine(controllerDir, "ControleAcesso"));

        // caminhoDosLogs
        var node2 = doc.SelectSingleNode("//add[@key='caminhoDosLogs']");
        node2.Should().NotBeNull();
        node2!.Attributes!["value"]!.Value.Should().Be(Path.Combine(dir, "Logs"));

        // caminhoDasDllsDosFabricantes
        var node3 = doc.SelectSingleNode("//add[@key='caminhoDasDllsDosFabricantes']");
        node3.Should().NotBeNull();
        node3!.Attributes!["value"]!.Value.Should().Be(Path.Combine(controllerDir, "Fabricantes"));

        // caminhoDosLogsDeEquipamentos
        var node4 = doc.SelectSingleNode("//add[@key='caminhoDosLogsDeEquipamentos']");
        node4.Should().NotBeNull();
        node4!.Attributes!["value"]!.Value.Should().Be(Path.Combine(dir, "Logs"));

        // quantidadeTentativaIniciarControlador
        var node5 = doc.SelectSingleNode("//add[@key='quantidadeTentativaIniciarControlador']");
        node5.Should().NotBeNull();
        node5!.Attributes!["value"]!.Value.Should().Be("-1");

        // intervaloTempoTentativasIniciarControlador
        var node6 = doc.SelectSingleNode("//add[@key='intervaloTempoTentativasIniciarControlador']");
        node6.Should().NotBeNull();
        node6!.Attributes!["value"]!.Value.Should().Be("5");
    }

    // ============================================================
    //  Both configs
    // ============================================================

    [Fact]
    public void UpdateConfigsAfterInstall_BothConfigs_UpdatesBoth()
    {
        var dir = CreateCoreWsDir();
        CreateMinimalConfig(Path.Combine(dir, "NewAcesso.Controlador.Watchdog.exe.config"));
        CreateMinimalConfig(Path.Combine(dir, "NewAcesso.Controlador.Ws.exe.config"));

        var result = CoreWsConfigHelper.UpdateConfigsAfterInstall(dir);

        result.Should().BeTrue();

        // Verify Watchdog
        var watchdogDoc = new XmlDocument();
        watchdogDoc.Load(Path.Combine(dir, "NewAcesso.Controlador.Watchdog.exe.config"));
        watchdogDoc.SelectSingleNode("//add[@key='caminhoDosLogs']").Should().NotBeNull();

        // Verify Ws
        var wsDoc = new XmlDocument();
        wsDoc.Load(Path.Combine(dir, "NewAcesso.Controlador.Ws.exe.config"));
        wsDoc.SelectSingleNode("//add[@key='caminhoDasDllsDoControleDeAcesso']").Should().NotBeNull();
    }

    // ============================================================
    //  Invalid directory structure
    // ============================================================

    [Fact]
    public void UpdateConfigsAfterInstall_TooFewLevels_ReturnsFalse()
    {
        var shallowDir = Path.Combine(_tempRoot, "CoreWs");
        Directory.CreateDirectory(shallowDir);

        var result = CoreWsConfigHelper.UpdateConfigsAfterInstall(shallowDir);

        result.Should().BeFalse();
    }

    // ============================================================
    //  Corrupted XML
    // ============================================================

    [Fact]
    public void UpdateConfigsAfterInstall_CorruptedWatchdogXml_ReturnsFalse()
    {
        var dir = CreateCoreWsDir();
        File.WriteAllText(Path.Combine(dir, "NewAcesso.Controlador.Watchdog.exe.config"), "invalid xml {{{");
        // Ws config doesn't exist either, so both fail

        var result = CoreWsConfigHelper.UpdateConfigsAfterInstall(dir);

        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateConfigsAfterInstall_CorruptedWatchdogXml_WsSucceeds_ReturnsFalse()
    {
        // BUG FIX: return watchdogOk && wsOk — both must succeed
        var dir = CreateCoreWsDir();
        File.WriteAllText(Path.Combine(dir, "NewAcesso.Controlador.Watchdog.exe.config"), "invalid xml {{{");
        CreateMinimalConfig(Path.Combine(dir, "NewAcesso.Controlador.Ws.exe.config"));

        var result = CoreWsConfigHelper.UpdateConfigsAfterInstall(dir);

        result.Should().BeFalse(); // watchdogOk is false, so && returns false
    }

    [Fact]
    public void UpdateConfigsAfterInstall_CorruptedWsXml_WatchdogSucceeds_ReturnsFalse()
    {
        // BUG FIX: return watchdogOk && wsOk — both configs must succeed
        var dir = CreateCoreWsDir();
        CreateMinimalConfig(Path.Combine(dir, "NewAcesso.Controlador.Watchdog.exe.config"));
        File.WriteAllText(Path.Combine(dir, "NewAcesso.Controlador.Ws.exe.config"), "invalid xml {{{");

        var result = CoreWsConfigHelper.UpdateConfigsAfterInstall(dir);

        result.Should().BeFalse(); // wsOk is false, so && returns false
    }

    // ============================================================
    //  Preserves existing keys
    // ============================================================

    [Fact]
    public void UpdateConfigsAfterInstall_WsConfig_PreservesOtherKeys()
    {
        var dir = CreateCoreWsDir();
        var configPath = Path.Combine(dir, "NewAcesso.Controlador.Ws.exe.config");

        var doc = new XmlDocument();
        var decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
        doc.AppendChild(decl);
        var config = doc.CreateElement("configuration");
        doc.AppendChild(config);
        var appSettings = doc.CreateElement("appSettings");
        config.AppendChild(appSettings);
        var add = doc.CreateElement("add");
        add.SetAttribute("key", "ExistingKey");
        add.SetAttribute("value", "ExistingValue");
        appSettings.AppendChild(add);
        doc.Save(configPath);

        CoreWsConfigHelper.UpdateConfigsAfterInstall(dir);

        var reloaded = new XmlDocument();
        reloaded.Load(configPath);
        reloaded.SelectSingleNode("//add[@key='ExistingKey']")!.Attributes!["value"]!.Value
            .Should().Be("ExistingValue");
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
