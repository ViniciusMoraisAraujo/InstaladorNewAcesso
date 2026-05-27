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

    public async Task<bool> InstallFeatureAsync(WindowsFeature feature)
    {
        var arguments = $"-Command \"Install-WindowsFeature -Name {feature.ServerName} -IncludeAllSubFeature\"";
        return await ProcessExecutor.RunPowerShellCommandAsync(arguments, feature.FriendlyName);
    }
}