using System.Xml;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class TaskConfigHelper
{
    private const string ConfigFileName = "PrimeAcesso.Task.exe.config";

    /// <summary>
    /// Apos instalar o Task, verifica se o diretorio de destino
    /// contem o arquivo PrimeAcesso.Task.exe.config e, se sim,
    /// solicita os valores do usuario e atualiza todas as chaves.
    /// </summary>
    public static bool UpdateConfigAfterInstall(string targetDirectory)
    {
        var configPath = Path.Combine(targetDirectory, ConfigFileName);

        if (!File.Exists(configPath))
        {
            UIScope.WriteMessage($"[gray]   [[INFO]] Task .config nao encontrado: {MarkupHelper.Escape(configPath)}[/]");
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
                UIScope.WriteMessage($"[yellow]   [[AVISO]] Nao foi possivel determinar estrutura de diretorios a partir de: {MarkupHelper.Escape(taskDir)}[/]");
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

            // Chaves que exigem input do usuario
            UIScope.WriteMessage("\n   [bold yellow]Configuracao do Task:[/]");

            var idConexao = UIScope.AskInput("   [bold yellow]ID_Conexao_NewAcessoConnectionRecord[/] (padrao: [gray]1[/]):", "1");
            ConfigHelperBase.SetKey(appSettings, "ID_Conexao_NewAcessoConnectionRecord", idConexao);

            var fabricante = UIScope.AskInput("   [bold yellow]FabricanteEquipamentoFacial[/] (ex: [gray]Evo|Avicam|ControlId[/]):", "Evo");
            ConfigHelperBase.SetKey(appSettings, "FabricanteEquipamentoFacial", fabricante);

            var horaExclusao = UIScope.AskInput("   [bold yellow]HoraExecucaoExclusaoFacial[/] (padrao: [gray]17:00[/]):", "17:00");
            ConfigHelperBase.SetKey(appSettings, "HoraExecucaoExclusaoFacial", horaExclusao);

            var logDetalhado = UIScope.Confirm("   [bold yellow]LogDetalhado[/] (deseja log detalhado?)", false);
            ConfigHelperBase.SetKey(appSettings, "LogDetalhado", logDetalhado ? "True" : "False");

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
