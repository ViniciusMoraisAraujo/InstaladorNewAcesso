using InstaladorNewAcesso.Interfaces;
using InstaladorNewAcesso.Models;

namespace InstaladorNewAcesso.Services;

public class MsiClassifier : IMsiClassifier
{
    private readonly InstallationPaths _paths;
    private readonly ISimpleLogger _logger;
    private readonly Dictionary<string, Func<InstallationPaths, string>> _folderMapping;
    private readonly Dictionary<string, string> _rootMsiMapping;

    public MsiClassifier(InstallationPaths paths, ISimpleLogger logger)
    {
        _paths = paths;
        _logger = logger;

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

    public MsiInstallationModel ClassifyDirectoryMsi(string msiPath, string msiSourceRoot, bool isDbSpecific)
    {
        var fileName = Path.GetFileNameWithoutExtension(msiPath);
        var relativePath = Path.GetRelativePath(msiSourceRoot, msiPath);
        var subfolder = Path.GetDirectoryName(relativePath) ?? "";

        bool isWinApp = fileName.Contains("Win", StringComparison.OrdinalIgnoreCase);

        bool isWebApp = !isWinApp && (
                        fileName.Contains("WebAppUI", StringComparison.OrdinalIgnoreCase) ||
                        fileName.Contains("WebAppDS", StringComparison.OrdinalIgnoreCase) ||
                        (isDbSpecific && (fileName.Contains("UI", StringComparison.OrdinalIgnoreCase) || 
                                          fileName.Contains("DS", StringComparison.OrdinalIgnoreCase))));

        string targetDir;
        string? siteName = null;
        string? appPoolName = null;

        if (isWebApp)
        {
            if (fileName.Contains("WebAppUI", StringComparison.OrdinalIgnoreCase) ||
                (isDbSpecific && fileName.Contains("UI", StringComparison.OrdinalIgnoreCase) && 
                                !fileName.Contains("DS", StringComparison.OrdinalIgnoreCase)))
            {
                targetDir = _paths.WebAppUI;
                siteName = "WebAppUI";
                appPoolName = "WebAppUI";
            }
            else
            {
                targetDir = _paths.WebAppDS;
                siteName = "WebAppDS";
                appPoolName = "WebAppDS";
            }
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
                    _logger.LogWarning($" Aviso: Subpasta '{firstFolder}' não mapeada → {targetDir}");
                    Directory.CreateDirectory(targetDir);
                }
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

    public MsiInstallationModel? ClassifyRootMsi(string msiPath)
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
            _logger.LogSuccess($"\n [RAIZ] '{fileName}' → {targetDir}");
        }
        else
        {
            targetDir = Path.Combine(_paths.NewAcessoRoot, "Outros");
            _logger.LogWarning($"\n [RAIZ] '{fileName}' não reconhecido → {targetDir}");
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
            MsiPath = msiPath,
            TargetDirectory = targetDir,
            IsWebApp = isWebApp,
            SiteName = siteName,
            AppPoolName = appPoolName
        };
    }

    public string ExtractManufacturerName(string fileName)
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
}