using InstaladorNewAcesso.Models;

namespace InstaladorNewAcesso.Interfaces;

public interface IFeatureInstaller
{
    Task<bool> IsFeatureInstalledAsync(WindowsFeature feature);
    Task<bool> InstallFeatureAsync(WindowsFeature feature);
}