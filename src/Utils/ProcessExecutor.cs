using System.Diagnostics;
using Spectre.Console;

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
                AnsiConsole.MarkupLine($"[green][SUCESSO][/] Ativado: {featureName.EscapeMarkup()}");
                if (!string.IsNullOrWhiteSpace(output))
                    AnsiConsole.MarkupLine($"[gray]{output.Trim().EscapeMarkup()}[/]");
                return true;
            }

            AnsiConsole.MarkupLine($"[red][FAIL][/] Ativado: {featureName.EscapeMarkup()}");
            if (!string.IsNullOrWhiteSpace(error))
                AnsiConsole.MarkupLine($"[yellow][AVISO DE VERIFICAÇÃO][/] {error.Trim().EscapeMarkup()}");
            return false;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red][FAIL] Erro: {featureName.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"[red]{ex.Message.EscapeMarkup()}[/]");
            return false;
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
                if (!string.IsNullOrWhiteSpace(error))
                    AnsiConsole.MarkupLine($"[yellow][AVISO DE VERIFICAÇÃO][/] {error.Trim().EscapeMarkup()}");
                return string.Empty;
            }

            return output.Trim();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red][ERRO] Falha ao executar PowerShell: {ex.Message.EscapeMarkup()}[/]");
            return string.Empty;
        }
    }
}
