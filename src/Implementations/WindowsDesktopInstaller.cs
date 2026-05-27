using InstaladorNewAcesso.Interfaces;
using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Utils;

namespace InstaladorNewAcesso.Implementations;

public class WindowsDesktopInstaller : IFeatureInstaller
{
    public async Task<bool> InstallFeatureAsync(WindowsFeature feature)
    {
        string arguments = $"-Command \"Enable-WindowsOptionalFeature -Online -FeatureName {feature.DesktopName} -All -NoRestart\"";
        return await ProcessExecutor.RunPowerShellCommandAsync(arguments, feature.FriendlyName);
    }
}