using InstaladorNewAcesso.Models;
using Spectre.Console;

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
            AnsiConsole.MarkupLine($"[gray][[DEBUG]] Diretório não encontrado: {_msiSourceRoot.EscapeMarkup()}[/]");
            return webApps;
        }

        AnsiConsole.MarkupLine($"[gray][[DEBUG]] Escaneando: {_msiSourceRoot.EscapeMarkup()}[/]");

        // Escaneia subpastas de primeiro nível
        foreach (var subDir in Directory.GetDirectories(_msiSourceRoot))
        {
            var folderName = Path.GetFileName(subDir);
            AnsiConsole.MarkupLine($"[gray][[DEBUG]] Verificando pasta: {folderName.EscapeMarkup()}[/]");

            bool isDbFolder =
                folderName.Equals("SQLServer", StringComparison.OrdinalIgnoreCase) ||
                folderName.Equals("Oracle", StringComparison.OrdinalIgnoreCase);

            if (isDbFolder)
            {
                if (folderName.Equals(_dbChoice, StringComparison.OrdinalIgnoreCase))
                {
                    AnsiConsole.MarkupLine($"[gray][[DEBUG]] Pasta do banco '{folderName.EscapeMarkup()}' corresponde. Escaneando...[/]");
                    AddRange(webApps, ScanDirectory(subDir, SearchOption.AllDirectories), addedMsiPaths);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[gray][[DEBUG]] Pasta do banco '{folderName.EscapeMarkup()}' ignorada (não é '{_dbChoice.EscapeMarkup()}')[/]");
                }
                continue;
            }

            AddRange(webApps, ScanDirectory(subDir, SearchOption.AllDirectories), addedMsiPaths);
        }

        // Escaneia apenas a raiz (TopDirectoryOnly para não reprocessar subpastas já visitadas)
        AnsiConsole.MarkupLine($"[gray][[DEBUG]] Verificando raiz...[/]");
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
                AnsiConsole.MarkupLine($"[gray][[DEBUG]] MSI duplicado ignorado: {model.MsiPath.EscapeMarkup()}[/]");
        }
    }

    private List<WebAppModel> ScanDirectory(string directory, SearchOption searchOption)
    {
        var webApps = new List<WebAppModel>();

        var msiFiles = Directory.GetFiles(directory, "*.msi", searchOption);
        AnsiConsole.MarkupLine($"[gray][[DEBUG]] Encontrados {msiFiles.Length} arquivos .msi em: {directory.EscapeMarkup()}[/]");

        foreach (var msiPath in msiFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(msiPath);
            AnsiConsole.MarkupLine($"[gray][[DEBUG]] Verificando: {fileName.EscapeMarkup()}[/]");

            // Identificar WebAppDS (verificado antes de UI para evitar falso positivo
            // caso um nome contenha ambas as siglas)
            if (fileName.Contains("DS", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine($"[gray][[DEBUG]] -> Detectado como WebAppDS![/]");
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
                AnsiConsole.MarkupLine($"[gray][[DEBUG]] -> Detectado como WebAppUI![/]");
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
                AnsiConsole.MarkupLine($"[gray][[DEBUG]] -> Não reconhecido como WebAppUI nem WebAppDS. Ignorado.[/]");
            }
        }

        return webApps;
    }
}
