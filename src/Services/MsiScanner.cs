using InstaladorNewAcesso.Models;

namespace InstaladorNewAcesso.Services;

public class MsiScanner
{
    private readonly InstallationPaths _paths;
    private readonly string _dbChoice; 
    private readonly string _msiSourceRoot;

    private readonly Dictionary<string, Func<InstallationPaths, string>> _folderMapping;

    private readonly Dictionary<string, string> _rootMsiMapping;

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
            ["WebAppDS"] = p => p.WebAppDS,
            ["WebAppUI"] = p => p.WebAppUI,
            ["Win"] = p => p.Win,
            ["Fabricantes"] = p => Path.Combine(p.NewAcessoRoot, "Fabricantes"),
            ["OffLine"] = p => Path.Combine(p.NewAcessoRoot, "OffLine"),
            ["ConexBridge"] = p => Path.Combine(p.NewAcessoRoot, "ConexBridge")
        };

        _rootMsiMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CoreWS"] = @"Controller\CoreWs",
            ["ControleAcesso"] = @"Controller\ControleAcesso",
            ["Task"] = @"Controller\Task",
            ["ControllerOffline"] = @"ControllerOffline",
            ["WebAppUI"] = @"WebAppUI",
            ["WebAppDS"] = @"WebAppDS"
        };
    }

    public List<MsiInstallationModel> Scan()
    {
        var tasks = new List<MsiInstallationModel>();

        if (!Directory.Exists(_msiSourceRoot))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Diretório de instaladores não encontrado: {_msiSourceRoot}");
            Console.ResetColor();
            return tasks;
        }

        foreach (var subDir in Directory.GetDirectories(_msiSourceRoot))
        {
            var folderName = Path.GetFileName(subDir);

            if (folderName.Equals("SQLServer", StringComparison.OrdinalIgnoreCase) ||
                folderName.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
            {
                if (folderName.Equals(_dbChoice, StringComparison.OrdinalIgnoreCase))
                    tasks.AddRange(ProcessDirectory(subDir, isDbSpecific: true));
                continue;
            }

            tasks.AddRange(ProcessDirectory(subDir, isDbSpecific: false));
        }

        var rootMsis = Directory.GetFiles(_msiSourceRoot, "*.msi", SearchOption.TopDirectoryOnly);
        foreach (var msiPath in rootMsis)
        {
            var task = ProcessRootMsi(msiPath);
            if (task != null)
                tasks.Add(task);
        }

        return tasks;
    }

    private List<MsiInstallationModel> ProcessDirectory(string directory, bool isDbSpecific)
    {
        var tasks = new List<MsiInstallationModel>();
        var msiFiles = Directory.GetFiles(directory, "*.msi", SearchOption.AllDirectories);

        foreach (var msiPath in msiFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(msiPath);
            var relativePath = Path.GetRelativePath(_msiSourceRoot, msiPath);
            var subfolder = Path.GetDirectoryName(relativePath) ?? "";

            bool isWebApp = fileName.Contains("WebAppUI", StringComparison.OrdinalIgnoreCase) ||
                            fileName.Contains("WebAppDS", StringComparison.OrdinalIgnoreCase) ||
                            (isDbSpecific && (fileName.Contains("UI") || fileName.Contains("DS")));

            string targetDir;
            string? siteName = null;
            string? appPoolName = null;

            if (isWebApp)
            {
                if (fileName.Contains("WebAppUI", StringComparison.OrdinalIgnoreCase))
                {
                    targetDir = _paths.WebAppUI;
                    siteName = "WebAppUI";
                    appPoolName = "WebAppUI";
                }
                else if (fileName.Contains("WebAppDS", StringComparison.OrdinalIgnoreCase))
                {
                    targetDir = _paths.WebAppDS;
                    siteName = "WebAppDS";
                    appPoolName = "WebAppDS";
                }
                else
                {
                    targetDir = Path.Combine(_paths.NewAcessoRoot, subfolder);
                    siteName = subfolder;
                    appPoolName = subfolder;
                }
            }
            else
            {
                var firstFolder = subfolder.Split(Path.DirectorySeparatorChar).FirstOrDefault() ?? "";
                if (_folderMapping.TryGetValue(firstFolder, out var targetFunc))
                {
                    targetDir = targetFunc(_paths);
                }
                else
                {
                    targetDir = Path.Combine(_paths.NewAcessoRoot, firstFolder);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Aviso: Subpasta '{firstFolder}' não mapeada. Criando diretório: {targetDir}");
                    Console.ResetColor();
                    Directory.CreateDirectory(targetDir);
                }
            }

            tasks.Add(new MsiInstallationModel
            {
                MsiPath = msiPath,
                TargetDirectory = targetDir,
                IsWebApp = isWebApp,
                SiteName = siteName,
                AppPoolName = appPoolName
            });
        }

        return tasks;
    }

    private MsiInstallationModel? ProcessRootMsi(string msiPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(msiPath);
        string? targetRelativePath = null;

        // Tenta encontrar mapeamento por substring
        foreach (var kvp in _rootMsiMapping)
        {
            if (fileName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                targetRelativePath = kvp.Value;
                break;
            }
        }

        string targetDir;
        if (targetRelativePath != null)
        {
            targetDir = Path.Combine(_paths.NewAcessoRoot, targetRelativePath);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[RAIZ] MSI '{fileName}' mapeado para: {targetDir}");
            Console.ResetColor();
        }
        else
        {
            // Fallback: instalar em NewAcessoRoot\Outros
            targetDir = Path.Combine(_paths.NewAcessoRoot, "Outros");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[RAIZ] MSI '{fileName}' não reconhecido. Será instalado em: {targetDir}");
            Console.ResetColor();
            Directory.CreateDirectory(targetDir);
        }

        // Verifica se é web app (caso raro, mas pode acontecer)
        bool isWebApp = fileName.Contains("WebAppUI", StringComparison.OrdinalIgnoreCase) ||
                        fileName.Contains("WebAppDS", StringComparison.OrdinalIgnoreCase);

        string? siteName = null, appPoolName = null;
        if (isWebApp)
        {
            if (fileName.Contains("WebAppUI", StringComparison.OrdinalIgnoreCase))
            {
                siteName = "WebAppUI";
                appPoolName = "WebAppUI";
            }
            else if (fileName.Contains("WebAppDS", StringComparison.OrdinalIgnoreCase))
            {
                siteName = "WebAppDS";
                appPoolName = "WebAppDS";
            }
        }

        return new MsiInstallationModel
        {
            MsiPath = msiPath,
            TargetDirectory = targetDir,
            IsWebApp = isWebApp,
            SiteName = siteName,
            AppPoolName = appPoolName
        };
    }
}