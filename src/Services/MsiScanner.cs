using InstaladorNewAcesso.Models;

namespace InstaladorNewAcesso.Services;

public class MsiScanner
{
    private readonly InstallationPaths _paths;
    private readonly string _dbChoice;
    private readonly string _msiSourceRoot;

    private readonly Dictionary<string, Func<InstallationPaths, string>> _folderMapping;

    public MsiScanner(InstallationPaths paths, string dbChoice, string msiSourceRoot)
    {
        _paths = paths;
        _dbChoice = dbChoice;
        _msiSourceRoot = msiSourceRoot;

        _folderMapping = new Dictionary<string, Func<InstallationPaths, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["AutoAtendimento"] = p => p.AutoAtendimento,
            ["ConnectionRecord"] = p => p.ConnectionRecord,
            ["Controller"] = p => p.Controller,
            ["ControllerOffline"] = p => p.ControllerOffline,
            ["VisitAuthorization"] = p => p.VisitAuthorization,
            ["Win"] = p => p.Win,
            ["Fabricantes"] = p => Path.Combine(p.NewAcessoRoot, "Fabricantes"),
            ["OffLine"] = p => Path.Combine(p.NewAcessoRoot, "OffLine"),
            ["ConexBridge"] = p => Path.Combine(p.NewAcessoRoot, "ConexBridge")
        };
    }

    public List<MsiInstallationModel> Scan()
    {
        var tasks = new List<MsiInstallationModel>();

        if (!Directory.Exists(_msiSourceRoot))
        {
            throw new DirectoryNotFoundException($"Diretório de instaladores não encontrado: {_msiSourceRoot}");
        }

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

            var relativePath = Path.GetRelativePath(_msiSourceRoot, msiPath);
            var subfolder = Path.GetDirectoryName(relativePath) ?? "";
            var firstFolder = subfolder.Split(Path.DirectorySeparatorChar).FirstOrDefault() ?? "";
            
            string targetDir;
            if (_folderMapping.TryGetValue(firstFolder, out var targetFunc))
            {
                targetDir = targetFunc(_paths);
            }
            else
            {
                targetDir = Path.Combine(_paths.NewAcessoRoot, firstFolder);
            }

            tasks.Add(new MsiInstallationModel
            {
                MsiPath = msiPath,
                TargetDirectory = targetDir
            });
        }

        return tasks;
    }
}