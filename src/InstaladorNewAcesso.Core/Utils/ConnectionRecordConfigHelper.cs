using System.Xml;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class ConnectionRecordConfigHelper
{
    private const string ConfigFileName = "PrimeAcesso.ConnectionRecord.exe.config";

    /// <summary>
    /// Após instalar o ConnectionRecord, verifica se o diretório de destino
    /// contém o arquivo PrimeAcesso.ConnectionRecord.exe.config e, se sim,
    /// ajusta o appSettings com a chave PathDataSource.
    /// </summary>
    public static bool UpdateConfigAfterInstall(string targetDirectory)
    {
        var configPath = Path.Combine(targetDirectory, ConfigFileName);

        if (!File.Exists(configPath))
        {
            UIScope.WriteMessage($"[gray]   [[INFO]] Arquivo .config não encontrado em: {MarkupHelper.Escape(configPath)}[/]");
            return false;
        }

        try
        {
            var doc = new XmlDocument();
            doc.Load(configPath);

            var appSettings = ConfigHelperBase.EnsureAppSettings(doc);
            var dbPath = Path.Combine(targetDirectory, "DataBase", "NewAcessoConnection.s3db");
            ConfigHelperBase.SetKey(appSettings, "PathDataSource", dbPath);

            doc.Save(configPath);
            return true;
        }
        catch (Exception ex)
        {
            UIScope.WriteMessage($"[red]   [[ERRO]] Falha ao atualizar config: {MarkupHelper.Escape(ex.Message)}[/]");
            return false;
        }
    }


}
