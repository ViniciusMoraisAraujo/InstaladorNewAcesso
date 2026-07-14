using System.Xml;
using FluentAssertions;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Tests.Utils;

/// <summary>
/// Testes para StandAloneImConfigHelper. Como o método UpdateConfigAfterInstall
/// chama AnsiConsole.Ask (input do usuário), testamos apenas os caminhos
/// que retornam antes de chegar ao prompt.
/// </summary>
public class StandAloneImConfigHelperTests : IDisposable
{
    private readonly string _tempRoot;

    public StandAloneImConfigHelperTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "StandAloneImTests_" + Guid.NewGuid().ToString("N"));
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
        var dir = Path.Combine(_tempRoot, "NewAcesso", "ControllerOffline", "WinService_In");
        Directory.CreateDirectory(dir);

        var result = StandAloneImConfigHelper.UpdateConfigAfterInstall(dir);

        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateConfigAfterInstall_TooFewLevels_ReturnsFalse()
    {
        var shallowDir = Path.Combine(_tempRoot, "WinService_In");
        Directory.CreateDirectory(shallowDir);
        CreateMinimalConfig(Path.Combine(shallowDir, "PrimeAcesso.Controller.StandAloneIm.exe.config"));

        var result = StandAloneImConfigHelper.UpdateConfigAfterInstall(shallowDir);

        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateConfigAfterInstall_CorruptedXml_ReturnsFalse()
    {
        var dir = Path.Combine(_tempRoot, "NewAcesso", "ControllerOffline", "WinService_In");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "PrimeAcesso.Controller.StandAloneIm.exe.config"), "invalid {{{");

        var result = StandAloneImConfigHelper.UpdateConfigAfterInstall(dir);

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
