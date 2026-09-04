using System.Xml;
using FluentAssertions;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Tests.Utils;

public class WinConfigHelperTests : IDisposable
{
    private readonly string _tempRoot;

    public WinConfigHelperTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "WinConfigTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    private static void CreateMinimalAppConfig(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var doc = new XmlDocument();
        doc.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <appSettings>
    <add key=""ServiceURI_PrimeAcesso"" value=""http://oldhost/DS.svc"" />
  </appSettings>
  <system.serviceModel>
    <client>
      <endpoint address=""net.tcp://192.168.0.156:8736/"" binding=""netTcpBinding"" name=""Test"">
        <identity><dns value=""betel.softprime.com.br"" /></identity>
      </endpoint>
    </client>
  </system.serviceModel>
</configuration>");
        doc.Save(path);
    }

    private static void CreateMinimalIni(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, @"[GERAL]
DiretorioDasImagensCapturadasPelaWebCam = 'old\path\'
HabilitaLeitorBiometrico = true");
    }

    [Fact]
    public void UpdateConfig_UpdatesAppConfigAndIniCorrectly()
    {
        var configPath = Path.Combine(_tempRoot, "PrimeAcesso.Win.exe.config");
        var iniPath = Path.Combine(_tempRoot, "PrimeAcesso.Win.ini");

        CreateMinimalAppConfig(configPath);
        CreateMinimalIni(iniPath);

        var result = WinConfigHelper.UpdateConfig(_tempRoot, "http://localhost:8080/DSPrimeAcesso.svc", "localhost", @"C:\SoftPrime\ImgWebCam\");

        result.Should().BeTrue();

        var doc = new XmlDocument();
        doc.Load(configPath);
        doc.SelectSingleNode("//add[@key='ServiceURI_PrimeAcesso']")!.Attributes!["value"]!.Value
            .Should().Be("http://localhost:8080/DSPrimeAcesso.svc");
        doc.SelectSingleNode("//add[@key='Endereco_ServidorBiometrico']")!.Attributes!["value"]!.Value
            .Should().Be("localhost");

        // WCF endpoint sanitized
        var ep = doc.SelectSingleNode("//system.serviceModel/client/endpoint")!;
        ep.Attributes!["address"]!.Value.Should().Be("net.tcp://localhost:8736/");

        // INI updated
        var iniLines = File.ReadAllLines(iniPath);
        iniLines.Should().Contain(l => l.Contains(@"C:\SoftPrime\ImgWebCam\"));
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
