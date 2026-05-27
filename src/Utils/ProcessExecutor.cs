using System.Diagnostics;

namespace InstaladorNewAcesso.Utils;

public static class ProcessExecutor
{
    public static async Task<bool> RunPowerShellCommandAsync(string arguments, string featureName)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Verb = "runas"
        };

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
                return true;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] Ativado: {featureName}");
            return false;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] Erro: {featureName}");
            return false;
        }
        finally
        {
            Console.ResetColor();
        }
    }
}