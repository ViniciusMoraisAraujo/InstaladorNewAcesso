using System.Diagnostics;

namespace InstaladorNewAcesso.Utils;

public static class ProcessExecutor
{
    private static ProcessStartInfo CreateStartInfo(string arguments) => new()
    {
        FileName = "powershell.exe",
        Arguments = arguments,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
    };
    
    public static async Task<bool> RunPowerShellCommandAsync(string arguments, string featureName)
    {
        var startInfo = CreateStartInfo(arguments);

        try
        {
            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[SUCESSO] Ativado: {featureName}");
                Console.WriteLine(output.Trim());

                return true;
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] Ativado: {featureName}");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[AVISO DE VERIFICAÇÃO] {error.Trim()}");
            return false;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] Erro: {featureName}");
            Console.WriteLine(ex.Message);
            return false;
        }
        finally
        {
            Console.ResetColor();
        }
    }

    public static async Task<string> RunPowerShellWithOutputAsync(string arguments)
    {
        var startInfo = CreateStartInfo(arguments);
        try
        {
            using var process = new Process { StartInfo = startInfo };
            process.Start();
        
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[AVISO DE VERIFICAÇÃO] {error.Trim()}");
                Console.ResetColor();
                return string.Empty;
            }

            return output.Trim();
        }
        catch(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERRO] Falha ao executar PowerShell: {ex.Message}");
            Console.ResetColor();
            return string.Empty;
            
        }
    }
}