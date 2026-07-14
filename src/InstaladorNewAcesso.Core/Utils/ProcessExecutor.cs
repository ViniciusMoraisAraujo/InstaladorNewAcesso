using System.Diagnostics;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

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

            // Read stdout and stderr in parallel to prevent deadlock
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode == 0)
            {
                UIScope.WriteMessage($"[green][[SUCESSO]][/] Ativado: {MarkupHelper.Escape(featureName)}");
                if (!string.IsNullOrWhiteSpace(output))
                    UIScope.WriteMessage($"[gray]{MarkupHelper.Escape(output.Trim())}[/]");
                return true;
            }

            UIScope.WriteMessage($"[red][[FAIL]][/] Ativado: {MarkupHelper.Escape(featureName)}");
            if (!string.IsNullOrWhiteSpace(error))
                UIScope.WriteMessage($"[yellow][[AVISO DE VERIFICAÇÃO]][/] {MarkupHelper.Escape(error.Trim())}");
            return false;
        }
        catch (Exception ex)
        {
            UIScope.WriteMessage($"[red][[FAIL]] Erro: {MarkupHelper.Escape(featureName)}[/]");
            UIScope.WriteMessage($"[red]{MarkupHelper.Escape(ex.Message)}[/]");
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

            // Read stdout and stderr in parallel to prevent deadlock
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                if (!string.IsNullOrWhiteSpace(error))
                    UIScope.WriteMessage($"[yellow][[AVISO DE VERIFICAÇÃO]][/] {MarkupHelper.Escape(error.Trim())}");
                return string.Empty;
            }

            return output.Trim();
        }
        catch (Exception ex)
        {
            UIScope.WriteMessage($"[red][[ERRO]] Falha ao executar PowerShell: {MarkupHelper.Escape(ex.Message)}[/]");
            return string.Empty;
        }
    }
}
