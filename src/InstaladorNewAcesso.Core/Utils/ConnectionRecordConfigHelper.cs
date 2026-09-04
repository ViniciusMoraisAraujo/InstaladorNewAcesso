using System.Xml;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class ConnectionRecordConfigHelper
{
    private const string ConfigFileName = "PrimeAcesso.ConnectionRecord.exe.config";

    /// <summary>
    /// Apos instalar o ConnectionRecord, verifica se o diretorio de destino
    /// contem o arquivo PrimeAcesso.ConnectionRecord.exe.config e ajusta o appSettings com a chave PathDataSource.
    /// </summary>
    public static bool UpdateConfigAfterInstall(string targetDirectory)
    {
        return UpdateConfig(targetDirectory);
    }

    /// <summary>
    /// Atualiza o arquivo PrimeAcesso.ConnectionRecord.exe.config com o caminho absoluto da base SQLite.
    /// </summary>
    public static bool UpdateConfig(string targetDirectory, string? dbPath = null)
    {
        var normalizedDir = ConfigHelperBase.NormalizeDirectoryPath(targetDirectory);
        var configPath = Path.Combine(normalizedDir, ConfigFileName);

        if (!File.Exists(configPath))
        {
            UIScope.WriteMessage($"[gray]   [[INFO]] Arquivo .config nao encontrado em: {MarkupHelper.Escape(configPath)}[/]");
            return false;
        }

        try
        {
            ConfigBackupService.BackupSingleFile(configPath);

            var doc = new XmlDocument();
            doc.Load(configPath);

            var appSettings = ConfigHelperBase.EnsureAppSettings(doc);
            var resolvedDbPath = dbPath ?? Path.Combine(normalizedDir, "DataBase", "NewAcessoConnection.s3db");
            ConfigHelperBase.SetKey(appSettings, "PathDataSource", resolvedDbPath);

            doc.Save(configPath);
            UIScope.WriteMessage($"   [green][[OK]] ConnectionRecord .config configurado.[/]");
            return true;
        }
        catch (Exception ex)
        {
            UIScope.WriteMessage($"[red]   [[ERRO]] Falha ao atualizar ConnectionRecord .config: {MarkupHelper.Escape(ex.Message)}[/]");
            return false;
        }
    }
}