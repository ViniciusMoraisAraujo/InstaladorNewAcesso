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
        Verb = "runas"
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
        catch
        {
            return string.Empty;
        }
    }
}