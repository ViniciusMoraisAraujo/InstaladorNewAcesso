using System.Xml;
using FluentAssertions;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Tests.Utils;

/// <summary>
/// Testes para TaskConfigHelper. Como o método UpdateConfigAfterInstall
/// chama AnsiConsole.Ask (input do usuário), testamos apenas os caminhos
/// que retornam antes de chegar ao prompt: arquivo não encontrado e
/// estrutura de diretórios inválida.
/// </summary>
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

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, true); }
            catch { }
        }
    }
}
