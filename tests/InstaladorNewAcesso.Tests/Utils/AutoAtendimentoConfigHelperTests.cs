using System.Text.Json.Nodes;
using FluentAssertions;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Tests.Utils;

public class AutoAtendimentoConfigHelperTests : IDisposable
{
    private readonly string _tempRoot;

    public AutoAtendimentoConfigHelperTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "AutoAtendimentoTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public void UpdateConfig_UpdatesJsonSettingsCorrectly()
    {
        var apiDir = Path.Combine(_tempRoot, "WebAPI");
        var appDir = Path.Combine(_tempRoot, "WebAPP");
        Directory.CreateDirectory(apiDir);
        Directory.CreateDirectory(appDir);

        var apiConfig = Path.Combine(apiDir, "appsettings.json");
        var appConfig = Path.Combine(appDir, "appsettings.json");

        File.WriteAllText(apiConfig, @"{""AllowedHosts"":""*"",""ApiKey"":""old-key""}");
        File.WriteAllText(appConfig, @"{""AllowedHosts"":""*"",""URLapi"":""old-url"",""URLnewAcessoUI"":""old-ui""}");

        var result = AutoAtendimentoConfigHelper.UpdateConfig(_tempRoot, "http://localhost:8082", "http://localhost:8081", "new-api-key");

        result.Should().BeTrue();

        var apiNode = JsonNode.Parse(File.ReadAllText(apiConfig))!;
        apiNode["ApiKey"]!.GetValue<string>().Should().Be("new-api-key");

        var appNode = JsonNode.Parse(File.ReadAllText(appConfig))!;
        appNode["URLapi"]!.GetValue<string>().Should().Be("http://localhost:8082");
        appNode["URLnewAcessoUI"]!.GetValue<string>().Should().Be("http://localhost:8081");
        appNode["ApiKeys"]!["VisitasApiKey"]!.GetValue<string>().Should().Be("new-api-key");
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
