using System.Xml;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class WebAppConfigHelper
{
    private const string ConfigFileName = "web.config";

    /// <summary>
    /// Atualiza o web.config do WebAppDS: PathDataSource + ID_Conexao (com prompt).
    /// </summary>
    public static bool UpdateWebAppDSConfig(string targetDirectory)
    {
        return UpdateConfig(targetDirectory, "WebAppDS", dsConfig =>
        {
            var newAcessoRoot = Path.GetDirectoryName(targetDirectory);
            if (string.IsNullOrEmpty(newAcessoRoot))
            {
                UIScope.WriteMessage($"[yellow]   [[AVISO]] Não foi possível determinar estrutura de diretórios.[/]");
                return false;
            }

            var dbPath = Path.Combine(newAcessoRoot, "ConnectionRecord", "DataBase", "NewAcessoConnection.s3db");

            ConfigHelperBase.SetKey(dsConfig, "PathDataSource_NewAcessoConnectionRecord", dbPath);

            UIScope.WriteMessage($"\n   [bold yellow]Configuração do WebAppDS:[/]");
            var idConexao = UIScope.AskInput("   [bold yellow]ID_Conexao_NewAcessoConnectionRecord[/] (padrão: [gray]1[/]):");
            if (string.IsNullOrWhiteSpace(idConexao)) idConexao = "1";
            ConfigHelperBase.SetKey(dsConfig, "ID_Conexao_NewAcessoConnectionRecord", idConexao);

            return true;
        });
    }

    /// <summary>
    /// Atualiza o web.config do WebAppUI: ServiceURI, PathDataSource, CaminhoDasDllsDeFabricantes.
    /// </summary>
    public static bool UpdateWebAppUIConfig(string targetDirectory)
    {
        return UpdateConfig(targetDirectory, "WebAppUI", uiConfig =>
        {
            var newAcessoRoot = Path.GetDirectoryName(targetDirectory);
            if (string.IsNullOrEmpty(newAcessoRoot))
            {
                UIScope.WriteMessage($"[yellow]   [[AVISO]] Não foi possível determinar estrutura de diretórios.[/]");
                return false;
            }

            var controllerDir = Path.Combine(newAcessoRoot, "Controller");
            var dbPath = Path.Combine(newAcessoRoot, "ConnectionRecord", "DataBase", "NewAcessoConnection.s3db");
            var fabricantesPath = Path.Combine(controllerDir, "Fabricantes");

            ConfigHelperBase.SetKey(uiConfig, "ServiceURI_PrimeAcesso", "http://localhost:8080/DSPrimeAcesso.svc");
            ConfigHelperBase.SetKey(uiConfig, "PathDataSource_NewAcessoConnectionRecord", dbPath);
            ConfigHelperBase.SetKey(uiConfig, "CaminhoDasDllsDeFabricantes", fabricantesPath);

            return true;
        });
    }

    private static bool UpdateConfig(string targetDirectory, string label, Func<XmlElement, bool> apply)
    {
        var configPath = Path.Combine(targetDirectory, ConfigFileName);

        if (!File.Exists(configPath))
        {
            UIScope.WriteMessage($"[gray]   [[INFO]] web.config não encontrado em: {MarkupHelper.Escape(configPath)}[/]");
            return false;
        }

        try
        {
            var doc = new XmlDocument();
            doc.Load(configPath);
            var appSettings = ConfigHelperBase.EnsureAppSettings(doc);

            if (!apply(appSettings))
                return false;

            doc.Save(configPath);
            UIScope.WriteMessage($"   [green][[OK]] web.config do {MarkupHelper.Escape(label)} configurado.[/]");
            return true;
        }
        catch (Exception ex)
        {
            UIScope.WriteMessage($"[red]   [[ERRO]] Falha ao atualizar web.config do {MarkupHelper.Escape(label)}: {MarkupHelper.Escape(ex.Message)}[/]");
            return false;
        }
    }


}
