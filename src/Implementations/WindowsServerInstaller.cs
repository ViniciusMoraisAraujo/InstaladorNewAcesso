using InstaladorNewAcesso.Interfaces;
using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Utils;

namespace InstaladorNewAcesso.Implementations;

public class WindowsServerInstaller : IFeatureInstaller
{
    public async Task<bool> IsFeatureInstalledAsync(WindowsFeature feature)
    {
        var arguments = $"-Command \"(Get-WindowsFeature -Name {feature.ServerName}).Installed\"";
        var output = await ProcessExecutor.RunPowerShellWithOutputAsync(arguments);
        
        return output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> InstallFeatureAsync(WindowsFeature feature, string? sxsPath = null)
    {
        var offlineArgs = string.IsNullOrWhiteSpace(sxsPath) 
            ? "" 
            : $" -Source \"{sxsPath}\"";

        var command = $"Install-WindowsFeature -Name {feature.ServerName}{offlineArgs}";       
        
        return await ProcessExecutor.RunPowerShellCommandAsync(command, feature.FriendlyName);
    }
}