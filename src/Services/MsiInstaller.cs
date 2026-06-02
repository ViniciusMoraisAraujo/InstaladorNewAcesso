using System.Diagnostics;
using InstaladorNewAcesso.Models;

namespace InstaladorNewAcesso.Services;

public class MsiInstaller
{
    public async Task<bool> InstallAsync(MsiInstallationModel model)
    {
        try
        {
            if (!Directory.Exists(model.TargetDirectory))
                Directory.CreateDirectory(model.TargetDirectory);

            string args = $"/i \"{model.MsiPath}\" /qn TARGETDIR=\"{model.TargetDirectory}\"";
            return await RunMsiexecAsync(args);
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
}