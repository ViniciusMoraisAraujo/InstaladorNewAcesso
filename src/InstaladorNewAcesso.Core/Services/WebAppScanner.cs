using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Core.Services;

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
        // Rastreia MSIs j� adicionados para evitar duplicatas quando um arquivo
        // aparece tanto na raiz quanto em uma subpasta escaneada recursivamente.
        var addedMsiPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(_msiSourceRoot))
        {
            UIScope.WriteMessage($"[gray][[DEBUG]] Diretrio no encontrado: {MarkupHelper.Escape(_msiSourceRoot)}[/]");
            return webApps;
        }

        UIScope.WriteMessage($"[gray][[DEBUG]] Escaneando: {MarkupHelper.Escape(_msiSourceRoot)}[/]");

        // Escaneia subpastas de primeiro nível
        foreach (var subDir in Directory.GetDirectories(_msiSourceRoot))
        {
            var folderName = Path.GetFileName(subDir);
            UIScope.WriteMessage($"[gray][[DEBUG]] Verificando pasta: {MarkupHelper.Escape(folderName)}[/]");

            var isDbFolder = IsDatabaseFolder(folderName);

            if (isDbFolder)
            {
                if (MatchesDatabaseChoice(folderName, _dbChoice))
                {
                    UIScope.WriteMessage($"[gray][[DEBUG]] Pasta do banco '{MarkupHelper.Escape(folderName)}' corresponde. Escaneando...[/]");
                    AddRange(webApps, ScanDirectory(subDir, SearchOption.AllDirectories), addedMsiPaths);
                }
                else
                {
                    UIScope.WriteMessage($"[gray][[DEBUG]] Pasta do banco '{MarkupHelper.Escape(folderName)}' ignorada (não é '{MarkupHelper.Escape(_dbChoice)}')[/]");
                }
                continue;
            }

            AddRange(webApps, ScanDirectory(subDir, SearchOption.AllDirectories), addedMsiPaths);
        }

        // Escaneia apenas a raiz (TopDirectoryOnly para n�o reprocessar subpastas j� visitadas)
        UIScope.WriteMessage($"[gray][[DEBUG]] Verificando raiz...[/]");
        AddRange(webApps, ScanDirectory(_msiSourceRoot, SearchOption.TopDirectoryOnly), addedMsiPaths);

        return webApps;
    }

    private static void AddRange(
        List<WebAppModel> target,
        List<WebAppModel> source,
        HashSet<string> addedPaths)
    {
        foreach (var model in source)
        {
            if (addedPaths.Add(model.MsiPath))
                target.Add(model);
            else
                UIScope.WriteMessage($"[gray][[DEBUG]] MSI duplicado ignorado: {MarkupHelper.Escape(model.MsiPath)}[/]");
        }
    }

    public static bool IsDatabaseFolder(string folderName) =>
        folderName.Contains("SQLServer", StringComparison.OrdinalIgnoreCase) ||
        folderName.Contains("SQL Server", StringComparison.OrdinalIgnoreCase) ||
        folderName.Contains("Oracle", StringComparison.OrdinalIgnoreCase);

    public static bool MatchesDatabaseChoice(string folderName, string dbChoice)
    {
        if (dbChoice.Equals("SQLServer", StringComparison.OrdinalIgnoreCase))
        {
            return (folderName.Contains("SQLServer", StringComparison.OrdinalIgnoreCase) ||
                    folderName.Contains("SQL Server", StringComparison.OrdinalIgnoreCase)) &&
                   !folderName.Contains("Oracle", StringComparison.OrdinalIgnoreCase);
        }
        if (dbChoice.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
        {
            return folderName.Contains("Oracle", StringComparison.OrdinalIgnoreCase) &&
                   !folderName.Contains("SQLServer", StringComparison.OrdinalIgnoreCase) &&
                   !folderName.Contains("SQL Server", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private List<WebAppModel> ScanDirectory(string directory, SearchOption searchOption)
    {
        var webApps = new List<WebAppModel>();

        var msiFiles = Directory.GetFiles(directory, "*.msi", searchOption);
        UIScope.WriteMessage($"[gray][[DEBUG]] Encontrados {msiFiles.Length} arquivos .msi em: {MarkupHelper.Escape(directory)}[/]");

        foreach (var msiPath in msiFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(msiPath);
            UIScope.WriteMessage($"[gray][[DEBUG]] Verificando: {MarkupHelper.Escape(fileName)}[/]");

            // Identificar WebAppDS (verificado antes de UI para evitar falso positivo
            // caso um nome contenha ambas as siglas)
            if (fileName.Contains("DS", StringComparison.OrdinalIgnoreCase) ||
                fileName.Contains("WebDataService", StringComparison.OrdinalIgnoreCase))
            {
                UIScope.WriteMessage($"[gray][[DEBUG]] -> Detectado como WebAppDS![/]");
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
            else if (fileName.Contains("UI", StringComparison.OrdinalIgnoreCase) ||
                     fileName.Contains("WebAppUI", StringComparison.OrdinalIgnoreCase) ||
                     fileName.Contains("WebUI", StringComparison.OrdinalIgnoreCase))
            {
                UIScope.WriteMessage($"[gray][[DEBUG]] -> Detectado como WebAppUI![/]");
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
                UIScope.WriteMessage($"[gray][[DEBUG]] -> Não reconhecido como WebAppUI nem WebAppDS. Ignorado.[/]");
            }
        }

        return webApps;
    }
}
