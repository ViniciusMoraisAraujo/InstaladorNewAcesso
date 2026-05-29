using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Utils;

namespace InstaladorNewAcesso.Views;

public class IISView
{
    private readonly IISInstaler _installer = new();

    public async Task ExecuteAsync(InstallationPaths paths)
    {
        ShowHeader();

        await ConfigureAppPool("WebAppDS", "v4.0", "Integrated", paths);
        await ConfigureAppPool("WebAppUI", "v4.0", "Integrated", paths);

        await ConfigureSite("WebAppDS", "WebAppDS", paths.WebAppDS, 8080);
        await ConfigureSite("WebAppUI", "WebAppUI", paths.WebAppUI, 8081);

        ShowFinishedMessage();
    }

    private async Task ConfigureAppPool(string name, string runtime, string pipeline, InstallationPaths paths)
    {
        Console.Write($"\n Verificando AppPool: {name.PadRight(20)}... ");

        if (await _installer.AppPoolExistsAsync(name))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[IGNORADO] Já existe.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("[CRIANDO]");
        Console.ResetColor();

        bool sucesso = await _installer.CreateApplicationPoolAsync(name, runtime, pipeline);

        Console.ForegroundColor = sucesso ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine(sucesso ? $"-> [SUCESSO] AppPool {name} criada." : $"-> [FALHA] Erro ao criar AppPool {name}.");
        Console.ResetColor();
    }

    private async Task ConfigureSite(string name, string poolName, string physicalPath, int port)
    {
        Console.Write($"\n Verificando Site: {name.PadRight(20)}... ");

        if (await _installer.SiteExistsAsync(name))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[IGNORADO] Já existe.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("[CRIANDO]");
        Console.ResetColor();

        bool sucesso = await _installer.CreateSiteAsync(name, poolName, physicalPath, port);

        Console.ForegroundColor = sucesso ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine(sucesso ? $"-> [SUCESSO] Site {name} criado na porta {port}." : $"-> [FALHA] Erro ao criar Site {name}.");
        Console.ResetColor();
    }

    private void ShowHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================");
        Console.WriteLine("          CONFIGURAÇÃO DO IIS                     ");
        Console.WriteLine("==================================================");
        Console.ResetColor();
    }

    private void ShowFinishedMessage()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n==================================================");
        Console.WriteLine("      Fim da etapa de Configuração do IIS.        ");
        Console.WriteLine("==================================================");
        Console.ResetColor();
        Console.ReadKey();
    }
}