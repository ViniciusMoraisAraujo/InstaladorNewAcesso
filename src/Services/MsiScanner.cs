using InstaladorNewAcesso.Interfaces;
using InstaladorNewAcesso.Models;

namespace InstaladorNewAcesso.Services;

public class MsiScanner
{
    private readonly InstallationPaths _paths;
    private readonly string _dbChoice;
    private readonly string _msiSourceRoot;
    private readonly IMsiClassifier _classifier;
    private readonly ISimpleLogger _logger;

    public MsiScanner(InstallationPaths paths, string dbChoice, string msiSourceRoot, IMsiClassifier classifier, ISimpleLogger logger)
    {
        _paths = paths;
        _dbChoice = dbChoice;
        _msiSourceRoot = msiSourceRoot;
        _classifier = classifier;
        _logger = logger;
    }

    public List<MsiInstallationModel> Scan()
    {
        var tasks = new List<MsiInstallationModel>();

        if (!Directory.Exists(_msiSourceRoot))
        {
            _logger.LogError($"Diretório de instaladores não encontrado: {_msiSourceRoot}");
            return tasks;
        }

        foreach (var subDir in Directory.GetDirectories(_msiSourceRoot))
        {
            var folderName = Path.GetFileName(subDir);

            if (folderName.Equals("SQLServer", StringComparison.OrdinalIgnoreCase) ||
                folderName.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
            {
                if (folderName.Equals(_dbChoice, StringComparison.OrdinalIgnoreCase))
                    tasks.AddRange(ScanSubDirectories(subDir, isDbSpecific: true));
                continue;
            }

            if (folderName.Equals("Fabricantes", StringComparison.OrdinalIgnoreCase))
                continue;

            tasks.AddRange(ScanSubDirectories(subDir, isDbSpecific: false));
        }

        foreach (var msiPath in Directory.GetFiles(_msiSourceRoot, "*.msi", SearchOption.TopDirectoryOnly))
        {
            var task = _classifier.ClassifyRootMsi(msiPath);
            if (task != null) tasks.Add(task);
        }

        return tasks;
    }

    public Dictionary<string, MsiInstallationModel> ScanManufacturers()
    {
        var result = new Dictionary<string, MsiInstallationModel>(StringComparer.OrdinalIgnoreCase);

        var manufacturersRoot = Path.Combine(_msiSourceRoot, "Fabricantes");
        if (!Directory.Exists(manufacturersRoot))
        {
            _logger.LogWarning($" Pasta Fabricantes não encontrada em: {manufacturersRoot}");
            return result;
        }

        var webAppUiManufacturers = Path.Combine(_paths.WebAppUI, "Fabricantes");

        var configDll = Directory
            .GetFiles(manufacturersRoot, "*.Configuracao.*.dll", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(manufacturersRoot, "*Configuracao*", SearchOption.TopDirectoryOnly)
                .Where(f => !f.EndsWith(".msi", StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault();

        bool configAdded = false;

        foreach (var msiPath in Directory.GetFiles(manufacturersRoot, "*.msi", SearchOption.TopDirectoryOnly)
                     .OrderBy(p => p))
        {
            var fileName = Path.GetFileNameWithoutExtension(msiPath);
            var manufacturerName = _classifier.ExtractManufacturerName(fileName);

            var model = new MsiInstallationModel
            {
                MsiPath = msiPath,
                TargetDirectory = _paths.Manufacturers,
                IsWebApp = false
            };

            if (!configAdded && configDll != null)
            {
                var dllDest = Path.Combine(webAppUiManufacturers, Path.GetFileName(configDll));
                model.FilesToCopy[configDll] = dllDest;
                configAdded = true;
            }

            var key = manufacturerName;
            int suffix = 2;
            while (result.ContainsKey(key))
                key = $"{manufacturerName} ({suffix++})";

            result[key] = model;
        }

        return result;
    }

    private List<MsiInstallationModel> ScanSubDirectories(string directory, bool isDbSpecific)
    {
        var tasks = new List<MsiInstallationModel>();

        foreach (var msiPath in Directory.GetFiles(directory, "*.msi", SearchOption.AllDirectories))
        {
            var classifiedTask = _classifier.ClassifyDirectoryMsi(msiPath, _msiSourceRoot, isDbSpecific);
            tasks.Add(classifiedTask);
        }

        return tasks;
    }
}

