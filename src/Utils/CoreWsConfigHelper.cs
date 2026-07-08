using System.Xml;
using Spectre.Console;

namespace InstaladorNewAcesso.Utils;

public static class CoreWsConfigHelper
{
    private const string WatchdogConfig = "NewAcesso.Controlador.Watchdog.exe.config";
    private const string WsConfig = "NewAcesso.Controlador.Ws.exe.config";

    /// <summary>
    /// Após instalar o CoreWs, atualiza os dois arquivos .config com os paths absolutos
    /// calculados a partir do diretório de instalação.
    /// </summary>
    public static bool UpdateConfigsAfterInstall(string targetDirectory)
    {
        // targetDirectory = {BasePath}\NewAcesso\Controller\CoreWs
        var coreWsDir = targetDirectory;
        var controllerDir = Path.GetDirectoryName(coreWsDir); // {BasePath}\NewAcesso\Controller
        var newAcessoRoot = Path.GetDirectoryName(controllerDir); // {BasePath}\NewAcesso

        if (string.IsNullOrEmpty(controllerDir) || string.IsNullOrEmpty(newAcessoRoot))
        {
            AnsiConsole.MarkupLine($"[yellow]   [AVISO] Não foi possível determinar estrutura de diretórios a partir de: {coreWsDir.EscapeMarkup()}[/]");
            return false;
        }

        var controleAcessoPath = Path.Combine(controllerDir, "ControleAcesso");
        var fabricantesPath = Path.Combine(controllerDir, "Fabricantes");
        var logsPath = Path.Combine(coreWsDir, "Logs");

        bool watchdogOk = UpdateWatchdogConfig(coreWsDir, logsPath);
        bool wsOk = UpdateWsConfig(coreWsDir, controleAcessoPath, logsPath, fabricantesPath);

        return watchdogOk || wsOk;
    }

    private static bool UpdateWatchdogConfig(string coreWsDir, string logsPath)
    {
        var configPath = Path.Combine(coreWsDir, WatchdogConfig);

        if (!File.Exists(configPath))
        {
            AnsiConsole.MarkupLine($"[gray]   [INFO] Watchdog .config não encontrado: {configPath.EscapeMarkup()}[/]");
            return false;
        }

        try
        {
            var doc = new XmlDocument();
            doc.Load(configPath);
            var appSettings = ConfigHelperBase.EnsureAppSettings(doc);
            ConfigHelperBase.SetKey(appSettings, "caminhoDosLogs", logsPath);

            doc.Save(configPath);
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]   [ERRO] Falha ao atualizar Watchdog .config: {ex.Message.EscapeMarkup()}[/]");
            return false;
        }
    }

    private static bool UpdateWsConfig(string coreWsDir, string controleAcessoPath, string logsPath, string fabricantesPath)
    {
        var configPath = Path.Combine(coreWsDir, WsConfig);

        if (!File.Exists(configPath))
        {
            AnsiConsole.MarkupLine($"[gray]   [INFO] Ws .config não encontrado: {configPath.EscapeMarkup()}[/]");
            return false;
        }

        try
        {
            var doc = new XmlDocument();
            doc.Load(configPath);
            var appSettings = ConfigHelperBase.EnsureAppSettings(doc);

            ConfigHelperBase.SetKey(appSettings, "caminhoDasDllsDoControleDeAcesso", controleAcessoPath);
            ConfigHelperBase.SetKey(appSettings, "caminhoDosLogs", logsPath);
            ConfigHelperBase.SetKey(appSettings, "caminhoDasDllsDosFabricantes", fabricantesPath);
            ConfigHelperBase.SetKey(appSettings, "caminhoDosLogsDeEquipamentos", logsPath);
            ConfigHelperBase.SetKey(appSettings, "quantidadeTentativaIniciarControlador", "-1");
            ConfigHelperBase.SetKey(appSettings, "intervaloTempoTentativasIniciarControlador", "5");

            doc.Save(configPath);
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]   [ERRO] Falha ao atualizar Ws .config: {ex.Message.EscapeMarkup()}[/]");
            return false;
        }
    }


}
