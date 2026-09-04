using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Implementations;

public class WindowsTaskInstaller
{
    private readonly IProcessExecutor _executor;

    public WindowsTaskInstaller(IProcessExecutor executor)
    {
        _executor = executor;
    }

    public async Task<bool> InstallTaskAsync(string taskName, string executablePath, string intervalMinutes)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
            UIScope.WriteMessage($"[yellow]Caminho do executável para a task inválido: {executablePath}[/]");
            return false;
        }

        try
        {
            // Apaga se já existir
            await _executor.RunPowerShellCommandAsync($"schtasks.exe /delete /tn \"{taskName}\" /f", $"Remover Tarefa {taskName}");

            var cmd = $"schtasks.exe /create /tn \"{taskName}\" /tr \"\\\"{executablePath}\\\"\" /sc minute /mo {intervalMinutes} /ru \"SYSTEM\" /f";

            var success = await _executor.RunPowerShellCommandAsync(cmd, $"Criar Tarefa {taskName}");
            if (success)
            {
                UIScope.WriteMessage($"[green][[OK]] Tarefa {taskName} criada com sucesso no Windows.[/]");
                return true;
            }
            else
            {
                UIScope.WriteMessage($"[red][[ERRO]] Falha ao criar a tarefa {taskName}.[/]");
                return false;
            }
        }
        catch (Exception ex)
        {
            UIScope.WriteMessage($"[red][[ERRO]] Exceção ao criar a tarefa {taskName}: {ex.Message}[/]");
            return false;
        }
    }
}
