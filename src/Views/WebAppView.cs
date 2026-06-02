using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Services;
using InstaladorNewAcesso.Utils;

namespace InstaladorNewAcesso.Views;

public class WebAppView
{
    private readonly WebAppInstaller _installer = new();
    private List<WebAppModel> _webApps = new();

    public async Task ExecuteAsync(InstallationPaths paths)
    {
        ShowHeader();

        Console.Write("\n Diretório raiz dos instaladores WEBApp ou ENTER para usar o padrão: ");
        var msiRootInput = Console.ReadLine()?.Trim();

        string msiRoot;
        if (string.IsNullOrWhiteSpace(msiRootInput))
        {
            msiRoot = Path.Combine(paths.InstallationPath, "PrimeAcesso V5.9");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" Usando diretório padrão: " + msiRoot);
            Console.ResetColor();

            if (!Directory.Exists(msiRoot))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n [ERRO] Diretório padrão não encontrado: " + msiRoot);
                Console.ResetColor();
                Console.ReadKey();
                return;
            }
        }
        else
        {
            msiRoot = msiRootInput;
            if (!Directory.Exists(msiRoot))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n [ERRO] Diretório informado não encontrado.");
                Console.ResetColor();
                Console.ReadKey();
                return;
            }
        }

        Console.Write(" Banco de dados ([1]SQLServer/[2]Oracle): ");
        var input = Console.ReadLine()?.Trim();
        var dbChoice = (input == "2") ? "Oracle" : "SQLServer";

        var scanner = new WebAppScanner(paths, dbChoice, msiRoot);
        Console.Write("\n Escaneando Web Apps".PadRight(30) + "... ");
        _webApps = scanner.Scan();
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[{_webApps.Count} ENCONTRADOS]");
        Console.ResetColor();

        if (_webApps.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n Nenhum Web App encontrado (WebAppUI/WebAppDS).");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("\n Web Apps encontrados:\n");
            foreach (var app in _webApps)
            {
                string nome = Path.GetFileName(app.MsiPath);
                Console.WriteLine($"  • {nome}");
                Console.WriteLine($"    Site: {app.SiteName} | AppPool: {app.AppPoolName} | Porta: {app.Port}");
                Console.WriteLine($"    Origem (forçada): {app.ForcedInstallPath}");
                Console.WriteLine($"    Destino: {app.TargetDirectory}\n");
            }

            Console.Write(" Deseja instalar os Web Apps? (S/N): ");
            if (Console.ReadLine()?.Trim().ToUpper() == "S")
            {
                await InstallAllAsync();
            }
            else
            {
                Console.WriteLine(" Instalação cancelada.");
            }
        }

        ShowFinishedMessage();
    }

    private async Task InstallAllAsync()
    {
        int sucessos = 0;
        foreach (var app in _webApps)
        {
            bool ok = await _installer.InstallAsync(app);
            if (ok) sucessos++;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n Instalação concluída. {sucessos}/{_webApps.Count} Web Apps instalados.");
        Console.ResetColor();
    }

    private void ShowHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================");
        Console.WriteLine("          INSTALAÇÃO DE WEB APPS                  ");
        Console.WriteLine("==================================================");
        Console.ResetColor();
    }

    private void ShowFinishedMessage()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n==================================================");
        Console.WriteLine("      Fim da etapa de Instalação de Web Apps.     ");
        Console.WriteLine("==================================================");
        Console.ResetColor();
        Console.ReadKey();
    }
}