using System.Xml;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class TaskConfigHelper
{
    private static readonly string[] PossibleConfigFileNames =
    [
        "PrimeAcesso.Controller.Task.exe.config",
        "PrimeAcesso.Task.exe.config"
    ];

    public static bool UpdateConfigAfterInstall(string targetDirectory)
    {
        var taskDir = ConfigHelperBase.NormalizeDirectoryPath(targetDirectory);
        var controllerDir = Path.GetDirectoryName(taskDir);
        var newAcessoRoot = controllerDir != null ? Path.GetDirectoryName(controllerDir) : null;

        if (string.IsNullOrEmpty(controllerDir) || string.IsNullOrEmpty(newAcessoRoot) ||
            (!controllerDir.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) && !controllerDir.EndsWith("Controlador", StringComparison.OrdinalIgnoreCase)))
        {
            UIScope.WriteMessage($"[yellow]   [[AVISO]] Nao foi possivel determinar estrutura de diretorios a partir de: {MarkupHelper.Escape(taskDir)}[/]");
            return false;
        }

        return UpdateConfig(targetDirectory);
    }

    public static bool UpdateConfig(string targetDirectory, string? idConexao = null, string? dbPath = null, string? fabricante = null, string? horaExclusao = null)
    {
        var taskDir = ConfigHelperBase.NormalizeDirectoryPath(targetDirectory);
        string? configPath = null;
        foreach (var name in PossibleConfigFileNames)
        {
            var p = Path.Combine(taskDir, name);
            if (File.Exists(p))
            {
                configPath = p;
                break;
            }
        }

        if (configPath == null)
        {
            UIScope.WriteMessage($"[gray]   [[INFO]] Task .config nao encontrado em: {MarkupHelper.Escape(targetDirectory)}[/]");
            return false;
        }

        try
        {
            var controllerDir = Path.GetDirectoryName(taskDir);
            var newAcessoRoot = controllerDir != null ? Path.GetDirectoryName(controllerDir) : null;

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

            ConfigHelperBase.SetKey(appSettings, "Endereco_ServidorBiometrico", "localhost");
            ConfigHelperBase.SetKey(appSettings, "PathDataSource_NewAcessoConnectionRecord", resolvedDbPath);
            ConfigHelperBase.SetKey(appSettings, "ID_Conexao_NewAcessoConnectionRecord", resolvedId);
            ConfigHelperBase.SetKey(appSettings, "ExecutarExclusaoFacial", "True");
            ConfigHelperBase.SetKey(appSettings, "FabricanteEquipamentoFacial", fabricante ?? "TopData");
            ConfigHelperBase.SetKey(appSettings, "HoraExecucaoExclusaoFacial", horaExclusao ?? "17:00");

            doc.Save(configPath);
            UIScope.WriteMessage("   [green][[OK]] Task .config configurado com sucesso.[/]");
            return true;
        }
        catch (Exception ex)
        {
            UIScope.WriteMessage($"[red]   [[ERRO]] Falha ao atualizar Task .config: {MarkupHelper.Escape(ex.Message)}[/]");
            return false;
        }
    }
}