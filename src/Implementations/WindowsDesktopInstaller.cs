using InstaladorNewAcesso.Interfaces;
using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Utils;

namespace InstaladorNewAcesso.Implementations;

public class WindowsDesktopInstaller : IFeatureInstaller
{
    private readonly IProcessExecutor _executor;

    public WindowsDesktopInstaller() : this(new ProcessExecutorService()) { }

    public WindowsDesktopInstaller(IProcessExecutor executor)
    {
        _executor = executor;
    }


    public async Task<bool> IsFeatureInstalledAsync(WindowsFeature feature)
    {
        var arguments = $"-Command \"(Get-WindowsOptionalFeature -Online -FeatureName {feature.DesktopName}).State\"";
        var output = await _executor.RunPowerShellWithOutputAsync(arguments);
        
        return output.Trim().Equals("Enabled", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> InstallFeatureAsync(WindowsFeature feature, string? sxsPath = null)
    {
        var offlineArgs = string.IsNullOrWhiteSpace(sxsPath) 
            ? "" 
            : $" -Source \"{sxsPath}\" -LimitAccess";

        var command = $"Enable-WindowsOptionalFeature -Online -FeatureName {feature.DesktopName} -All -NoRestart{offlineArgs}";        
        
        return await _executor.RunPowerShellCommandAsync(command, feature.FriendlyName);
    }
}