using InstaladorNewAcesso.Models;

namespace InstaladorNewAcesso.Utils;

public class WebAppScanner
{
    private readonly InstallationPaths _paths;
    private readonly string _dbChoice;
    private readonly string _msiSourceRoot;

    public WebAppScanner(InstallationPaths paths, string dbChoice, string msiSourceRoot)
    {
        _paths = paths;
        _dbChoice = dbChoice;
        _msiSourceRoot = msiSourceRoot;
    }

    public List<WebAppModel> Scan()
    {
        var webApps = new List<WebAppModel>();
        // Rastreia MSIs já adicionados para evitar duplicatas quando um arquivo
        // aparece tanto na raiz quanto em uma subpasta escaneada recursivamente.
        var addedMsiPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(_msiSourceRoot))
        {
            Console.WriteLine($"   [DEBUG] Diretório não encontrado: {_msiSourceRoot}");
            return webApps;
        }

        Console.WriteLine($"   [DEBUG] Escaneando: {_msiSourceRoot}");

        // Escaneia subpastas de primeiro nível
        foreach (var subDir in Directory.GetDirectories(_msiSourceRoot))
        {
            var folderName = Path.GetFileName(subDir);
            Console.WriteLine($"   [DEBUG] Verificando pasta: {folderName}");

            bool isDbFolder =
                folderName.Equals("SQLServer", StringComparison.OrdinalIgnoreCase) ||
                folderName.Equals("Oracle", StringComparison.OrdinalIgnoreCase);

            if (isDbFolder)
            {
                if (folderName.Equals(_dbChoice, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"   [DEBUG] Pasta do banco '{folderName}' corresponde. Escaneando...");
                    AddRange(webApps, ScanDirectory(subDir, SearchOption.AllDirectories), addedMsiPaths);
                }
                else
                {
                    Console.WriteLine($"   [DEBUG] Pasta do banco '{folderName}' ignorada (não é '{_dbChoice}')");
                }
                continue;
            }

            AddRange(webApps, ScanDirectory(subDir, SearchOption.AllDirectories), addedMsiPaths);
        }

        // Escaneia apenas a raiz (TopDirectoryOnly para não reprocessar subpastas já visitadas)
        Console.WriteLine($"   [DEBUG] Verificando raiz...");
        AddRange(webApps, ScanDirectory(_msiSourceRoot, SearchOption.TopDirectoryOnly), addedMsiPaths);

        return webApps;
    }

    private void AddRange(
        List<WebAppModel> target,
        List<WebAppModel> source,
        HashSet<string> addedPaths)
    {
        foreach (var model in source)
        {
            if (addedPaths.Add(model.MsiPath))
                target.Add(model);
            else
                Console.WriteLine($"   [DEBUG] MSI duplicado ignorado: {model.MsiPath}");
        }
    }

    private List<WebAppModel> ScanDirectory(string directory, SearchOption searchOption)
    {
        var webApps = new List<WebAppModel>();

        var msiFiles = Directory.GetFiles(directory, "*.msi", searchOption);
        Console.WriteLine($"   [DEBUG] Encontrados {msiFiles.Length} arquivos .msi em: {directory}");

        foreach (var msiPath in msiFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(msiPath);
            Console.WriteLine($"   [DEBUG] Verificando: {fileName}");

            // Identificar WebAppDS (verificado antes de UI para evitar falso positivo
            // caso um nome contenha ambas as siglas)
            if (fileName.Contains("DS", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"   [DEBUG] -> Detectado como WebAppDS!");
                webApps.Add(new WebAppModel
                {
                    MsiPath = msiPath,
                    SiteName = "WebAppDS",
                    AppPoolName = "WebAppDS",
                    ForcedInstallPath = @"C:\inetpub\wwwroot\WebAppDS",
                    TargetDirectory = _paths.WebAppDS,
                    Port = 8080
                });
            }
            // Identificar WebAppUI
            else if (fileName.Contains("UI", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"   [DEBUG] -> Detectado como WebAppUI!");
                webApps.Add(new WebAppModel
                {
                    MsiPath = msiPath,
                    SiteName = "WebAppUI",
                    AppPoolName = "WebAppUI",
                    ForcedInstallPath = @"C:\inetpub\wwwroot\WebAppUI",
                    TargetDirectory = _paths.WebAppUI,
                    Port = 8081
                });
            }
            else
            {
                Console.WriteLine($"   [DEBUG] -> Não reconhecido como WebAppUI nem WebAppDS. Ignorado.");
            }
        }

        return webApps;
    }
}