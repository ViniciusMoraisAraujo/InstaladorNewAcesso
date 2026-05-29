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
            ["AutoAtendimento"]    = p => p.AutoAtendimento,
            ["ConnectionRecord"]   = p => p.ConnectionRecord,
            ["Controller"]         = p => p.Controller,
            ["ControllerOffline"]  = p => p.ControllerOffline,
            ["VisitAuthorization"] = p => p.VisitAuthorization,
            ["WebAppDS"]           = p => p.WebAppDS,
            ["WebAppUI"]           = p => p.WebAppUI,
            ["Win"]                = p => p.Win,
            ["OffLine"]            = p => Path.Combine(p.NewAcessoRoot, "OffLine"),
            ["ConexBridge"]        = p => Path.Combine(p.NewAcessoRoot, "ConexBridge")
        };

        _rootMsiMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CoreWS"]            = @"Controller\CoreWs",
            ["ControleAcesso"]    = @"Controller\ControleAcesso",
            ["Task"]              = @"Controller\Task",
            ["ControllerOffline"] = @"ControllerOffline",
            ["WebAppUI"]          = @"WebAppUI",
            ["WebAppDS"]          = @"WebAppDS"
        };
    }

    private static readonly HashSet<string> _skipFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "SQLServer", "Oracle", "Fabricantes"
    };


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

            if (folderName.Equals("Fabricantes", StringComparison.OrdinalIgnoreCase))
                continue;

            tasks.AddRange(ProcessDirectory(subDir, isDbSpecific: false));
        }

        foreach (var msiPath in Directory.GetFiles(_msiSourceRoot, "*.msi", SearchOption.TopDirectoryOnly))
        {
            var task = ProcessRootMsi(msiPath);
            if (task != null) tasks.Add(task);
        }

        return tasks;
    }

  
    public Dictionary<string, MsiInstallationModel> ScanFabricantes()
    {
        var result = new Dictionary<string, MsiInstallationModel>(StringComparer.OrdinalIgnoreCase);

        var fabricantesRoot = Path.Combine(_msiSourceRoot, "Fabricantes");
        if (!Directory.Exists(fabricantesRoot))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($" Pasta Fabricantes não encontrada em: {fabricantesRoot}");
            Console.ResetColor();
            return result;
        }

        var webAppUIFabricantes = Path.Combine(_paths.WebAppUI, "Fabricantes");

        var configDll = Directory
            .GetFiles(fabricantesRoot, "*.Configuracao.*.dll", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(fabricantesRoot, "*Configuracao*", SearchOption.TopDirectoryOnly)
                .Where(f => !f.EndsWith(".msi", StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault();

        bool configAdded = false;

        foreach (var msiPath in Directory.GetFiles(fabricantesRoot, "*.msi", SearchOption.TopDirectoryOnly)
                     .OrderBy(p => p))
        {
            var fileName = Path.GetFileNameWithoutExtension(msiPath);
            
            var fabricanteName = ExtrairNomeFabricante(fileName);

            var model = new MsiInstallationModel
            {
                MsiPath      = msiPath,
                TargetDirectory = _paths.Fabricantes,
                IsWebApp     = false
            };

            if (!configAdded && configDll != null)
            {
                var dllDest = Path.Combine(webAppUIFabricantes, Path.GetFileName(configDll));
                model.FilesToCopy[configDll] = dllDest;
                configAdded = true;
            }
            
            var key = fabricanteName;
            int suffix = 2;
            while (result.ContainsKey(key))
                key = $"{fabricanteName} ({suffix++})";

            result[key] = model;
        }

        return result;
    }

    
    private static string ExtrairNomeFabricante(string fileName)
    {
        var prefixes = new[] { "NewAcesso.Fabricante.", "PrimeAcesso.Fabricante." };
        foreach (var prefix in prefixes)
        {
            var idx = fileName.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var after = fileName[(idx + prefix.Length)..];
                var setupIdx = after.IndexOf(".Setup", StringComparison.OrdinalIgnoreCase);
                var name = setupIdx >= 0 ? after[..setupIdx] : after;
                return name.Replace("_", " ").Trim();
            }
        }

        return fileName;
    }

    private List<MsiInstallationModel> ProcessDirectory(string directory, bool isDbSpecific)
    {
        var tasks = new List<MsiInstallationModel>();

        foreach (var msiPath in Directory.GetFiles(directory, "*.msi", SearchOption.AllDirectories))
        {
            var fileName    = Path.GetFileNameWithoutExtension(msiPath);
            var relativePath = Path.GetRelativePath(_msiSourceRoot, msiPath);
            var subfolder   = Path.GetDirectoryName(relativePath) ?? "";

            bool isWebUI = fileName.Contains("WebAppUI", StringComparison.OrdinalIgnoreCase) ||
                           fileName.Contains("Mvc",      StringComparison.OrdinalIgnoreCase) ||
                           (isDbSpecific && fileName.Contains("WebApp", StringComparison.OrdinalIgnoreCase)
                                        && fileName.Contains("UI",      StringComparison.OrdinalIgnoreCase));

            bool isWebDS = !isWebUI && (
                           fileName.Contains("WebAppDS", StringComparison.OrdinalIgnoreCase) ||
                           (isDbSpecific && fileName.Contains("WebApp", StringComparison.OrdinalIgnoreCase)
                                        && fileName.Contains("DS",      StringComparison.OrdinalIgnoreCase)));

            bool isWebApp = isWebUI || isWebDS;

            string targetDir;
            string? siteName    = null;
            string? appPoolName = null;

            if (isWebUI)
            {
                targetDir   = _paths.WebAppUI;
                siteName    = "WebAppUI";
                appPoolName = "WebAppUI";
            }
            else if (isWebDS)
            {
                targetDir   = _paths.WebAppDS;
                siteName    = "WebAppDS";
                appPoolName = "WebAppDS";
            }
            else
            {
                if (isDbSpecific)
                {
                    targetDir = _paths.Win;
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
                        Console.WriteLine($" Aviso: Subpasta '{firstFolder}' não mapeada → {targetDir}");
                        Console.ResetColor();
                        Directory.CreateDirectory(targetDir);
                    }
                }
            }

            tasks.Add(new MsiInstallationModel
            {
                MsiPath        = msiPath,
                TargetDirectory = targetDir,
                IsWebApp       = isWebApp,
                SiteName       = siteName,
                AppPoolName    = appPoolName
            });
        }

        return tasks;
    }

    private MsiInstallationModel? ProcessRootMsi(string msiPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(msiPath);
        string? targetRelativePath = null;

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
            Console.WriteLine($"\n [RAIZ] '{fileName}' → {targetDir}");
            Console.ResetColor();
        }
        else
        {
            targetDir = Path.Combine(_paths.NewAcessoRoot, "Outros");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n [RAIZ] '{fileName}' não reconhecido → {targetDir}");
            Console.ResetColor();
            Directory.CreateDirectory(targetDir);
        }

        bool isWebApp = fileName.Contains("WebAppUI", StringComparison.OrdinalIgnoreCase) ||
                        fileName.Contains("WebAppDS", StringComparison.OrdinalIgnoreCase);

        string? siteName = null, appPoolName = null;
        if (isWebApp)
        {
            if (fileName.Contains("WebAppUI", StringComparison.OrdinalIgnoreCase))
            { siteName = "WebAppUI"; appPoolName = "WebAppUI"; }
            else
            { siteName = "WebAppDS"; appPoolName = "WebAppDS"; }
        }

        return new MsiInstallationModel
        {
            MsiPath         = msiPath,
            TargetDirectory = targetDir,
            IsWebApp        = isWebApp,
            SiteName        = siteName,
            AppPoolName     = appPoolName
        };
    }
}