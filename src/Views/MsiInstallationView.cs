using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Services;
using InstaladorNewAcesso.Utils;

namespace InstaladorNewAcesso.Views;

public class MsiInstallationView
{
    private const string GoogleApiKey = "SUA_API_KEY_AQUI";

    public async Task ExecuteAsync(InstallationPaths paths)
    {
        MostrarCabecalho();

        var dbChoice = PerguntarTipoBanco();
        if (dbChoice == null) return;

        var msiSourceDir = await ObterDiretorioFonte(paths);
        if (msiSourceDir == null) return;

        var scanner = new MsiScanner(paths, dbChoice, msiSourceDir);

        // MSIs obrigatórios
        var tasks = scanner.Scan();

        // Fabricantes: usuário escolhe individualmente
        var fabricantesDisponiveis = scanner.ScanFabricantes();
        if (fabricantesDisponiveis.Count > 0)
        {
            var selecionados = SelecionarFabricantes(fabricantesDisponiveis);
            tasks.AddRange(selecionados);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n Nenhum fabricante encontrado.");
            Console.ResetColor();
        }

        if (tasks.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n Nenhum MSI encontrado para instalação.");
            Console.ResetColor();
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"\n Foram encontrados {tasks.Count} pacotes para instalar.");
        foreach (var task in tasks)
            Console.WriteLine($"  - {Path.GetFileName(task.MsiPath)} → {(task.IsWebApp ? "Web App" : "App de pasta")}");

        Console.Write("\n Deseja continuar com a instalação? (S/N): ");
        if (Console.ReadLine()?.Trim().ToUpper() != "S")
        {
            Console.WriteLine(" Instalação cancelada.");
            return;
        }

        int sucessos = 0, falhas = 0;

        foreach (var task in tasks)
        {
            Console.Write($"\n Instalando {Path.GetFileName(task.MsiPath)}... ");

            string args;
            if (task.IsWebApp)
            {
                // Web apps do IIS: não passar TARGETDIR.
                // O MSI usa o diretório físico já configurado no Site.
                args = "";
                if (!string.IsNullOrEmpty(task.SiteName))    args += $"SITE=\"{task.SiteName}\" ";
                if (!string.IsNullOrEmpty(task.AppPoolName)) args += $"APPPOOL=\"{task.AppPoolName}\" ";
            }
            else
            {
                Directory.CreateDirectory(task.TargetDirectory);
                args = $"TARGETDIR=\"{task.TargetDirectory}\"";
            }
            if (!string.IsNullOrEmpty(task.ExtraArgs)) args += $" {task.ExtraArgs}";

            var success = await MsiInstaller.InstallMsiAsync(task.MsiPath, args);
            if (success) sucessos++; else falhas++;

            if (success && task.FilesToCopy.Count > 0)
                CopiarArquivosExtras(task.FilesToCopy);
        }

        MostrarResumo(sucessos, falhas);
    }

    // -------------------------------------------------------------------------
    // Seleção de fabricantes
    // -------------------------------------------------------------------------

    private List<MsiInstallationModel> SelecionarFabricantes(
        Dictionary<string, MsiInstallationModel> fabricantes)
    {
        var nomes = fabricantes.Keys.OrderBy(n => n).ToList();
        var selecionados = new HashSet<int>();

        while (true)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==================================================");
            Console.WriteLine("          SELEÇÃO DE FABRICANTES                  ");
            Console.WriteLine("==================================================");
            Console.ResetColor();
            Console.WriteLine(" Digite o número para marcar/desmarcar.");
            Console.WriteLine(" [A] Todos   [0] Confirmar\n");

            for (int i = 0; i < nomes.Count; i++)
            {
                var marcado = selecionados.Contains(i) ? "[X]" : "[ ]";
                Console.ForegroundColor = selecionados.Contains(i) ? ConsoleColor.Green : ConsoleColor.White;
                Console.WriteLine($"  {marcado} {i + 1,2}. {nomes[i]}");
                Console.ResetColor();
            }

            Console.Write("\n Opção: ");
            var input = Console.ReadLine()?.Trim().ToUpper();

            if (input == "0") break;

            if (input == "A")
            {
                if (selecionados.Count == nomes.Count) selecionados.Clear();
                else for (int i = 0; i < nomes.Count; i++) selecionados.Add(i);
                continue;
            }

            if (int.TryParse(input, out int idx) && idx >= 1 && idx <= nomes.Count)
            {
                var i = idx - 1;
                if (selecionados.Contains(i)) selecionados.Remove(i);
                else selecionados.Add(i);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" Opção inválida.");
                Console.ResetColor();
            }
        }

        var escolhidos = selecionados.OrderBy(i => i).Select(i => fabricantes[nomes[i]]).ToList();

        Console.ForegroundColor = escolhidos.Count > 0 ? ConsoleColor.Green : ConsoleColor.Yellow;
        Console.WriteLine(escolhidos.Count > 0
            ? $"\n Fabricantes selecionados: {string.Join(", ", selecionados.OrderBy(i => i).Select(i => nomes[i]))}"
            : "\n Nenhum fabricante selecionado.");
        Console.ResetColor();

        return escolhidos;
    }

    // -------------------------------------------------------------------------
    // Cópia de arquivos extras (.Configuracao.dll)
    // -------------------------------------------------------------------------

    private void CopiarArquivosExtras(Dictionary<string, string> filesToCopy)
    {
        foreach (var (origem, destino) in filesToCopy)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
                File.Copy(origem, destino, overwrite: true);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  [COPIADO] {Path.GetFileName(destino)} → {Path.GetDirectoryName(destino)}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [ERRO] Falha ao copiar {Path.GetFileName(origem)}: {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    // -------------------------------------------------------------------------
    // Fonte dos instaladores
    // -------------------------------------------------------------------------

    private async Task<string?> ObterDiretorioFonte(InstallationPaths paths)
    {
        Console.WriteLine("\n De onde deseja obter os instaladores?");
        Console.WriteLine(" [1] Local");
        Console.WriteLine(" [2] Google Drive");
        Console.WriteLine(" [0] Cancelar");
        Console.Write("\n Escolha: ");

        return Console.ReadLine()?.Trim() switch
        {
            "1" => ObterDiretorioLocal(paths),
            "2" => await BaixarDoDrive(paths),
            _   => null
        };
    }

    private string? ObterDiretorioLocal(InstallationPaths paths)
    {
        var defaultPath = Path.Combine(paths.InstallationPath, "PrimeAcesso V5.9");

        Console.WriteLine($"\n Caminho padrão: {defaultPath}");
        Console.Write(" Pressione Enter para usar o padrão ou digite outro caminho: ");
        var input = Console.ReadLine()?.Trim();

        var dir = string.IsNullOrEmpty(input) ? defaultPath : input;

        if (!Directory.Exists(dir))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n [ERRO] Pasta não encontrada: {dir}");
            Console.ResetColor();
            Console.ReadKey();
            return null;
        }

        return dir;
    }

    private async Task<string?> BaixarDoDrive(InstallationPaths paths)
    {
        Console.Write("\n Cole o link da pasta no Google Drive: ");
        var url = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(url)) return null;

        var folderId = GoogleDriveDownloader.ExtractFolderId(url);
        if (folderId == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" Não foi possível extrair o ID da pasta.");
            Console.ResetColor();
            Console.ReadKey();
            return null;
        }

        var destino = Path.Combine(paths.InstallationPath, "PrimeAcesso V5.9");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n Baixando para: {destino}");
        Console.WriteLine(" Isso pode levar alguns minutos...\n");
        Console.ResetColor();

        try
        {
            var downloader = new GoogleDriveDownloader(GoogleApiKey);
            var progress = new Progress<string>(msg =>
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  {msg}");
                Console.ResetColor();
            });

            await downloader.DownloadFolderAsync(folderId, destino, progress);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n [SUCESSO] Download concluído.");
            Console.ResetColor();
            return destino;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n [ERRO] {ex.Message}");
            Console.ResetColor();
            Console.ReadKey();
            return null;
        }
    }

    // -------------------------------------------------------------------------
    // UI helpers
    // -------------------------------------------------------------------------

    private string? PerguntarTipoBanco()
    {
        Console.WriteLine("\n Qual banco de dados será utilizado?");
        Console.WriteLine(" [1] SQL Server");
        Console.WriteLine(" [2] Oracle");
        Console.WriteLine(" [0] Cancelar");
        Console.Write("\n Escolha: ");

        return Console.ReadLine()?.Trim() switch
        {
            "1" => "SQLServer",
            "2" => "Oracle",
            _   => null
        };
    }

    private void MostrarCabecalho()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================");
        Console.WriteLine("     INSTALAÇÃO DOS SISTEMAS NEW ACESSO           ");
        Console.WriteLine("==================================================");
        Console.ResetColor();
    }

    private void MostrarResumo(int sucessos, int falhas)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n==================================================");
        Console.WriteLine("      RESULTADO DA INSTALAÇÃO DOS MSIs            ");
        Console.WriteLine("==================================================");
        Console.ResetColor();

        Console.WriteLine($" Pacotes instalados com sucesso: {sucessos}");
        if (falhas > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Pacotes com falha: {falhas}");
            Console.WriteLine(" Verifique os logs em %temp% para mais detalhes.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" Todas as aplicações foram instaladas com sucesso!");
            Console.ResetColor();
        }
        Console.WriteLine("\n Pressione qualquer tecla para continuar...");
        Console.ReadKey();
    }
}