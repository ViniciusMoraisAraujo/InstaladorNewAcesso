using System.Xml;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class WinConfigHelper
{
    private const string ConfigFileName = "PrimeAcesso.Win.exe.config";
    private const string IniFileName = "PrimeAcesso.Win.ini";

    /// <summary>
    /// Atualiza os arquivos de configuracao do modulo Win Desktop (.exe.config e .ini).
    /// </summary>
    public static bool UpdateConfig(string targetDirectory, string? serviceUri = null, string? biometricServer = null, string? webcamDir = null)
    {
        var normalizedDir = ConfigHelperBase.NormalizeDirectoryPath(targetDirectory);
        var configOk = UpdateAppConfig(normalizedDir, serviceUri, biometricServer);
        var iniOk = UpdateIni(normalizedDir, webcamDir);

        return configOk || iniOk;
    }

    private static bool UpdateAppConfig(string targetDirectory, string? serviceUri, string? biometricServer)
    {
        var configPath = Path.Combine(targetDirectory, ConfigFileName);

        if (!File.Exists(configPath))
        {
            UIScope.WriteMessage($"[gray]   [[INFO]] Win .config nao encontrado em: {MarkupHelper.Escape(configPath)}[/]");
            return false;
        }

        try
        {
            ConfigBackupService.BackupSingleFile(configPath);

            var doc = new XmlDocument();
            doc.Load(configPath);
            var appSettings = ConfigHelperBase.EnsureAppSettings(doc);

            var resolvedUri = serviceUri ?? "http://localhost:8080/DSPrimeAcesso.svc";
            var resolvedBio = biometricServer ?? "localhost";

            ConfigHelperBase.SetKey(appSettings, "ServiceURI_PrimeAcesso", resolvedUri);
            ConfigHelperBase.SetKey(appSettings, "Endereco_ServidorBiometrico", resolvedBio);

            // Sanitiza endpoints WCF locais se houver IPs remotos legados
            var clientEndpoints = doc.SelectNodes("//system.serviceModel/client/endpoint");
            if (clientEndpoints != null)
            {
                foreach (XmlNode node in clientEndpoints)
                {
                    if (node is XmlElement ep)
                    {
                        var addr = ep.GetAttribute("address");
                        if (addr.Contains("192.168.0.156", StringComparison.OrdinalIgnoreCase))
                        {
                            ep.SetAttribute("address", addr.Replace("192.168.0.156", resolvedBio));
                            var dnsNode = ep.SelectSingleNode("identity/dns");
                            if (dnsNode is XmlElement dnsEl)
                            {
                                dnsEl.SetAttribute("value", resolvedBio);
                            }
                        }
                        else if (addr.Contains("localhost", StringComparison.OrdinalIgnoreCase) && !string.Equals(resolvedBio, "localhost", StringComparison.OrdinalIgnoreCase))
                        {
                            ep.SetAttribute("address", addr.Replace("localhost", resolvedBio));
                            var dnsNode = ep.SelectSingleNode("identity/dns");
                            if (dnsNode is XmlElement dnsEl)
                            {
                                dnsEl.SetAttribute("value", resolvedBio);
                            }
                        }
                    }
                }
            }

            doc.Save(configPath);
            UIScope.WriteMessage("   [green][[OK]] Win .config configurado com sucesso.[/]");
            return true;
        }
        catch (Exception ex)
        {
            UIScope.WriteMessage($"[red]   [[ERRO]] Falha ao atualizar Win .config: {MarkupHelper.Escape(ex.Message)}[/]");
            return false;
        }
    }

    private static bool UpdateIni(string targetDirectory, string? webcamDir)
    {
        var iniPath = Path.Combine(targetDirectory, IniFileName);

        if (!File.Exists(iniPath))
        {
            UIScope.WriteMessage($"[gray]   [[INFO]] Win .ini nao encontrado em: {MarkupHelper.Escape(iniPath)}[/]");
            return false;
        }

        try
        {
            var winDir = targetDirectory;
            var newAcessoRoot = Path.GetDirectoryName(winDir);
            var basePath = newAcessoRoot != null ? Path.GetDirectoryName(newAcessoRoot) : @"C:\SoftPrime";

            var resolvedWebcam = webcamDir ?? Path.Combine(basePath ?? @"C:\SoftPrime", "ImgWebCam") + @"\";

            ConfigBackupService.BackupSingleFile(iniPath);

            var lines = File.ReadAllLines(iniPath).ToList();
            var mod = IniHelperBase.SetIniKey(lines, "DiretorioDasImagensCapturadasPelaWebCam", resolvedWebcam, useQuotes: true, section: "GERAL");

            if (mod)
            {
                File.WriteAllLines(iniPath, lines);
                UIScope.WriteMessage("   [green][[OK]] Win .ini configurado com sucesso.[/]");
            }
            else
            {
                UIScope.WriteMessage("   [gray][[INFO]] Win .ini ja esta atualizado.[/]");
            }

            return true;
        }
        catch (Exception ex)
        {
            UIScope.WriteMessage($"[red]   [[ERRO]] Falha ao atualizar Win .ini: {MarkupHelper.Escape(ex.Message)}[/]");
            return false;
        }
    }
}