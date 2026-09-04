using InstaladorNewAcesso.Abstractions.Models;

namespace InstaladorNewAcesso.Core.Configurations;

/// <summary>
/// Mapeamento e criação estruturada da árvore de diretórios do NewAcesso.
/// </summary>
public static class DirectorySetup
{
    private static readonly Dictionary<string, string[]> s_foldersWithChildren = new()
    {
        ["Controller"] = ["ControleAcesso", "CoreWs", "Fabricantes", "Task"],
        ["ControllerOffline"] = ["Arquivos", "WinService_Ex", "WinService_In"],
        ["WebAppUI"] = ["Fabricantes"]
    };

    /// <summary>
    /// Retorna todos os caminhos de diretórios (base e aninhados) da solução NewAcesso.
    /// </summary>
    public static IEnumerable<string> GetAllPaths(InstallationPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        yield return paths.InstallationPath;

        foreach (var folder in paths.GetBaseFolders())
            yield return folder;

        foreach (var (parent, children) in s_foldersWithChildren)
        {
            foreach (var child in children)
                yield return Path.Combine(paths.NewAcessoRoot, parent, child);
        }
    }

    /// <summary>
    /// Cria todos os diretórios da estrutura NewAcesso de forma segura e idempotente.
    /// </summary>
    public static void CreateDirectories(InstallationPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        foreach (var path in GetAllPaths(paths))
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
