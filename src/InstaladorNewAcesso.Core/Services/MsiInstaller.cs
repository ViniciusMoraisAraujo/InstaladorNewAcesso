using System.Diagnostics;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Core.Services;

public class MsiInstaller
{
    public async Task<bool> InstallAsync(MsiInstallationModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        try
        {
            if (!Directory.Exists(model.TargetDirectory))
                Directory.CreateDirectory(model.TargetDirectory);

            var logArg = "";
            string? logPath = null;
            if (model.GenerateLog)
            {
                logPath = MsiLogHelper.GenerateLogFilePath(model.MsiPath);
                logArg = $" /lvx* \"{logPath}\"";
                UIScope.WriteMessage($"   [gray][[LOG]] Log verbose: {MarkupHelper.Escape(logPath)}[/]");
            }

            var args = $"/i \"{model.MsiPath}\" /qn TARGETDIR=\"{model.TargetDirectory}\"{logArg}";
            var success = await RunMsiexecAsync(args);

            if (!success && model.GenerateLog && logPath != null)
                AnalisarLogFalha(logPath);

            if (success)
            {
                var configDir = model.TargetDirectory;

                // Só chama cada config helper se o arquivo de configuração correspondente existir
                if (File.Exists(Path.Combine(configDir, "PrimeAcesso.ConnectionRecord.exe.config")))
                    ConnectionRecordConfigHelper.UpdateConfigAfterInstall(configDir);

                if (File.Exists(Path.Combine(configDir, "PrimeAcesso.ControleAcesso.ini")))
                {
                    ControleAcessoConfigHelper.UpdateIniAfterInstall(configDir);
                    ControleAcessoAgendamentoHelper.UpdateAgendamentoAfterInstall(configDir);
                }

                if (File.Exists(Path.Combine(configDir, "NewAcesso.Controlador.Watchdog.exe.config")) ||
                    File.Exists(Path.Combine(configDir, "NewAcesso.Controlador.Ws.exe.config")))
                    CoreWsConfigHelper.UpdateConfigsAfterInstall(configDir);

                if (File.Exists(Path.Combine(configDir, "PrimeAcesso.Task.exe.config")))
                    TaskConfigHelper.UpdateConfigAfterInstall(configDir);

                if (File.Exists(Path.Combine(configDir, "PrimeAcesso.Controller.StandAloneEx.exe.config")))
                    StandAloneExConfigHelper.UpdateConfigAfterInstall(configDir);

                if (File.Exists(Path.Combine(configDir, "PrimeAcesso.Controller.StandAloneIm.exe.config")))
                    StandAloneImConfigHelper.UpdateConfigAfterInstall(configDir);
            }

            return success;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Executa msiexec.exe com os argumentos fornecidos.
    /// Protected virtual para permitir mock em testes de unidade.
    /// </summary>
    protected virtual async Task<bool> RunMsiexecAsync(string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "msiexec.exe",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        return process.ExitCode == 0;
    }

    private static void AnalisarLogFalha(string logPath)
    {
        MsiLogHelper.DisplayLogAnalysis(logPath);
    }
}
