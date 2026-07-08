using System.Xml;
using Spectre.Console;

namespace InstaladorNewAcesso.Utils;

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
                AnsiConsole.MarkupLine($"[yellow]   [AVISO] Não foi possível determinar estrutura de diretórios.[/]");
                return false;
            }

            var dbPath = Path.Combine(newAcessoRoot, "ConnectionRecord", "DataBase", "NewAcessoConnection.s3db");

            ConfigHelperBase.SetKey(dsConfig, "PathDataSource_NewAcessoConnectionRecord", dbPath);

            AnsiConsole.MarkupLine($"\n   [bold yellow]Configuração do WebAppDS:[/]");
            var idConexao = AnsiConsole.Ask<string>("   [bold yellow]ID_Conexao_NewAcessoConnectionRecord[/] (padrão: [gray]1[/]):");
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
                AnsiConsole.MarkupLine($"[yellow]   [AVISO] Não foi possível determinar estrutura de diretórios.[/]");
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
            AnsiConsole.MarkupLine($"[gray]   [INFO] web.config não encontrado em: {configPath.EscapeMarkup()}[/]");
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
            AnsiConsole.MarkupLine($"   [green][OK] web.config do {label.EscapeMarkup()} configurado.[/]");
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]   [ERRO] Falha ao atualizar web.config do {label.EscapeMarkup()}: {ex.Message.EscapeMarkup()}[/]");
            return false;
        }
    }


}
