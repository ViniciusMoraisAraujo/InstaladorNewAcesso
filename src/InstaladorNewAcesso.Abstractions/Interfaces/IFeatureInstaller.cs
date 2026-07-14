using InstaladorNewAcesso.Abstractions.Models;

namespace InstaladorNewAcesso.Abstractions.Interfaces;

public interface IFeatureInstaller
{
    Task<bool> IsFeatureInstalledAsync(WindowsFeature feature);
    Task<bool> InstallFeatureAsync(WindowsFeature feature, string? sxsPath = null);
    Task<List<(WindowsFeature Feature, bool IsInstalled)>> CheckFeaturesInstalledAsync(List<WindowsFeature> features);

    /// <summary>
    /// Instala múltiplos recursos em paralelo (com limite de concorrência) para acelerar
    /// a instalação dos recursos do Windows.
    /// </summary>
    Task<List<(WindowsFeature Feature, bool Success)>> InstallFeaturesAsync(List<WindowsFeature> features, string? sxsPath = null);
}
