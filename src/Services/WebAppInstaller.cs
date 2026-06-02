using System.Diagnostics;
using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Utils;

namespace InstaladorNewAcesso.Services;

public class WebAppInstaller
{
    public async Task<bool> InstallAsync(WebAppModel model)
    {
        try
        {
            Console.WriteLine($"\n [{model.SiteName}] Iniciando instalação...");

            // Etapa 1: Instalar o MSI sem parâmetros
            Console.Write($"  1. Instalando MSI".PadRight(40) + "... ");
            bool installed = await InstallMsiSilentlyAsync(model);
            if (!installed)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FALHA]");
                Console.ResetColor();
                return false;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[OK]");
            Console.ResetColor();

            await Task.Delay(2000); // Aguardar finalização

            // Etapa 2: Localizar a subpasta criada
            string installedPath = Path.Combine(model.TargetDirectory, model.SiteName);
            if (!Directory.Exists(installedPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"   [ERRO] Subpasta esperada não encontrada: {installedPath}");
                Console.ResetColor();
                return false;
            }
            Console.WriteLine($"   [INFO] Arquivos em: {installedPath}");

            // Etapa 3: Mover conteúdo para a raiz do destino
            Console.Write($"  3. Movendo para raiz".PadRight(40) + "... ");
            bool moved = await MoveContentsToRootAsync(installedPath, model.TargetDirectory, model.SiteName);
            if (!moved)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FALHA]");
                Console.ResetColor();
                return false;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[OK]");
            Console.ResetColor();

            // Etapa 4: Reiniciar IIS
            Console.Write($"  4. Reiniciando IIS".PadRight(40) + "... ");
            bool restarted = await RestartIisAsync(model.SiteName, model.AppPoolName);
            if (!restarted)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[FALHA]");
                Console.ResetColor();
                return false;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[OK]");
            Console.ResetColor();

            return true;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n [ERRO] {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }

    /// <summary>
    /// Instala o MSI sem parâmetros. O instalador usará os defaults (site, app pool) já configurados no IIS.
    /// </summary>
    private async Task<bool> InstallMsiSilentlyAsync(WebAppModel model)
    {
        try
        {
            string args = $"/i \"{model.MsiPath}\" /qn";
            Console.WriteLine($"\n   [DEBUG] msiexec.exe {args}");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "msiexec.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            Console.WriteLine($"   [DEBUG] ExitCode: {process.ExitCode}");
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   [ERRO] {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Move todo o conteúdo da subpasta (ex: WebAppDS\WebAppDS) para a raiz (ex: WebAppDS).
    /// Depois remove a subpasta vazia.
    /// </summary>
    private async Task<bool> MoveContentsToRootAsync(string sourceSubfolder, string destinationRoot, string siteName)
    {
        try
        {
            // Parar o site para liberar arquivos
            string stopArgs = $"-Command \"Stop-Website -Name '{siteName}' -ErrorAction SilentlyContinue\"";
            await ProcessExecutor.RunPowerShellCommandAsync(stopArgs, $"Parar {siteName}");

            // Robocopy: move tudo da subpasta para a raiz (sobrescreve)
            string robocopyArgs = $"\"{sourceSubfolder}\" \"{destinationRoot}\" /E /MOVE /R:3 /W:5";
            Console.WriteLine($"   [DEBUG] robocopy.exe {robocopyArgs}");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "robocopy.exe",
                    Arguments = robocopyArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            Console.WriteLine($"   [DEBUG] Robocopy ExitCode: {process.ExitCode}");

            // Remove a subpasta vazia
            if (Directory.Exists(sourceSubfolder))
            {
                try { Directory.Delete(sourceSubfolder, true); }
                catch { /* não é crítico */ }
            }

            return process.ExitCode >= 0 && process.ExitCode <= 7;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   [ERRO] {ex.Message}");
            return false;
        }
    }

    private async Task<bool> RestartIisAsync(string siteName, string appPoolName)
    {
        string args = $"-Command \"" +
                      $"Restart-WebAppPool -Name '{appPoolName}'; " +
                      $"Start-Website -Name '{siteName}'\"";
        
        return await ProcessExecutor.RunPowerShellCommandAsync(args, $"Reiniciar {siteName}");
    }
}