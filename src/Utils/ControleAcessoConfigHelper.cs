using Spectre.Console;

namespace InstaladorNewAcesso.Utils;

public static class ControleAcessoConfigHelper
{
    private const string IniFileName = "PrimeAcesso.ControleAcesso.ini";
    private const string KeyName = "PathDataSouce_NewAcessoConnectionRecord";
    private const string ExpectedValue = @"ConnectionRecord\Database\NewAcessoConnection.s3db";

    /// <summary>
    /// Após instalar o ControleAcesso, verifica se o diretório de destino
    /// contém o arquivo PrimeAcesso.ControleAcesso.ini e, se sim,
    /// ajusta a chave PathDataSouce_NewAcessoConnectionRecord.
    /// </summary>
    public static bool UpdateIniAfterInstall(string targetDirectory)
    {
        var iniPath = Path.Combine(targetDirectory, IniFileName);

        if (!File.Exists(iniPath))
        {
            AnsiConsole.MarkupLine($"[gray]   [INFO] Arquivo .INI não encontrado em: {iniPath.EscapeMarkup()}[/]");
            return false;
        }

        try
        {
            var lines = File.ReadAllLines(iniPath).ToList();
            var modified = IniHelperBase.SetIniKey(lines, KeyName, ExpectedValue, useQuotes: true);

            if (modified)
            {
                File.WriteAllLines(iniPath, lines);
            }

            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]   [ERRO] Falha ao atualizar .INI: {ex.Message.EscapeMarkup()}[/]");
            return false;
        }
    }
}
