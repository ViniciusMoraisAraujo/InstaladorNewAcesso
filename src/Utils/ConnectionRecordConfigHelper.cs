using System.Xml;
using InstaladorNewAcesso.Models;
using Spectre.Console;

namespace InstaladorNewAcesso.Utils;

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
            AnsiConsole.MarkupLine($"[gray]   [INFO] Arquivo .config não encontrado em: {configPath.EscapeMarkup()}[/]");
            return false;
        }

        try
        {
            var doc = new XmlDocument();
            doc.Load(configPath);

            var appSettings = ConfigHelperBase.EnsureAppSettings(doc);
            var relativePath = @"ConnectionRecord\Database\NewAcessoConnection.s3db";
            ConfigHelperBase.SetKey(appSettings, "PathDataSource", relativePath);

            doc.Save(configPath);
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]   [ERRO] Falha ao atualizar config: {ex.Message.EscapeMarkup()}[/]");
            return false;
        }
    }


}
