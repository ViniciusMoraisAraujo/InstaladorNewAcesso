using System.Xml;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class CoreWsConfigHelper
{
    private const string WatchdogConfig = "NewAcesso.Controlador.WatchDog.exe.config";
    private const string WsConfig = "NewAcesso.Controlador.Ws.exe.config";

    /// <summary>
    /// Apos instalar o CoreWs, atualiza os arquivos .config com os paths absolutos.
    /// </summary>
    public static bool UpdateConfigsAfterInstall(string targetDirectory)
    {
        var coreWsDir = ConfigHelperBase.NormalizeDirectoryPath(targetDirectory);
        var controllerDir = Path.GetDirectoryName(coreWsDir);
        var newAcessoRoot = controllerDir != null ? Path.GetDirectoryName(controllerDir) : null;

        if (string.IsNullOrEmpty(controllerDir) || string.IsNullOrEmpty(newAcessoRoot))
        {
            UIScope.WriteMessage($"[yellow]   [[AVISO]] Nao foi possivel determinar estrutura de diretorios a partir de: {MarkupHelper.Escape(coreWsDir)}[/]");
            return false;
        }

        var controleAcessoPath = Path.Combine(controllerDir, "ControleAcesso");
        var fabricantesPath = Path.Combine(controllerDir, "Fabricantes");
        var logsPath = Path.Combine(coreWsDir, "Logs");

        var watchdogOk = UpdateWatchdogConfig(coreWsDir, logsPath);
        var wsOk = UpdateWsConfig(coreWsDir, controleAcessoPath, logsPath, fabricantesPath);

        return watchdogOk && wsOk;
    }

    /// <summary>
    /// Atualiza os arquivos .config do CoreWs (Ws e WatchDog).
    /// </summary>
    public static bool UpdateConfig(string targetDirectory, string? controleAcessoPath = null, string? logsPath = null, string? fabricantesPath = null, string? biometricServer = null)
    {
        var coreWsDir = ConfigHelperBase.NormalizeDirectoryPath(targetDirectory);
        var controllerDir = Path.GetDirectoryName(coreWsDir);

        var resolvedControleAcesso = controleAcessoPath ??
            (controllerDir != null ? Path.Combine(controllerDir, "ControleAcesso") : @"C:\SoftPrime\NewAcesso\Controller\ControleAcesso");

        var resolvedFabricantes = fabricantesPath ??
            (controllerDir != null ? Path.Combine(controllerDir, "Fabricantes") : @"C:\SoftPrime\NewAcesso\Controller\Fabricantes");

        var resolvedLogs = logsPath ?? Path.Combine(coreWsDir, "Logs");

        var watchdogOk = UpdateWatchdogConfig(coreWsDir, resolvedLogs);
        var wsOk = UpdateWsConfig(coreWsDir, resolvedControleAcesso, resolvedLogs, resolvedFabricantes, biometricServer);

        return watchdogOk && wsOk;
    }

    private static bool UpdateWatchdogConfig(string coreWsDir, string logsPath)
    {
        var configPath = FindConfigFile(coreWsDir, WatchdogConfig);

        if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
        {
            UIScope.WriteMessage($"[gray]   [[INFO]] Watchdog .config nao encontrado em: {MarkupHelper.Escape(coreWsDir)}[/]");
            return false;
        }

        try
        {
            ConfigBackupService.BackupSingleFile(configPath);

            var doc = new XmlDocument();
            doc.Load(configPath);
            var appSettings = ConfigHelperBase.EnsureAppSettings(doc);
            ConfigHelperBase.SetKey(appSettings, "caminhoDosLogs", logsPath);
            ConfigHelperBase.SetKey(appSettings, "nomedoProcessodoNewAcessoController", "NewAcesso.Controlador.Ws");
            ConfigHelperBase.SetKey(appSettings, "finalizaProcessoNewAcessoControllerEmCasoTravamentoStop", "true");
            ConfigHelperBase.SetKey(appSettings, "segundosLimiteStoppingServicoController", "60");

            doc.Save(configPath);
            UIScope.WriteMessage($"   [green][[OK]] CoreWs WatchDog .config configurado.[/]");
            return true;
        }
        catch (Exception ex)
        {
            UIScope.WriteMessage($"[red]   [[ERRO]] Falha ao atualizar Watchdog .config: {MarkupHelper.Escape(ex.Message)}[/]");
            return false;
        }
    }

    private static bool UpdateWsConfig(string coreWsDir, string controleAcessoPath, string logsPath, string fabricantesPath, string? biometricServer = null)
    {
        var configPath = FindConfigFile(coreWsDir, WsConfig);

        if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
        {
            UIScope.WriteMessage($"[gray]   [[INFO]] Ws .config nao encontrado em: {MarkupHelper.Escape(coreWsDir)}[/]");
            return false;
        }

        try
        {
            ConfigBackupService.BackupSingleFile(configPath);

            var doc = new XmlDocument();
            doc.Load(configPath);
            var appSettings = ConfigHelperBase.EnsureAppSettings(doc);

            ConfigHelperBase.SetKey(appSettings, "caminhoDasDllsDoControleDeAcesso", controleAcessoPath);
            ConfigHelperBase.SetKey(appSettings, "caminhoDosLogs", logsPath);
            ConfigHelperBase.SetKey(appSettings, "caminhoDasDllsDosFabricantes", fabricantesPath);
            ConfigHelperBase.SetKey(appSettings, "caminhoDosLogsDeEquipamentos", logsPath);
            ConfigHelperBase.SetKey(appSettings, "quantidadeTentativaIniciarControlador", "-1");
            ConfigHelperBase.SetKey(appSettings, "intervaloTempoTentativasIniciarControlador", "5");

            // Atualiza endpoint biometrico se servidor for especificado
            if (!string.IsNullOrWhiteSpace(biometricServer))
            {
                var clientEndpoints = doc.SelectNodes("//system.serviceModel/client/endpoint");
                if (clientEndpoints != null)
                {
                    foreach (XmlNode node in clientEndpoints)
                    {
                        if (node is XmlElement ep && ep.GetAttribute("contract").Contains("INewAcessoBiometricsService", StringComparison.OrdinalIgnoreCase))
                        {
                            ep.SetAttribute("address", $"net.tcp://{biometricServer}:8734/");
                            var dnsNode = ep.SelectSingleNode("identity/dns");
                            if (dnsNode is XmlElement dnsEl)
                            {
                                dnsEl.SetAttribute("value", biometricServer);
                            }
                        }
                    }
                }
            }

            doc.Save(configPath);
            UIScope.WriteMessage($"   [green][[OK]] CoreWs .config configurado.[/]");
            return true;
        }
        catch (Exception ex)
        {
            UIScope.WriteMessage($"[red]   [[ERRO]] Falha ao atualizar Ws .config: {MarkupHelper.Escape(ex.Message)}[/]");
            return false;
        }
    }

    private static string? FindConfigFile(string dir, string targetName)
    {
        var direct = Path.Combine(dir, targetName);
        if (File.Exists(direct)) return direct;

        if (!Directory.Exists(dir)) return null;

        var matches = Directory.GetFiles(dir, "*.config");
        return matches.FirstOrDefault(f => Path.GetFileName(f).Equals(targetName, StringComparison.OrdinalIgnoreCase));
    }
}