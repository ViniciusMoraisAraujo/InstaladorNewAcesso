using InstaladorNewAcesso.Configurations;
using InstaladorNewAcesso.Models;

namespace InstaladorNewAcesso.Views;

public class DirectoryView
{
    public void ExecuteDirectoryCreation(InstallationPaths basePath)
    {
        var setup = new DirectorySetup();
        
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("\n Criando estrutura de diretórios...");
        Console.ResetColor();
        Console.WriteLine(new string('-', 50));

        foreach (var path in setup.GetAllPaths(basePath))
        {
            if (Directory.Exists(path))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($" [IGNORADO] {path}");
            }
            else
            {
                Directory.CreateDirectory(path);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($" [CRIADO]   {path}");
            }
            Console.ResetColor();
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n==================================================");
        Console.WriteLine("      Fim da etapa de Diretórios.                 ");
        Console.WriteLine("==================================================");
        Console.ResetColor();
    }
}