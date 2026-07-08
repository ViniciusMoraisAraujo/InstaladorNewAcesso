using System.Diagnostics;
using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Utils;
using Spectre.Console;

namespace InstaladorNewAcesso.Services;

public class MsiInstaller
{
    public async Task<bool> InstallAsync(MsiInstallationModel model)
    {
        try
        {
            if (!Directory.Exists(model.TargetDirectory))
                Directory.CreateDirectory(model.TargetDirectory);

            string logArg = "";
            string? logPath = null;
            if (model.GenerateLog)
            {
                logPath = MsiLogHelper.GenerateLogFilePath(model.MsiPath);
                logArg = $" /lvx* \"{logPath}\"";
                AnsiConsole.MarkupLine($"   [gray][LOG] Log verbose: {logPath.EscapeMarkup()}[/]");
            }

            string args = $"/i \"{model.MsiPath}\" /qn TARGETDIR=\"{model.TargetDirectory}\"{logArg}";
            bool success = await RunMsiexecAsync(args);

            if (!success && model.GenerateLog && logPath != null)
                AnalisarLogFalha(logPath);

            if (success)
            {
                ConnectionRecordConfigHelper.UpdateConfigAfterInstall(model.TargetDirectory);
                ControleAcessoConfigHelper.UpdateIniAfterInstall(model.TargetDirectory);
                ControleAcessoAgendamentoHelper.UpdateAgendamentoAfterInstall(model.TargetDirectory);
                CoreWsConfigHelper.UpdateConfigsAfterInstall(model.TargetDirectory);
                TaskConfigHelper.UpdateConfigAfterInstall(model.TargetDirectory);
                StandAloneExConfigHelper.UpdateConfigAfterInstall(model.TargetDirectory);
                StandAloneImConfigHelper.UpdateConfigAfterInstall(model.TargetDirectory);
            }

            return success;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> RunMsiexecAsync(string arguments)
    {
        var process = new Process
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
