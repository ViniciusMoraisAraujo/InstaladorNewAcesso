
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class ControleAcessoConfigHelper
{
    private const string IniFileName = "PrimeAcesso.ControleAcesso.ini";
    private const string KeyName = "PathDataSouce_NewAcessoConnectionRecord";

    /// <summary>
    /// Após instalar o ControleAcesso, verifica se o diretório de destino
    /// contém o arquivo PrimeAcesso.ControleAcesso.ini e, se sim,
    /// ajusta a chave PathDataSouce_NewAcessoConnectionRecord
    /// com o caminho absoluto do banco de dados.
    /// </summary>
    public static bool UpdateIniAfterInstall(string targetDirectory)
    {
        var iniPath = Path.Combine(targetDirectory, IniFileName);

        if (!File.Exists(iniPath))
        {
            UIScope.WriteMessage($"[gray]   [[INFO]] Arquivo .INI não encontrado em: {MarkupHelper.Escape(iniPath)}[/]");
            return false;
        }

        try
        {
            // targetDirectory = {BasePath}\NewAcesso\Controller\ControleAcesso
            var controllerDir = Path.GetDirectoryName(targetDirectory); // {BasePath}\NewAcesso\Controller
            var newAcessoRoot = Path.GetDirectoryName(controllerDir);   // {BasePath}\NewAcesso

            if (string.IsNullOrEmpty(controllerDir) || string.IsNullOrEmpty(newAcessoRoot))
            {
                UIScope.WriteMessage($"[yellow]   [[AVISO]] Não foi possível determinar estrutura de diretórios a partir de: {MarkupHelper.Escape(targetDirectory)}[/]");
                return false;
            }

            var dbPath = Path.Combine(newAcessoRoot, "ConnectionRecord", "DataBase", "NewAcessoConnection.s3db");

            var lines = File.ReadAllLines(iniPath).ToList();
            var modified = IniHelperBase.SetIniKey(lines, KeyName, dbPath, useQuotes: true);

            if (modified)
            {
                File.WriteAllLines(iniPath, lines);
            }

            return true;
        }
        catch (Exception ex)
        {
            UIScope.WriteMessage($"[red]   [[ERRO]] Falha ao atualizar .INI: {MarkupHelper.Escape(ex.Message)}[/]");
            return false;
        }
    }
}
