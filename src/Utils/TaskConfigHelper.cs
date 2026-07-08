using System.Xml;
using Spectre.Console;

namespace InstaladorNewAcesso.Utils;

public static class TaskConfigHelper
{
    private const string ConfigFileName = "PrimeAcesso.Task.exe.config";

    /// <summary>
    /// Após instalar o Task, verifica se o diretório de destino
    /// contém o arquivo PrimeAcesso.Task.exe.config e, se sim,
    /// solicita os valores do usuário e atualiza todas as chaves.
    /// </summary>
    public static bool UpdateConfigAfterInstall(string targetDirectory)
    {
        var configPath = Path.Combine(targetDirectory, ConfigFileName);

        if (!File.Exists(configPath))
        {
            AnsiConsole.MarkupLine($"[gray]   [INFO] Task .config não encontrado: {configPath.EscapeMarkup()}[/]");
            return false;
        }

        try
        {
            // Calcula o path absoluto para ConnectionRecord\DataBase\NewAcessoConnection.s3db
            // targetDirectory = {BasePath}\NewAcesso\Controller\Task
            var taskDir = targetDirectory;
            var controllerDir = Path.GetDirectoryName(taskDir);    // {BasePath}\NewAcesso\Controller
            var newAcessoRoot = Path.GetDirectoryName(controllerDir); // {BasePath}\NewAcesso

            if (string.IsNullOrEmpty(controllerDir) || string.IsNullOrEmpty(newAcessoRoot))
            {
                AnsiConsole.MarkupLine($"[yellow]   [AVISO] Não foi possível determinar estrutura de diretórios a partir de: {taskDir.EscapeMarkup()}[/]");
                return false;
            }

            var connectionRecordDbPath = Path.Combine(newAcessoRoot, "ConnectionRecord", "DataBase", "NewAcessoConnection.s3db");

            // Carrega o XML
            var doc = new XmlDocument();
            doc.Load(configPath);
            var appSettings = ConfigHelperBase.EnsureAppSettings(doc);

            // Chaves fixas (sem prompt)
            ConfigHelperBase.SetKey(appSettings, "Endereco_ServidorBiometrico", "localhost");
            ConfigHelperBase.SetKey(appSettings, "PathDataSource_NewAcessoConnectionRecord", connectionRecordDbPath);
            ConfigHelperBase.SetKey(appSettings, "ExecutarExclusaoFacial", "True");

            // Chaves que exigem input do usuário
            AnsiConsole.MarkupLine($"\n   [bold yellow]Configuração do Task:[/]");

            var idConexao = AnsiConsole.Ask<string>("   [bold yellow]ID_Conexao_NewAcessoConnectionRecord[/] (padrão: [gray]1[/]):");
            if (string.IsNullOrWhiteSpace(idConexao)) idConexao = "1";
            ConfigHelperBase.SetKey(appSettings, "ID_Conexao_NewAcessoConnectionRecord", idConexao);

            var fabricante = AnsiConsole.Ask<string>("   [bold yellow]FabricanteEquipamentoFacial[/] (ex: [gray]Evo|Avicam|ControlId[/]):");
            if (string.IsNullOrWhiteSpace(fabricante)) fabricante = "Evo";
            ConfigHelperBase.SetKey(appSettings, "FabricanteEquipamentoFacial", fabricante);

            var horaExclusao = AnsiConsole.Ask<string>("   [bold yellow]HoraExecucaoExclusaoFacial[/] (padrão: [gray]17:00[/]):");
            if (string.IsNullOrWhiteSpace(horaExclusao)) horaExclusao = "17:00";
            ConfigHelperBase.SetKey(appSettings, "HoraExecucaoExclusaoFacial", horaExclusao);

            var logDetalhado = AnsiConsole.Confirm("   [bold yellow]LogDetalhado[/] (deseja log detalhado?)", false);
            ConfigHelperBase.SetKey(appSettings, "LogDetalhado", logDetalhado ? "True" : "False");

            doc.Save(configPath);
            AnsiConsole.MarkupLine($"   [green][OK] Task .config configurado com sucesso.[/]");
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]   [ERRO] Falha ao atualizar Task .config: {ex.Message.EscapeMarkup()}[/]");
            return false;
        }
    }


}
