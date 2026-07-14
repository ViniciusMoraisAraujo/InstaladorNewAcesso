using System.Xml;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class StandAloneImConfigHelper
{
    private const string ConfigFileName = "PrimeAcesso.Controller.StandAloneIm.exe.config";

    public static bool UpdateConfigAfterInstall(string targetDirectory)
    {
        var configPath = Path.Combine(targetDirectory, ConfigFileName);

        if (!File.Exists(configPath))
        {
            UIScope.WriteMessage($"[gray]   [[INFO]] StandAloneIm .config nao encontrado: {MarkupHelper.Escape(configPath)}[/]");
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
                UIScope.WriteMessage($"[yellow]   [[AVISO]] Nao foi possivel determinar estrutura de diretorios a partir de: {MarkupHelper.Escape(serviceDir)}[/]");
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

            UIScope.WriteMessage("\n   [bold yellow]Configuracao do StandAloneIm:[/]");
            var idConexao = UIScope.AskInput("   [bold yellow]ID_Conexao_NewAcessoConnectionRecord[/] (padrao: [gray]1[/]):", "1");
            ConfigHelperBase.SetKey(appSettings, "ID_Conexao_NewAcessoConnectionRecord", idConexao);

            doc.Save(configPath);
            UIScope.WriteMessage("   [green][[OK]] StandAloneIm .config configurado com sucesso.[/]");
            return true;
        }
        catch (Exception ex)
        {
            UIScope.WriteMessage($"[red]   [[ERRO]] Falha ao atualizar StandAloneIm .config: {MarkupHelper.Escape(ex.Message)}[/]");
            return false;
        }
    }
}
