using InstaladorNewAcesso.Interfaces;
using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Utils;

namespace InstaladorNewAcesso.Implementations;

public class WindowsServerInstaller : IFeatureInstaller
{
    public async Task<bool> InstallFeatureAsync(WindowsFeature feature)
    {
        string arguments = $"-Command \"Install-WindowsFeature -Name {feature.ServerName} -IncludeAllSubFeature\"";
        return await ProcessExecutor.RunPowerShellCommandAsync(arguments, feature.FriendlyName);
    }
}