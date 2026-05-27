using InstaladorNewAcesso.Implementations;
using InstaladorNewAcesso.Models;


namespace InstaladorNewAcesso.Views;

public class ResourceView
{
    public async Task ExecuteInstallAsync()
    {
        MostrarCabecalho();
        var installer = InstallerFactory.Create();
        string? sxsPath = ObterCaminhoSxsPorMenu();

        if (sxsPath == "SAIR") { /* ... */ return; }

        MostrarCarregamentoInicial();

        var setup = new FeatureSetup();

        // 1. Verifica todas em paralelo
        Console.WriteLine("\n Verificando recursos instalados...");
        var checkTasks = setup.Features
            .Select(async feature => new
            {
                Feature = feature,
                IsInstalled = await installer.IsFeatureInstalledAsync(feature)
            });

        var results = await Task.WhenAll(checkTasks);

        var toInstall = results
            .Where(r => !r.IsInstalled)
            .Select(r => r.Feature)
            .ToList();

        foreach (var installed in results.Where(r => r.IsInstalled))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n {installed.Feature.FriendlyName.PadRight(30)} [IGNORADO]");
            Console.ResetColor();
        }

        Console.WriteLine($"\n {results.Count(r => r.IsInstalled)} já instalados. {toInstall.Count} para instalar.");

        foreach (var feature in toInstall)
        {
            Console.Write($"\n Instalando: {feature.FriendlyName.PadRight(30)}... ");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("[INSTALANDO]");
            Console.ResetColor();

            bool sucesso = await installer.InstallFeatureAsync(feature, sxsPath);

            Console.ForegroundColor = sucesso ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine(sucesso ? "-> [SUCESSO]" : "-> [FALHA]");
            Console.ResetColor();
        }

        MostrarFimEtapa();
    }
    private void MostrarCabecalho()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================");
        Console.WriteLine("   INSTALADOR NEW ACESSO: RECURSOS DO WINDOWS   ");
        Console.WriteLine("==================================================");
        Console.ResetColor();
        Console.WriteLine();
    }

    private void MostrarCarregamentoInicial()
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("\n Carregando Recursos do Sistema...");
        Console.ResetColor();
        Console.WriteLine(new string('-', 50));
    }

    private string? ObterCaminhoSxsPorMenu()
    {
        Console.WriteLine("Deseja realizar a instalação online ou offline?");
        Console.WriteLine("[1] Online (Padrão - Requer Internet)");
        Console.WriteLine("[2] Offline (Utilizando pasta sxs/mídia do Windows)");
        Console.Write("\nEscolha uma opção: ");

        string opcao = Console.ReadLine() ?? "1";
        string? sxsPath = null;

        if (opcao == "2")
        {
            Console.Write("\nDigite o caminho completo da pasta sxs (Ex: D:\\sources\\sxs) ou digite '2' para sair: ");
            sxsPath = Console.ReadLine();
    
            if (sxsPath == "2") return "SAIR"; 

            while (string.IsNullOrWhiteSpace(sxsPath) || !Directory.Exists(sxsPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Caminho inválido ou inacessível.");
                Console.ResetColor();
        
                Console.WriteLine("Digite o caminho da pasta sxs ou digite '2' para sair: ");
                sxsPath = Console.ReadLine();

                if (sxsPath == "2")
                {
                    break; 
                }
            }
    
            if (sxsPath == "2") return "SAIR";
        }

        return sxsPath;
    }

    private void MostrarFimEtapa()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n==================================================");
        Console.WriteLine("      Fim da etapa de Recursos do Windows.        ");
        Console.WriteLine("==================================================");
        Console.ResetColor();
        Console.ReadKey();
    }
}