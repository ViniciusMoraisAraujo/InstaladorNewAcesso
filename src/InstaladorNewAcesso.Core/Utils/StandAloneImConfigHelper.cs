using System.Xml;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class StandAloneImConfigHelper
{
    private static readonly string[] PossibleConfigFileNames =
    [
        "PrimeAcesso.Controller.StandAloneIn.exe.config",
        "PrimeAcesso.Controller.StandAloneIm.exe.config",
        "PrimeAcesso.StandAloneIn.exe.config",
        "PrimeAcesso.StandAloneIm.exe.config"
    ];

    public static bool UpdateConfigAfterInstall(string targetDirectory)
    {
        var serviceDir = ConfigHelperBase.NormalizeDirectoryPath(targetDirectory);
        var controllerOfflineDir = Path.GetDirectoryName(serviceDir);
        var newAcessoRoot = controllerOfflineDir != null ? Path.GetDirectoryName(controllerOfflineDir) : null;

        if (string.IsNullOrEmpty(controllerOfflineDir) || string.IsNullOrEmpty(newAcessoRoot) ||
            !controllerOfflineDir.EndsWith("ControllerOffline", StringComparison.OrdinalIgnoreCase))
        {
            UIScope.WriteMessage($"[yellow]   [[AVISO]] Nao foi possivel determinar estrutura de diretorios a partir de: {MarkupHelper.Escape(serviceDir)}[/]");
            return false;
        }

        return UpdateConfig(targetDirectory);
    }

    public static bool UpdateConfig(string targetDirectory, string? idConexao = null, string? dbPath = null)
    {
        var serviceDir = ConfigHelperBase.NormalizeDirectoryPath(targetDirectory);
        string? configPath = null;
        foreach (var name in PossibleConfigFileNames)
        {
            var p = Path.Combine(serviceDir, name);
            if (File.Exists(p))
            {
                configPath = p;
                break;
            }
        }

        if (configPath == null)
        {
            UIScope.WriteMessage($"[gray]   [[INFO]] StandAloneIn/Im .config nao encontrado em: {MarkupHelper.Escape(targetDirectory)}[/]");
            return false;
        }

        try
        {
            var controllerOfflineDir = Path.GetDirectoryName(serviceDir);
            var newAcessoRoot = controllerOfflineDir != null ? Path.GetDirectoryName(controllerOfflineDir) : null;

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

            ConfigBackupService.BackupSingleFile(configPath);

            var doc = new XmlDocument();
            doc.Load(configPath);
            var appSettings = ConfigHelperBase.EnsureAppSettings(doc);

            ConfigHelperBase.SetKey(appSettings, "PathDataSource_NewAcessoConnectionRecord", resolvedDbPath);
            ConfigHelperBase.SetKey(appSettings, "ID_Conexao_NewAcessoConnectionRecord", resolvedId);
            ConfigHelperBase.SetKey(appSettings, "NomePastaLog", "Logs");
            ConfigHelperBase.SetKey(appSettings, "QuantidadeDiasCriacaoLog", "8");
            ConfigHelperBase.SetKey(appSettings, "Endereco_ServidorBiometrico", "localhost");

            doc.Save(configPath);
            UIScope.WriteMessage("   [green][[OK]] StandAloneIn/Im .config configurado com sucesso.[/]");
            return true;
        }
        catch (Exception ex)
        {
            UIScope.WriteMessage($"[red]   [[ERRO]] Falha ao atualizar StandAloneIn/Im .config: {MarkupHelper.Escape(ex.Message)}[/]");
            return false;
        }
    }
}