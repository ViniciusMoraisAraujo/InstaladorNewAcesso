using System.Globalization;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

/// <summary>
/// Serviço para backup e restauração de arquivos de configuração
/// (.config, .ini, .xml) antes de reinstalar uma aplicação MSI.
/// </summary>
public static class ConfigBackupService
{
    private static readonly string[] ConfigPatterns = ["*.config", "*.ini", "*.xml"];

    /// <summary>
    /// Faz backup de todos os arquivos de configuração (.config, .ini, .xml)
    /// do diretório de destino para uma pasta temporária.
    /// </summary>
    /// <param name="targetDirectory">Diretório onde a aplicação está instalada.</param>
    /// <param name="msiName">Nome do MSI (usado para identificar o backup).</param>
    /// <returns>Caminho da pasta de backup, ou null se nenhum config foi encontrado.</returns>
    public static string? Backup(string targetDirectory, string msiName)
    {
        if (!Directory.Exists(targetDirectory))
            return null;

        var configFiles = new List<string>();
        foreach (var pattern in ConfigPatterns)
        {
            configFiles.AddRange(Directory.GetFiles(targetDirectory, pattern, SearchOption.TopDirectoryOnly));
        }

        if (configFiles.Count == 0)
        {
            UIScope.WriteMessage($"   [gray][[INFO]] Nenhum arquivo de configuração encontrado em: {MarkupHelper.Escape(targetDirectory)}[/]");
            return null;
        }

        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var sanitizedMsiName = SanitizeFileName(msiName);
        var backupDir = Path.Combine(Path.GetTempPath(), "InstaladorNewAcesso", "ConfigBackup", $"{sanitizedMsiName}_{timestamp}");

        Directory.CreateDirectory(backupDir);

        foreach (var file in configFiles)
        {
            var destPath = Path.Combine(backupDir, Path.GetFileName(file));
            File.Copy(file, destPath, overwrite: true);
        }

        UIScope.WriteMessage($"   [green][[OK]][/] Backup de {configFiles.Count} arquivo(s) de configuração: [cyan]{MarkupHelper.Escape(backupDir)}[/]");
        return backupDir;
    }

    /// <summary>
    /// Restaura os arquivos de configuração do backup de volta ao diretório de destino.
    /// </summary>
    public static void Restore(string? backupPath, string targetDirectory)
    {
        if (backupPath == null || !Directory.Exists(backupPath))
        {
            UIScope.WriteMessage("   [gray][[INFO]] Nenhum backup para restaurar.[/]");
            return;
        }

        if (!Directory.Exists(targetDirectory))
            Directory.CreateDirectory(targetDirectory);

        var restored = 0;
        foreach (var file in Directory.GetFiles(backupPath))
        {
            var destPath = Path.Combine(targetDirectory, Path.GetFileName(file));
            try
            {
                File.Copy(file, destPath, overwrite: true);
                restored++;
            }
            catch (Exception ex)
            {
                UIScope.WriteMessage($"   [red][[ERRO]] Falha ao restaurar {MarkupHelper.Escape(Path.GetFileName(file))}: {MarkupHelper.Escape(ex.Message)}[/]");
            }
        }

        UIScope.WriteMessage($"   [green][[OK]][/] Restaurado(s) {restored} arquivo(s) de configuração.");
    }

    /// <summary>
    /// Remove a pasta de backup.
    /// </summary>
    public static void Cleanup(string? backupPath)
    {
        if (backupPath == null || !Directory.Exists(backupPath))
            return;

        try
        {
            Directory.Delete(backupPath, true);
            UIScope.WriteMessage($"   [gray][[INFO]] Backup removido: {MarkupHelper.Escape(backupPath)}[/]");
        }
        catch
        {
            // Silencia erros de limpeza
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return sanitized.Length > 80 ? sanitized[..80] : sanitized;
    }
}
