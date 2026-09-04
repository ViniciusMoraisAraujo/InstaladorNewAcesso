using System.Diagnostics;
using System.Text;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

/// <summary>
/// Executor estático seguro e assíncrono para comandos PowerShell e processos do sistema.
/// </summary>
public static class ProcessExecutor
{
    private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Executa um comando PowerShell e retorna true se o processo finalizar com ExitCode == 0.
    /// </summary>
    public static async Task<bool> RunPowerShellCommandAsync(
        string arguments,
        string featureName,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(arguments);

        var effectiveTimeout = timeout ?? s_defaultTimeout;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(effectiveTimeout);

        var psi = CreatePowerShellStartInfo(arguments);

        Process? process = null;
        try
        {
            process = new Process { StartInfo = psi };
            if (!process.Start())
            {
                UIScope.WriteError($"Falha ao iniciar processo PowerShell para '{featureName}'.");
                return false;
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(cts.Token);
            var errorTask = process.StandardError.ReadToEndAsync(cts.Token);

            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            var output = await outputTask.ConfigureAwait(false);
            var error = await errorTask.ConfigureAwait(false);

            if (process.ExitCode == 0)
            {
                return true;
            }

            var errMsg = !string.IsNullOrWhiteSpace(error) ? error.Trim() : output.Trim();
            UIScope.WriteError($"Erro ao executar '{featureName}' (ExitCode {process.ExitCode}): {errMsg}");
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            UIScope.WriteWarning($"Execução de '{featureName}' cancelada pelo usuário.");
            return false;
        }
        catch (OperationCanceledException)
        {
            UIScope.WriteError($"Timeout de {effectiveTimeout.TotalMinutes} minutos excedido ao executar '{featureName}'.");
            return false;
        }
        catch (Exception ex)
        {
            UIScope.WriteError($"Exceção ao executar '{featureName}': {ex.Message}");
            return false;
        }
        finally
        {
            KillProcessSafely(process);
            process?.Dispose();
        }
    }

    /// <summary>
    /// Executa um comando PowerShell e retorna a saída padrão (stdout) formatada como string.
    /// Retorna string.Empty em caso de erro, exit code != 0 ou timeout.
    /// </summary>
    public static async Task<string> RunPowerShellWithOutputAsync(
        string arguments,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(arguments);

        var effectiveTimeout = timeout ?? s_defaultTimeout;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(effectiveTimeout);

        var psi = CreatePowerShellStartInfo(arguments);

        Process? process = null;
        try
        {
            process = new Process { StartInfo = psi };
            if (!process.Start())
                return string.Empty;

            var outputTask = process.StandardOutput.ReadToEndAsync(cts.Token);
            var errorTask = process.StandardError.ReadToEndAsync(cts.Token);

            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            var output = await outputTask.ConfigureAwait(false);
            _ = await errorTask.ConfigureAwait(false);

            if (process.ExitCode != 0)
                return string.Empty;

            return output.Trim();
        }
        catch (OperationCanceledException ex)
        {
            AuditLogger.Log("ProcessExecutor", $"PowerShell cancelado/timeout: {arguments[..Math.Min(80, arguments.Length)]}", false, ex.Message);
            return string.Empty;
        }
        catch (Exception ex)
        {
            AuditLogger.Log("ProcessExecutor", $"Erro ao executar PowerShell: {arguments[..Math.Min(80, arguments.Length)]}", false, ex.Message);
            return string.Empty;
        }
        finally
        {
            KillProcessSafely(process);
            process?.Dispose();
        }
    }

    /// <summary>
    /// Encerra o processo e toda a sua árvore de subprocessos, se ainda estiver em execução.
    /// </summary>
    private static void KillProcessSafely(Process? process)
    {
        if (process == null) return;

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // O processo pode já ter terminado entre a verificação e o Kill.
        }
    }

    private static ProcessStartInfo CreatePowerShellStartInfo(string arguments)
    {
        string command;
        if (arguments.StartsWith("-Command", StringComparison.OrdinalIgnoreCase))
        {
            var commandIndex = arguments.IndexOf(' ');
            command = commandIndex > 0 ? arguments[(commandIndex + 1)..].Trim() : string.Empty;
        }
        else
        {
            command = arguments.Trim();
        }

        if (command.Length >= 2 && command.StartsWith('"') && command.EndsWith('"'))
        {
            command = command[1..^1];
        }

        return new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
    }
}
