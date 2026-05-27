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

    public async Task<bool> InstallFeatureAsync(WindowsFeature feature)
    {
        var arguments = $"-Command \"Enable-WindowsOptionalFeature -Online -FeatureName {feature.DesktopName} -All -NoRestart\"";
        return await ProcessExecutor.RunPowerShellCommandAsync(arguments, feature.FriendlyName);
    }
}