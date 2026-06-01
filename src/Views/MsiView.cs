using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Services;

namespace InstaladorNewAcesso.Views;

public class MsiView
{
    private readonly MsiInstaller _installer = new();
    private List<MsiInstallationModel> _todosMsi = new();
    private List<MsiInstallationModel> _outros = new();
    private List<MsiInstallationModel> _fabricantes = new();

    public async Task ExecuteAsync(InstallationPaths paths)
    {
        ShowHeader();

        Console.Write("\n Diretório raiz dos instaladores MSI ou ENTER para usar o padrão (ex: \\Installers\\PrimeAcesso V5.9): ");
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

        Console.Write(" Banco de dados (SQLServer/Oracle): ");
        var dbChoice = Console.ReadLine()?.Trim() ?? "SQLServer";

        // --- Escaneamento ---
        var scanner = new MsiScanner(paths, dbChoice, msiRoot);
        Console.Write("\n Escaneando MSIs".PadRight(30) + "... ");
        try
        {
            _todosMsi = scanner.Scan();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[" + _todosMsi.Count + " ENCONTRADOS]");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[FALHA] " + ex.Message);
            Console.ResetColor();
            Console.ReadKey();
            return;
        }

        if (_todosMsi.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n Nenhum MSI regular encontrado (WebApps são ignorados).");
            Console.ResetColor();
        }
        else
        {
            SepararMsIs(paths.NewAcessoRoot);

            // 1. MSIs gerais
            if (_outros.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\n " + _outros.Count + " MSI(s) gerais encontrados (não fabricantes).");
                Console.ResetColor();
                Console.Write(" Deseja instalar todos eles? (S/N): ");
                if (Console.ReadLine()?.Trim().ToUpper() == "S")
                {
                    await InstalarListaAsync(_outros);
                }
                else
                {
                    Console.WriteLine(" Instalação dos MSIs gerais cancelada.");
                }
            }

            // 2. Fabricantes
            if (_fabricantes.Count > 0)
            {
                await TelaFabricantesAsync();
            }
            else if (_outros.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n Nenhum MSI encontrado.");
                Console.ResetColor();
            }
        }

        ShowFinishedMessage();
    }

    private void SepararMsIs(string root)
    {
        string fabricantesPath = Path.Combine(root, "Fabricantes");
        foreach (var msi in _todosMsi)
        {
            if (msi.TargetDirectory.StartsWith(fabricantesPath, StringComparison.OrdinalIgnoreCase))
                _fabricantes.Add(msi);
            else
                _outros.Add(msi);
        }
    }

    private async Task TelaFabricantesAsync()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================");
        Console.WriteLine("          FABRICANTES DISPONÍVEIS                 ");
        Console.WriteLine("==================================================");
        Console.ResetColor();

        Console.WriteLine("\n " + _fabricantes.Count + " MSI(s) de fabricantes encontrados:\n");
        for (int i = 0; i < _fabricantes.Count; i++)
        {
            // Uso explícito de string para evitar ambiguidade Path.GetFileName
            string nome = Path.GetFileName(_fabricantes[i].MsiPath) ?? "";
            string linha = string.Format("  {0,2}. {1,-50} -> {2}", i + 1, nome, _fabricantes[i].TargetDirectory);
            Console.WriteLine(linha);
        }

        Console.WriteLine("\n Opções:");
        Console.WriteLine("   T - Instalar TODOS os fabricantes");
        Console.WriteLine("   S - Selecionar manualmente (índices separados por vírgula)");
        Console.WriteLine("   N - Não instalar fabricantes");
        Console.Write("\n Sua escolha: ");
        var opcao = Console.ReadLine()?.Trim().ToUpper();

        switch (opcao)
        {
            case "T":
                await InstalarListaAsync(_fabricantes);
                break;
            case "S":
                Console.Write(" Digite os números dos fabricantes (ex: 1,3): ");
                var input = Console.ReadLine();
                var indicesLocais = ParseIndices(input, _fabricantes.Count);
                if (indicesLocais.Count > 0)
                {
                    var selecionados = indicesLocais.Select(i => _fabricantes[i]).ToList();
                    await InstalarListaAsync(selecionados);
                }
                else
                {
                    Console.WriteLine(" Nenhum índice válido. Nenhum fabricante será instalado.");
                }
                break;
            case "N":
                Console.WriteLine(" Fabricantes ignorados.");
                break;
            default:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(" Opção inválida. Fabricantes não serão instalados.");
                Console.ResetColor();
                break;
        }
    }

    private List<int> ParseIndices(string? input, int max)
    {
        var indices = new List<int>();
        if (string.IsNullOrWhiteSpace(input))
            return indices;

        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (int.TryParse(part.Trim(), out int num) && num >= 1 && num <= max)
            {
                indices.Add(num - 1);
            }
        }
        return indices.Distinct().OrderBy(i => i).ToList();
    }

    private async Task InstalarListaAsync(List<MsiInstallationModel> lista)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n Iniciando instalação de " + lista.Count + " MSI(s)...\n");
        Console.ResetColor();

        int sucessos = 0;
        for (int i = 0; i < lista.Count; i++)
        {
            var model = lista[i];
            string nome = Path.GetFileName(model.MsiPath) ?? "";
            string statusMsg = string.Format(" [{0}/{1}] {2,-50}... ", i + 1, lista.Count, nome);
            Console.Write(statusMsg);

            bool ok = await _installer.InstallAsync(model);

            if (ok)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[SUCESSO]");
                Console.ResetColor();
                sucessos++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FALHA]");
                Console.ResetColor();
            }
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n Instalação concluída. " + sucessos + "/" + lista.Count + " MSIs instalados com sucesso.");
        Console.ResetColor();
    }

    private void ShowHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================");
        Console.WriteLine("          INSTALAÇÃO DE APLICAÇÕES (MSIs)         ");
        Console.WriteLine("==================================================");
        Console.ResetColor();
    }

    private void ShowFinishedMessage()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n==================================================");
        Console.WriteLine("      Fim da etapa de Instalação de MSIs.         ");
        Console.WriteLine("==================================================");
        Console.ResetColor();
        Console.ReadKey();
    }
}