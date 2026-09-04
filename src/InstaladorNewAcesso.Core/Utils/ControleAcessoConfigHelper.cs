using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class ControleAcessoConfigHelper
{
    private const string IniFileName = "PrimeAcesso.ControleAcesso.ini";
    private const string KeyPathDataSource = "PathDataSource_NewAcessoConnectionRecord";
    private const string TypoKeyPathDataSource = "PathDataSouce_NewAcessoConnectionRecord";
    private const string KeyIdConexao = "ID_Conexao_NewAcessoConnectionRecord";

    /// <summary>
    /// Apos instalar o ControleAcesso, atualiza o arquivo .ini com o caminho do banco e ID de conexao.
    /// </summary>
    public static bool UpdateIniAfterInstall(string targetDirectory)
    {
        return UpdateConfig(targetDirectory);
    }

    /// <summary>
    /// Atualiza o arquivo PrimeAcesso.ControleAcesso.ini com ID de conexao e caminho da base SQLite.
    /// </summary>
    public static bool UpdateConfig(string targetDirectory, string? idConexao = null, string? dbPath = null)
    {
        var normalizedDir = ConfigHelperBase.NormalizeDirectoryPath(targetDirectory);
        var iniPath = Path.Combine(normalizedDir, IniFileName);

        if (!File.Exists(iniPath))
        {
            UIScope.WriteMessage($"[gray]   [[INFO]] Arquivo .INI nao encontrado em: {MarkupHelper.Escape(iniPath)}[/]");
            return false;
        }

        try
        {
            var controllerDir = Path.GetDirectoryName(normalizedDir);
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

            ConfigBackupService.BackupSingleFile(iniPath);

            var lines = File.ReadAllLines(iniPath).ToList();

            // Atualiza chaves na secao [GERAL] e mantem compatibilidade com DLLs legadas
            var mod1 = IniHelperBase.SetIniKey(lines, KeyIdConexao, resolvedId, useQuotes: false, section: "GERAL");
            var mod2 = IniHelperBase.SetIniKey(lines, KeyPathDataSource, resolvedDbPath, useQuotes: true, section: "GERAL");
            var mod3 = IniHelperBase.SetIniKey(lines, TypoKeyPathDataSource, resolvedDbPath, useQuotes: true);

            if (mod1 || mod2 || mod3)
            {
                File.WriteAllLines(iniPath, lines);
                UIScope.WriteMessage($"   [green][[OK]] ControleAcesso .ini configurado com sucesso.[/]");
            }
            else
            {
                UIScope.WriteMessage($"   [gray][[INFO]] ControleAcesso .ini ja esta atualizado.[/]");
            }

            return true;
        }
        catch (Exception ex)
        {
            UIScope.WriteMessage($"[red]   [[ERRO]] Falha ao atualizar ControleAcesso .INI: {MarkupHelper.Escape(ex.Message)}[/]");
            return false;
        }
    }
}