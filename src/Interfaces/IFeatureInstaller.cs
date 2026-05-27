using InstaladorNewAcesso.Models;

namespace InstaladorNewAcesso.Interfaces;

public interface IFeatureInstaller
{
    Task<bool> InstallFeatureAsync(WindowsFeature feature);
}