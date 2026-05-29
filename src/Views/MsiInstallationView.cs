using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Services;
using InstaladorNewAcesso.Utils;

namespace InstaladorNewAcesso.Views;

public class MsiInstallationView
{
    private string GetGoogleApiKey()
    {
        var apiKey = Environment.GetEnvironmentVariable("NEWACESSO_GOOGLE_API_KEY");
        if (!string.IsNullOrEmpty(apiKey)) return apiKey;
        
        return "SUA_API_KEY_VERDADEIRA_AQUI"; 
    }

    public async Task ExecuteAsync(InstallationPaths paths)
    {
        ShowHeader();

        var dbChoice = PromptDatabaseType();
        if (dbChoice == null) return;

        var msiSourceDir = await GetSourceDirectory(paths);
        if (msiSourceDir == null) return;

        var logger = new ConsoleLogger();
        var classifier = new MsiClassifier(paths, logger);
        var scanner = new MsiScanner(paths, dbChoice, msiSourceDir, classifier, logger);

        var tasks = scanner.Scan();

        var manufacturersAvailable = scanner.ScanManufacturers();
        if (manufacturersAvailable.Count > 0)
        {
            var selectedManufacturers = SelectManufacturers(manufacturersAvailable);
            tasks.AddRange(selectedManufacturers);
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

        int successes = 0, failures = 0;

        foreach (var task in tasks)
        {
            Console.Write($"\n Instalando {Path.GetFileName(task.MsiPath)}... ");

            string args = $"TARGETDIR=\"{task.TargetDirectory}\"";

            if (task.IsWebApp)
            {
                if (!string.IsNullOrEmpty(task.SiteName))    args += $" WEBSITE=\"{task.SiteName}\"";
                if (!string.IsNullOrEmpty(task.AppPoolName)) args += $" APPPOOL=\"{task.AppPoolName}\"";
            }

            if (!string.IsNullOrEmpty(task.ExtraArgs)) 
                args += $" {task.ExtraArgs}";

            var success = await MsiInstaller.InstallMsiAsync(task.MsiPath, args);
            if (success) successes++; else failures++;

            if (success && task.FilesToCopy.Count > 0)
                CopyExtraFiles(task.FilesToCopy);
        }

        ShowSummary(successes, failures);
    }
    
    private List<MsiInstallationModel> SelectManufacturers(Dictionary<string, MsiInstallationModel> manufacturers)
    {
        var names = manufacturers.Keys.OrderBy(n => n).ToList();
        var selected = new HashSet<int>();

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

            for (int i = 0; i < names.Count; i++)
            {
                var marked = selected.Contains(i) ? "[X]" : "[ ]";
                Console.ForegroundColor = selected.Contains(i) ? ConsoleColor.Green : ConsoleColor.White;
                Console.WriteLine($"  {marked} {i + 1,2}. {names[i]}");
                Console.ResetColor();
            }

            Console.Write("\n Opção: ");
            var input = Console.ReadLine()?.Trim().ToUpper();

            if (input == "0") break;

            if (input == "A")
            {
                if (selected.Count == names.Count) selected.Clear();
                else for (int i = 0; i < names.Count; i++) selected.Add(i);
                continue;
            }

            if (int.TryParse(input, out int idx) && idx >= 1 && idx <= names.Count)
            {
                var i = idx - 1;
                if (selected.Contains(i)) selected.Remove(i);
                else selected.Add(i);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" Opção inválida.");
                Console.ResetColor();
            }
        }

        var chosen = selected.OrderBy(i => i).Select(i => manufacturers[names[i]]).ToList();

        Console.ForegroundColor = chosen.Count > 0 ? ConsoleColor.Green : ConsoleColor.Yellow;
        Console.WriteLine(chosen.Count > 0
            ? $"\n Fabricantes selecionados: {string.Join(", ", selected.OrderBy(i => i).Select(i => names[i]))}"
            : "\n Nenhum fabricante selecionado.");
        Console.ResetColor();

        return chosen;
    }
    

    private void CopyExtraFiles(Dictionary<string, string> filesToCopy)
    {
        foreach (var (source, destination) in filesToCopy)
        {
            try
            {
                File.Copy(source, destination, overwrite: true);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  [COPIADO] {Path.GetFileName(destination)} → {Path.GetDirectoryName(destination)}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [ERRO] Falha ao copiar {Path.GetFileName(source)}: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
    

    private async Task<string?> GetSourceDirectory(InstallationPaths paths)
    {
        Console.WriteLine("\n De onde deseja obter os instaladores?");
        Console.WriteLine(" [1] Local");
        Console.WriteLine(" [2] Google Drive");
        Console.WriteLine(" [0] Cancelar");
        Console.Write("\n Escolha: ");

        return Console.ReadLine()?.Trim() switch
        {
            "1" => GetLocalDirectory(paths),
            "2" => await DownloadFromDrive(paths),
            _   => null
        };
    }

    private string? GetLocalDirectory(InstallationPaths paths)
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

    private async Task<string?> DownloadFromDrive(InstallationPaths paths)
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
            var downloader = new GoogleDriveDownloader(GetGoogleApiKey());
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

    private string? PromptDatabaseType()
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

    private void ShowHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================");
        Console.WriteLine("     INSTALAÇÃO DOS SISTEMAS NEW ACESSO           ");
        Console.WriteLine("==================================================");
        Console.ResetColor();
    }

    private void ShowSummary(int successes, int failures)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n==================================================");
        Console.WriteLine("      RESULTADO DA INSTALAÇÃO DOS MSIs            ");
        Console.WriteLine("==================================================");
        Console.ResetColor();

        Console.WriteLine($" Pacotes instalados com sucesso: {successes}");
        if (failures > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" Pacotes com falha: {failures}");
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

