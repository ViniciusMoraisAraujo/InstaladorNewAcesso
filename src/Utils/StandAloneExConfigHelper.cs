using System.Xml;
using Spectre.Console;

namespace InstaladorNewAcesso.Utils;

public static class StandAloneExConfigHelper
{
    private const string ConfigFileName = "PrimeAcesso.Controller.StandAloneEx.exe.config";

    /// <summary>
    /// Após instalar o WinService_Ex, verifica se o diretório de destino
    /// contém o PrimeAcesso.Controller.StandAloneEx.exe.config e, se sim,
    /// solicita o ID_Conexao e atualiza todas as chaves.
    /// </summary>
    public static bool UpdateConfigAfterInstall(string targetDirectory)
    {
        var configPath = Path.Combine(targetDirectory, ConfigFileName);

        if (!File.Exists(configPath))
        {
            AnsiConsole.MarkupLine($"[gray]   [INFO] StandAloneEx .config não encontrado: {configPath.EscapeMarkup()}[/]");
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
                AnsiConsole.MarkupLine($"[yellow]   [AVISO] Não foi possível determinar estrutura de diretórios a partir de: {serviceDir.EscapeMarkup()}[/]");
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

            // Chave que exige input do usuário
            AnsiConsole.MarkupLine($"\n   [bold yellow]Configuração do StandAloneEx:[/]");
            var idConexao = AnsiConsole.Ask<string>("   [bold yellow]ID_Conexao_NewAcessoConnectionRecord[/] (padrão: [gray]1[/]):");
            if (string.IsNullOrWhiteSpace(idConexao)) idConexao = "1";
            ConfigHelperBase.SetKey(appSettings, "ID_Conexao_NewAcessoConnectionRecord", idConexao);

            doc.Save(configPath);
            AnsiConsole.MarkupLine($"   [green][OK] StandAloneEx .config configurado com sucesso.[/]");
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]   [ERRO] Falha ao atualizar StandAloneEx .config: {ex.Message.EscapeMarkup()}[/]");
            return false;
        }
    }


}
