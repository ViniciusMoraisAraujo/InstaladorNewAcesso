using System.Xml;
using FluentAssertions;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Tests.Utils;

public class TaskConfigHelperTests : IDisposable
{
    private readonly string _tempRoot;

    public TaskConfigHelperTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "TaskConfigTests_" + Guid.NewGuid().ToString("N"));
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

    [Fact]
    public void UpdateConfigAfterInstall_ConfigNotFound_ReturnsFalse()
    {
        var dir = Path.Combine(_tempRoot, "NewAcesso", "Controller", "Task");
        Directory.CreateDirectory(dir);

        var result = TaskConfigHelper.UpdateConfigAfterInstall(dir);

        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateConfigAfterInstall_TooFewLevels_ReturnsFalse()
    {
        var shallowDir = Path.Combine(_tempRoot, "Task");
        Directory.CreateDirectory(shallowDir);
        CreateMinimalConfig(Path.Combine(shallowDir, "PrimeAcesso.Task.exe.config"));

        var result = TaskConfigHelper.UpdateConfigAfterInstall(shallowDir);

        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateConfigAfterInstall_CorruptedXml_ReturnsFalse()
    {
        var dir = Path.Combine(_tempRoot, "NewAcesso", "Controller", "Task");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "PrimeAcesso.Task.exe.config"), "invalid {{{");

        var result = TaskConfigHelper.UpdateConfigAfterInstall(dir);

        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateConfig_WithAlternateConfigName_UpdatesSuccessfully()
    {
        var dir = Path.Combine(_tempRoot, "NewAcesso", "Controller", "Task");
        Directory.CreateDirectory(dir);
        var configPath = Path.Combine(dir, "PrimeAcesso.Controller.Task.exe.config");
        CreateMinimalConfig(configPath);

        var result = TaskConfigHelper.UpdateConfig(dir, idConexao: "2", fabricante: "Hikvision", horaExclusao: "18:30");

        result.Should().BeTrue();

        var doc = new XmlDocument();
        doc.Load(configPath);
        var idNode = doc.SelectSingleNode("//add[@key=\'ID_Conexao_NewAcessoConnectionRecord\']") as XmlElement;
        idNode.Should().NotBeNull();
        idNode!.GetAttribute("value").Should().Be("2");

        var fabNode = doc.SelectSingleNode("//add[@key=\'FabricanteEquipamentoFacial\']") as XmlElement;
        fabNode.Should().NotBeNull();
        fabNode!.GetAttribute("value").Should().Be("Hikvision");

        var horaNode = doc.SelectSingleNode("//add[@key=\'HoraExecucaoExclusaoFacial\']") as XmlElement;
        horaNode.Should().NotBeNull();
        horaNode!.GetAttribute("value").Should().Be("18:30");
    }

    [Fact]
    public void UpdateConfig_WithTrailingSlash_ResolvesDbPathCorrectly()
    {
        var dir = Path.Combine(_tempRoot, "NewAcesso", "Controller", "Task");
        Directory.CreateDirectory(dir);
        var configPath = Path.Combine(dir, "PrimeAcesso.Task.exe.config");
        CreateMinimalConfig(configPath);

        var result = TaskConfigHelper.UpdateConfig(dir + @"\");

        result.Should().BeTrue();

        var doc = new XmlDocument();
        doc.Load(configPath);
        var dbNode = doc.SelectSingleNode("//add[@key=\'PathDataSource_NewAcessoConnectionRecord\']") as XmlElement;
        dbNode.Should().NotBeNull();
        var expectedDb = Path.Combine(_tempRoot, "NewAcesso", "ConnectionRecord", "DataBase", "NewAcessoConnection.s3db");
        dbNode!.GetAttribute("value").Should().Be(expectedDb);
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