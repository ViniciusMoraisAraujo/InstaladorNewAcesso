using System.Xml;
using Spectre.Console;

namespace InstaladorNewAcesso.Utils;

public static class StandAloneImConfigHelper
{
    private const string ConfigFileName = "PrimeAcesso.Controller.StandAloneIm.exe.config";

    public static bool UpdateConfigAfterInstall(string targetDirectory)
    {
        var configPath = Path.Combine(targetDirectory, ConfigFileName);

        if (!File.Exists(configPath))
        {
            AnsiConsole.MarkupLine($"[gray]   [INFO] StandAloneIm .config não encontrado: {configPath.EscapeMarkup()}[/]");
            return false;
        }

        try
        {
            // targetDirectory = {BasePath}\NewAcesso\ControllerOffline\WinService_In
            var serviceDir = targetDirectory;
            var controllerOfflineDir = Path.GetDirectoryName(serviceDir);
            var newAcessoRoot = Path.GetDirectoryName(controllerOfflineDir);

            if (string.IsNullOrEmpty(controllerOfflineDir) || string.IsNullOrEmpty(newAcessoRoot))
            {
                AnsiConsole.MarkupLine($"[yellow]   [AVISO] Não foi possível determinar estrutura de diretórios a partir de: {serviceDir.EscapeMarkup()}[/]");
                return false;
            }

            var connectionRecordDbPath = Path.Combine(newAcessoRoot, "ConnectionRecord", "DataBase", "NewAcessoConnection.s3db");

            var doc = new XmlDocument();
            doc.Load(configPath);
            var appSettings = ConfigHelperBase.EnsureAppSettings(doc);

            ConfigHelperBase.SetKey(appSettings, "PathDataSource_NewAcessoConnectionRecord", connectionRecordDbPath);
            ConfigHelperBase.SetKey(appSettings, "NomePastaLog", "Logs");
            ConfigHelperBase.SetKey(appSettings, "QuantidadeDiasCriacaoLog", "8");
            ConfigHelperBase.SetKey(appSettings, "Endereco_ServidorBiometrico", "localhost");

            AnsiConsole.MarkupLine($"\n   [bold yellow]Configuração do StandAloneIm:[/]");
            var idConexao = AnsiConsole.Ask<string>("   [bold yellow]ID_Conexao_NewAcessoConnectionRecord[/] (padrão: [gray]1[/]):");
            if (string.IsNullOrWhiteSpace(idConexao)) idConexao = "1";
            ConfigHelperBase.SetKey(appSettings, "ID_Conexao_NewAcessoConnectionRecord", idConexao);

            doc.Save(configPath);
            AnsiConsole.MarkupLine($"   [green][OK] StandAloneIm .config configurado com sucesso.[/]");
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]   [ERRO] Falha ao atualizar StandAloneIm .config: {ex.Message.EscapeMarkup()}[/]");
            return false;
        }
    }


}
