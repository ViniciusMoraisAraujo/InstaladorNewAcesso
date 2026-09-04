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

    private static void CreateConfigWithWcf(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var doc = new XmlDocument();
        var decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
        doc.AppendChild(decl);
        var config = doc.CreateElement("configuration");
        doc.AppendChild(config);
        var appSettings = doc.CreateElement("appSettings");
        config.AppendChild(appSettings);

        var sm = doc.CreateElement("system.serviceModel");
        var client = doc.CreateElement("client");
        var ep = doc.CreateElement("endpoint");
        ep.SetAttribute("address", "net.tcp://localhost:8734/");
        ep.SetAttribute("contract", "BiometricsService.INewAcessoBiometricsService");
        var id = doc.CreateElement("identity");
        var dns = doc.CreateElement("dns");
        dns.SetAttribute("value", "localhost");
        id.AppendChild(dns);
        ep.AppendChild(id);
        client.AppendChild(ep);
        sm.AppendChild(client);
        config.AppendChild(sm);

        doc.Save(path);
    }

    // ============================================================
    //  Watchdog config only
    // ============================================================

    [Fact]
    public void UpdateConfigsAfterInstall_OnlyWatchdogConfig_ReturnsFalse()
    {
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
        var node = doc.SelectSingleNode("//add[@key=\'caminhoDosLogs\']");
        node.Should().NotBeNull();
        node!.Attributes!["value"]!.Value.Should().Be(Path.Combine(dir, "Logs"));
    }

    // ============================================================
    //  Ws config only
    // ============================================================

    [Fact]
    public void UpdateConfigsAfterInstall_OnlyWsConfig_ReturnsFalse()
    {
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

        // caminhoDasDllsDoControleDeAcesso
        var node1 = doc.SelectSingleNode("//add[@key=\'caminhoDasDllsDoControleDeAcesso\']");
        node1.Should().NotBeNull();
        node1!.Attributes!["value"]!.Value.Should().Be(Path.Combine(controllerDir, "ControleAcesso"));

        // caminhoDosLogs
        var node2 = doc.SelectSingleNode("//add[@key=\'caminhoDosLogs\']");
        node2.Should().NotBeNull();
        node2!.Attributes!["value"]!.Value.Should().Be(Path.Combine(dir, "Logs"));

        // caminhoDasDllsDosFabricantes
        var node3 = doc.SelectSingleNode("//add[@key=\'caminhoDasDllsDosFabricantes\']");
        node3.Should().NotBeNull();
        node3!.Attributes!["value"]!.Value.Should().Be(Path.Combine(controllerDir, "Fabricantes"));

        // caminhoDosLogsDeEquipamentos
        var node4 = doc.SelectSingleNode("//add[@key=\'caminhoDosLogsDeEquipamentos\']");
        node4.Should().NotBeNull();
        node4!.Attributes!["value"]!.Value.Should().Be(Path.Combine(dir, "Logs"));

        // quantidadeTentativaIniciarControlador
        var node5 = doc.SelectSingleNode("//add[@key=\'quantidadeTentativaIniciarControlador\']");
        node5.Should().NotBeNull();
        node5!.Attributes!["value"]!.Value.Should().Be("-1");

        // intervaloTempoTentativasIniciarControlador
        var node6 = doc.SelectSingleNode("//add[@key=\'intervaloTempoTentativasIniciarControlador\']");
        node6.Should().NotBeNull();
        node6!.Attributes!["value"]!.Value.Should().Be("5");
    }

    // ============================================================
    //  Both configs & Trailing Slashes
    // ============================================================

    [Fact]
    public void UpdateConfigsAfterInstall_BothConfigs_WithTrailingSlash_UpdatesBoth()
    {
        var dir = CreateCoreWsDir();
        CreateMinimalConfig(Path.Combine(dir, "NewAcesso.Controlador.Watchdog.exe.config"));
        CreateMinimalConfig(Path.Combine(dir, "NewAcesso.Controlador.Ws.exe.config"));

        var result = CoreWsConfigHelper.UpdateConfigsAfterInstall(dir + @"\");

        result.Should().BeTrue();

        // Verify Watchdog
        var watchdogDoc = new XmlDocument();
        watchdogDoc.Load(Path.Combine(dir, "NewAcesso.Controlador.Watchdog.exe.config"));
        watchdogDoc.SelectSingleNode("//add[@key=\'caminhoDosLogs\']").Should().NotBeNull();

        // Verify Ws
        var wsDoc = new XmlDocument();
        wsDoc.Load(Path.Combine(dir, "NewAcesso.Controlador.Ws.exe.config"));
        wsDoc.SelectSingleNode("//add[@key=\'caminhoDasDllsDoControleDeAcesso\']").Should().NotBeNull();
    }

    [Fact]
    public void UpdateConfig_WithBiometricServer_UpdatesWcfClientEndpoint()
    {
        var dir = CreateCoreWsDir();
        CreateMinimalConfig(Path.Combine(dir, "NewAcesso.Controlador.Watchdog.exe.config"));
        var wsPath = Path.Combine(dir, "NewAcesso.Controlador.Ws.exe.config");
        CreateConfigWithWcf(wsPath);

        var result = CoreWsConfigHelper.UpdateConfig(dir, biometricServer: "192.168.1.100");

        result.Should().BeTrue();

        var wsDoc = new XmlDocument();
        wsDoc.Load(wsPath);
        var epNode = wsDoc.SelectSingleNode("//system.serviceModel/client/endpoint") as XmlElement;
        epNode.Should().NotBeNull();
        epNode!.GetAttribute("address").Should().Be("net.tcp://192.168.1.100:8734/");
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

        var result = CoreWsConfigHelper.UpdateConfigsAfterInstall(dir);

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