using InstaladorNewAcesso.Interfaces;
using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Utils;

namespace InstaladorNewAcesso.Implementations;

public class WindowsDesktopInstaller : IFeatureInstaller
{
    public async Task<bool> IsFeatureInstalledAsync(WindowsFeature feature)
    {
        var arguments = $"-Command \"(Get-WindowsOptionalFeature -Online -FeatureName {feature.DesktopName}).State\"";
        var output = await ProcessExecutor.RunPowerShellWithOutputAsync(arguments);
        
        return output.Trim().Equals("Enabled", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> InstallFeatureAsync(WindowsFeature feature, string? sxsPath = null)
    {
        var offlineArgs = string.IsNullOrWhiteSpace(sxsPath) 
            ? "" 
            : $" -Source \"{sxsPath}\" -LimitAccess";

        var command = $"Enable-WindowsOptionalFeature -Online -FeatureName {feature.DesktopName} -All -NoRestart{offlineArgs}";        
        
        return await ProcessExecutor.RunPowerShellCommandAsync(command, feature.FriendlyName);
    }
}