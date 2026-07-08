using InstaladorNewAcesso.Models;

namespace InstaladorNewAcesso.Configurations;

public class DirectorySetup
{
    private static readonly Dictionary<string, string[]> FoldersWithChildren = new()
    {
        ["Controller"] = ["ControleAcesso", "CoreWs", "Fabricantes", "Task"],
        ["ControllerOffline"] = ["Arquivos", "WinService_Ex", "WinService_In"],
        ["WebAppUI"] = ["Fabricantes"]
    };

    public IEnumerable<string> GetAllPaths(InstallationPaths paths)
    {
        foreach (var folder in paths.GetBaseFolders())
            yield return folder;

        foreach (var (parent, children) in FoldersWithChildren)
        {
            foreach (var child in children)
                yield return Path.Combine(paths.NewAcessoRoot, parent, child);
        }
    }
}