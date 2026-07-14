using InstaladorNewAcesso.Abstractions.Models;

namespace InstaladorNewAcesso.Core.Services;

public class MsiScanner
{
    private readonly InstallationPaths _paths;
    private readonly string _dbChoice;
    private readonly string _msiSourceRoot;

    private readonly Dictionary<string, Func<InstallationPaths, string>> _folderMapping;
    private readonly Dictionary<string, Func<InstallationPaths, string>> _fileNameMapping;

    public MsiScanner(InstallationPaths paths, string dbChoice, string msiSourceRoot)
    {
        _paths = paths;
        _dbChoice = dbChoice;
        _msiSourceRoot = msiSourceRoot;

        _folderMapping = new Dictionary<string, Func<InstallationPaths, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["AutoAtendimento"] = p => p.AutoAtendimento,
            ["ConexBridge"] = p => p.ConexBridge,
            ["ConnectionRecord"] = p => p.ConnectionRecord,
            ["Controller"] = p => p.Controller,
            ["ControllerOffline"] = p => p.ControllerOffline,
            ["VisitAuthorization"] = p => p.VisitAuthorization,
            ["Win"] = p => p.Win,

            ["ControleAcesso"] = p => p.ControleAcesso,
            ["CoreWs"] = p => p.CoreWs,
            ["Fabricantes"] = p => p.Fabricantes,
            ["Task"] = p => p.Task,

            ["Arquivos"] = p => p.ControllerOfflineArquivos,
            ["WinService_Ex"] = p => p.ControllerOfflineWinServiceEx,
            ["WinService_In"] = p => p.ControllerOfflineWinServiceIn,

            ["OffLine"] = p => Path.Combine(p.NewAcessoRoot, "OffLine")
        };

        _fileNameMapping = new Dictionary<string, Func<InstallationPaths, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["StandAloneEx"] = p => p.ControllerOfflineWinServiceEx,
            ["StandAloneIn"] = p => p.ControllerOfflineWinServiceIn,
            ["AutoAtendimento"] = p => p.AutoAtendimento,
            ["ConexBridge"] = p => p.ConexBridge,
            ["ConnectionRecord"] = p => p.ConnectionRecord,
            ["ControllerOffline"] = p => p.ControllerOffline,
            ["VisitAuthorization"] = p => p.VisitAuthorization,
            ["ControleAcesso"] = p => p.ControleAcesso,
            ["CoreWs"] = p => p.CoreWs,
            ["Task"] = p => p.Task,
            ["Fabricantes"] = p => p.Fabricantes,
            ["Win"] = p => p.Win,
            ["Controller"] = p => p.Controller
        };
    }

    public List<MsiInstallationModel> Scan()
    {
        var tasks = new List<MsiInstallationModel>();

        if (!Directory.Exists(_msiSourceRoot))
            throw new DirectoryNotFoundException($"Diretório de instaladores não encontrado: {_msiSourceRoot}");

        foreach (var subDir in Directory.GetDirectories(_msiSourceRoot))
        {
            var folderName = Path.GetFileName(subDir);

            if (folderName.Equals("SQLServer", StringComparison.OrdinalIgnoreCase) ||
                folderName.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
            {
                if (folderName.Equals(_dbChoice, StringComparison.OrdinalIgnoreCase))
                    tasks.AddRange(ProcessDirectory(subDir));
                continue;
            }

            tasks.AddRange(ProcessDirectory(subDir));
        }

        var rootMsis = Directory.GetFiles(_msiSourceRoot, "*.msi", SearchOption.TopDirectoryOnly);
        foreach (var msiPath in rootMsis)
        {
            var fileName = Path.GetFileNameWithoutExtension(msiPath);

            if (fileName.Contains("UI", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("DS", StringComparison.OrdinalIgnoreCase))
                continue;

            var targetDir = ResolveTargetDirectory(msiPath, fileName);
            tasks.Add(new MsiInstallationModel
            {
                MsiPath = msiPath,
                TargetDirectory = targetDir
            });
        }

        return tasks;
    }

    private List<MsiInstallationModel> ProcessDirectory(string directory)
    {
        var tasks = new List<MsiInstallationModel>();
        var msiFiles = Directory.GetFiles(directory, "*.msi", SearchOption.AllDirectories);

        foreach (var msiPath in msiFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(msiPath);

            if (fileName.Contains("UI", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("DS", StringComparison.OrdinalIgnoreCase))
                continue;

            var targetDir = ResolveTargetDirectory(msiPath, fileName);

            tasks.Add(new MsiInstallationModel
            {
                MsiPath = msiPath,
                TargetDirectory = targetDir
            });
        }

        return tasks;
    }


    private string ResolveTargetDirectory(string msiPath, string fileName)
    {
        foreach (var kvp in _fileNameMapping)
        {
            if (fileName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value(_paths);
            }
        }

        var relativePath = Path.GetRelativePath(_msiSourceRoot, msiPath);
        var subfolder = Path.GetDirectoryName(relativePath) ?? "";
        var folders = subfolder.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        if (folders.Length > 0)
        {
            var lastFolder = folders.Last();
            if (_folderMapping.TryGetValue(lastFolder, out var targetFunc))
            {
                return targetFunc(_paths);
            }

            var firstFolder = folders.First();
            if (_folderMapping.TryGetValue(firstFolder, out targetFunc))
            {
                return targetFunc(_paths);
            }
        }

        return Path.Combine(_paths.NewAcessoRoot, fileName);
    }
}
