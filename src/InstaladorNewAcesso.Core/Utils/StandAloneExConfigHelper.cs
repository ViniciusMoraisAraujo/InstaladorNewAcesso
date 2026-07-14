using System.Xml;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class StandAloneExConfigHelper
{
    private const string ConfigFileName = "PrimeAcesso.Controller.StandAloneEx.exe.config";

    /// <summary>
    /// Apos instalar o WinService_Ex, verifica se o diretorio de destino
    /// contem o PrimeAcesso.Controller.StandAloneEx.exe.config e, se sim,
    /// solicita o ID_Conexao e atualiza todas as chaves.
    /// </summary>
    public static bool UpdateConfigAfterInstall(string targetDirectory)
    {
        var configPath = Path.Combine(targetDirectory, ConfigFileName);

        if (!File.Exists(configPath))
        {
            UIScope.WriteMessage($"[gray]   [[INFO]] StandAloneEx .config nao encontrado: {MarkupHelper.Escape(configPath)}[/]");
            return false;
        }

        try
        {
            // targetDirectory = {BasePath}\NewAcesso\ControllerOffline\WinService_Ex
            var serviceDir = targetDirectory;
            var controllerOfflineDir = Path.GetDirectoryName(serviceDir);   // {BasePath}\NewAcesso\ControllerOffline
            var newAcessoRoot = Path.GetDirectoryName(controllerOfflineDir); // {BasePath}\NewAcesso

            if (string.IsNullOrEmpty(controllerOfflineDir) || string.IsNullOrEmpty(newAcessoRoot))
            {
                UIScope.WriteMessage($"[yellow]   [[AVISO]] Nao foi possivel determinar estrutura de diretorios a partir de: {MarkupHelper.Escape(serviceDir)}[/]");
                return false;
            }

            var connectionRecordDbPath = Path.Combine(newAcessoRoot, "ConnectionRecord", "DataBase", "NewAcessoConnection.s3db");

            var doc = new XmlDocument();
            doc.Load(configPath);
            var appSettings = ConfigHelperBase.EnsureAppSettings(doc);

            // Chaves fixas
            ConfigHelperBase.SetKey(appSettings, "PathDataSource_NewAcessoConnectionRecord", connectionRecordDbPath);
            ConfigHelperBase.SetKey(appSettings, "NomePastaLog", "Logs");
            ConfigHelperBase.SetKey(appSettings, "QuantidadeDiasCriacaoLog", "8");
            ConfigHelperBase.SetKey(appSettings, "Endereco_ServidorBiometrico", "localhost");

            // Chave que exige input do usuario
            UIScope.WriteMessage("\n   [bold yellow]Configuracao do StandAloneEx:[/]");
            var idConexao = UIScope.AskInput("   [bold yellow]ID_Conexao_NewAcessoConnectionRecord[/] (padrao: [gray]1[/]):", "1");
            ConfigHelperBase.SetKey(appSettings, "ID_Conexao_NewAcessoConnectionRecord", idConexao);

            doc.Save(configPath);
            UIScope.WriteMessage("   [green][[OK]] StandAloneEx .config configurado com sucesso.[/]");
            return true;
        }
        catch (Exception ex)
        {
            UIScope.WriteMessage($"[red]   [[ERRO]] Falha ao atualizar StandAloneEx .config: {MarkupHelper.Escape(ex.Message)}[/]");
            return false;
        }
    }
}
