using InstaladorNewAcesso.Models;

namespace InstaladorNewAcesso.Views;

public class MainMenuView
{
    private readonly ResourceView _resourceView = new();
    private readonly DirectoryView _directoryView = new();
    private readonly IISView _iisView = new();
    private InstallationPaths? _paths;

    public async Task ExecuteAsync()
    {
        while (true)
        {
            MostrarMenu();
            var opcao = Console.ReadLine()?.Trim();

            switch (opcao)
            {
                case "1":
                    await _resourceView.ExecuteInstallAsync();
                    break;
                case "2":
                    if (!GarantirPaths()) break;
                    _directoryView.ExecuteDirectoryCreation(_paths!);
                    Console.ReadKey();
                    break;
                case "3":
                    if (!GarantirPaths()) break;
                    await _iisView.ExecuteAsync(_paths!);
                    break;
                case "0":
                    MostrarSaida();
                    return;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n Opção inválida. Tente novamente.");
                    Console.ResetColor();
                    Console.ReadKey();
                    break;
            }
        }
    }
    
    private void MostrarMenu()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================");
        Console.WriteLine("          INSTALADOR NEW ACESSO                   ");
        Console.WriteLine("==================================================");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine(" [1] Instalar Recursos do Windows");
        Console.WriteLine(" [2] Criar Diretórios");
        Console.WriteLine(" [3] Configurar IIS");
        Console.WriteLine(" [0] Sair");
        Console.WriteLine();
        Console.Write(" Escolha uma opção: ");
    }

    private void MostrarSaida()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n Encerrando instalador. Até logo!");
        Console.ResetColor();
    }
    private bool GarantirPaths()
    {
        if (_paths != null) return true;

        Console.Write("\n Digite o caminho base (Ex: C:\\SoftPrime ou D:\\SoftPrime): ");
        var basePath = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(basePath) || !Directory.Exists(Path.GetPathRoot(basePath)))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n [ERRO] Caminho inválido.");
            Console.ResetColor();
            Console.ReadKey();
            return false;
        }

        _paths = new InstallationPaths(basePath);
        return true;
    }
}