using System.Diagnostics;

namespace InstaladorNewAcesso.Utils;

public static class MsiInstaller
{
    public static async Task<bool> InstallMsiAsync(string msiPath, string arguments)
    {
        if (!File.Exists(msiPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Arquivo MSI não encontrado: {msiPath}");
            Console.ResetColor();
            return false;
        }

        var fullArgs = $"/i \"{msiPath}\" {arguments} /quiet /norestart /lv \"{Path.GetTempPath()}{Path.GetFileNameWithoutExtension(msiPath)}_install.log\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = "msiexec.exe",
            Arguments = fullArgs,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
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
                Console.WriteLine($"[SUCESSO] Instalação concluída: {Path.GetFileName(msiPath)}");
                return true;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FALHA] Código de erro {process.ExitCode} ao instalar: {Path.GetFileName(msiPath)}");
            if (!string.IsNullOrWhiteSpace(error))
                Console.WriteLine($"Detalhe: {error}");
            Console.ResetColor();
            return false;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Exceção ao executar msiexec: {ex.Message}");
            Console.ResetColor();
            return false;
        }
        finally
        {
            Console.ResetColor();
        }
    }
}