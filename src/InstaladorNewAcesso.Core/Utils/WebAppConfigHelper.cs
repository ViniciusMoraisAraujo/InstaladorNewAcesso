using System.Xml;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class WebAppConfigHelper
{
    private const string ConfigFileName = "web.config";

    /// <summary>
    /// Atualiza o web.config do WebAppDS: PathDataSource + ID_Conexao.
    /// </summary>
    public static bool UpdateWebAppDSConfig(string targetDirectory, string? idConexao = null, string? dbPath = null)
    {
        return UpdateConfig(targetDirectory, "WebAppDS", dsConfig =>
        {
            var normalizedDir = ConfigHelperBase.NormalizeDirectoryPath(targetDirectory);
            var newAcessoRoot = Path.GetDirectoryName(normalizedDir);

            var resolvedDbPath = dbPath;
            if (string.IsNullOrEmpty(resolvedDbPath) && !string.IsNullOrEmpty(newAcessoRoot))
            {
                resolvedDbPath = Path.Combine(newAcessoRoot, "ConnectionRecord", "DataBase", "NewAcessoConnection.s3db");
            }

            if (string.IsNullOrEmpty(resolvedDbPath))
            {
                resolvedDbPath = @"C:\SoftPrime\NewAcesso\ConnectionRecord\DataBase\NewAcessoConnection.s3db";
            }

            var resolvedId = idConexao ?? "1";

            ConfigHelperBase.SetKey(dsConfig, "PathDataSource_NewAcessoConnectionRecord", resolvedDbPath);
            ConfigHelperBase.SetKey(dsConfig, "ID_Conexao_NewAcessoConnectionRecord", resolvedId);

            return true;
        });
    }

    /// <summary>
    /// Atualiza o web.config do WebAppUI: ServiceURI, PathDataSource, ID_Conexao, CaminhoDasDllsDeFabricantes.
    /// </summary>
    public static bool UpdateWebAppUIConfig(string targetDirectory, string? idConexao = null, string? dbPath = null, string? serviceUri = null, string? fabricantesPath = null)
    {
        return UpdateConfig(targetDirectory, "WebAppUI", uiConfig =>
        {
            var normalizedDir = ConfigHelperBase.NormalizeDirectoryPath(targetDirectory);
            var newAcessoRoot = Path.GetDirectoryName(normalizedDir);

            var resolvedDbPath = dbPath;
            if (string.IsNullOrEmpty(resolvedDbPath) && !string.IsNullOrEmpty(newAcessoRoot))
            {
                resolvedDbPath = Path.Combine(newAcessoRoot, "ConnectionRecord", "DataBase", "NewAcessoConnection.s3db");
            }

            if (string.IsNullOrEmpty(resolvedDbPath))
            {
                resolvedDbPath = @"C:\SoftPrime\NewAcesso\ConnectionRecord\DataBase\NewAcessoConnection.s3db";
            }

            var resolvedFabricantes = fabricantesPath;
            if (string.IsNullOrEmpty(resolvedFabricantes) && !string.IsNullOrEmpty(newAcessoRoot))
            {
                resolvedFabricantes = Path.Combine(newAcessoRoot, "Controller", "Fabricantes");
            }

            if (string.IsNullOrEmpty(resolvedFabricantes))
            {
                resolvedFabricantes = @"C:\SoftPrime\NewAcesso\Controller\Fabricantes";
            }

            var resolvedId = idConexao ?? "1";
            var resolvedUri = serviceUri ?? "http://localhost:8080/DSPrimeAcesso.svc";

            ConfigHelperBase.SetKey(uiConfig, "ServiceURI_PrimeAcesso", resolvedUri);
            ConfigHelperBase.SetKey(uiConfig, "PathDataSource_NewAcessoConnectionRecord", resolvedDbPath);
            ConfigHelperBase.SetKey(uiConfig, "ID_Conexao_NewAcessoConnectionRecord", resolvedId);
            ConfigHelperBase.SetKey(uiConfig, "CaminhoDasDllsDeFabricantes", resolvedFabricantes);

            return true;
        });
    }

    private static bool UpdateConfig(string targetDirectory, string label, Func<XmlElement, bool> apply)
    {
        var normalizedDir = ConfigHelperBase.NormalizeDirectoryPath(targetDirectory);
        var configPath = Path.Combine(normalizedDir, ConfigFileName);
        if (!File.Exists(configPath))
        {
            // Tenta maiuscula Web.config
            configPath = Path.Combine(normalizedDir, "Web.config");
        }

        if (!File.Exists(configPath))
        {
            UIScope.WriteMessage($"[gray]   [[INFO]] web.config nao encontrado em: {MarkupHelper.Escape(normalizedDir)}[/]");
            return false;
        }

        try
        {
            ConfigBackupService.BackupSingleFile(configPath);

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