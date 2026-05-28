using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Services;
using InstaladorNewAcesso.Utils;

namespace InstaladorNewAcesso.Views;

public class MsiInstallationView
{
    
    public async Task ExecuteAsync(InstallationPaths paths)
    {
        MostrarCabecalho();

        var dbChoice = PerguntarTipoBanco();
        if (dbChoice == null)
            return;

        var msiSourceDir = Path.Combine(paths.InstallationPath, "PrimeAcesso V5.9");
        if (!Directory.Exists(msiSourceDir))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Pasta dos instaladores não encontrada: {msiSourceDir}");
            Console.ResetColor();
            Console.ReadKey();
            return;
        }

        var scanner = new MsiScanner(paths, dbChoice, msiSourceDir);
        var tasks = scanner.Scan();

        if (tasks.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nNenhum MSI encontrado para instalação.");
            Console.ResetColor();
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"\nForam encontrados {tasks.Count} pacotes para instalar.");
        foreach (var task in tasks)
        {
            Console.WriteLine($" - {Path.GetFileName(task.MsiPath)} -> {(task.IsWebApp ? "Web App" : "App de pasta")}");
        }

        Console.Write("\nDeseja continuar com a instalação? (S/N): ");
        var resposta = Console.ReadLine()?.Trim().ToUpper();
        if (resposta != "S")
        {
            Console.WriteLine("Instalação cancelada.");
            return;
        }

        int sucessos = 0, falhas = 0;
        foreach (var task in tasks)
        {
            Console.Write($"\nInstalando {Path.GetFileName(task.MsiPath)}... ");

            Directory.CreateDirectory(task.TargetDirectory);

            var args = $"TARGETDIR=\"{task.TargetDirectory}\"";
            if (task.IsWebApp)
            {
                if (!string.IsNullOrEmpty(task.SiteName))
                    args += $" SITE=\"{task.SiteName}\"";
                if (!string.IsNullOrEmpty(task.AppPoolName))
                    args += $" APPPOOL=\"{task.AppPoolName}\"";
            }
            if (!string.IsNullOrEmpty(task.ExtraArgs))
                args += $" {task.ExtraArgs}";

            var success = await MsiInstaller.InstallMsiAsync(task.MsiPath, args);
            if (success) sucessos++;
            else falhas++;
        }

        MostrarResumo(sucessos, falhas);
    }

    private string? PerguntarTipoBanco()
    {
        Console.WriteLine("\nQual banco de dados será utilizado pelas aplicações?");
        Console.WriteLine("[1] SQL Server");
        Console.WriteLine("[2] Oracle");
        Console.WriteLine("[0] Cancelar");
        Console.Write("\nEscolha: ");

        var opcao = Console.ReadLine()?.Trim();
        return opcao switch
        {
            "1" => "SQLServer",
            "2" => "Oracle",
            "0" => null,
            _ => null
        };
    }

    private void MostrarCabecalho()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================");
        Console.WriteLine("          INSTALAÇÃO DOS SISTEMAS NEW ACESSO       ");
        Console.WriteLine("==================================================");
        Console.ResetColor();
    }

    private void MostrarResumo(int sucessos, int falhas)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n==================================================");
        Console.WriteLine("      RESULTADO DA INSTALAÇÃO DOS MSIs             ");
        Console.WriteLine("==================================================");
        Console.ResetColor();

        Console.WriteLine($"Pacotes instalados com sucesso: {sucessos}");
        if (falhas > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Pacotes com falha: {falhas}");
            Console.WriteLine("Verifique os logs em %temp% para mais detalhes.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Todas as aplicações foram instaladas com sucesso!");
            Console.ResetColor();
        }
        Console.WriteLine("\nPressione qualquer tecla para continuar...");
        Console.ReadKey();
    }
}