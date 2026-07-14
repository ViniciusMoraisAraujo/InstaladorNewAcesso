using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Core.Implementations;

public class WindowsDesktopInstaller : IFeatureInstaller
{
    private readonly IProcessExecutor _executor;

    // Nível de concorrência para instalação paralela de recursos.
    // Aumentar acelera a instalação, mas consome mais CPU/disco.
    private const int MaxConcurrency = 4;

    public WindowsDesktopInstaller() : this(new ProcessExecutorService()) { }

    public WindowsDesktopInstaller(IProcessExecutor executor)
    {
        _executor = executor;
    }


    public async Task<bool> IsFeatureInstalledAsync(WindowsFeature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        var arguments = $"-Command \"(Get-WindowsOptionalFeature -Online -FeatureName {feature.DesktopName}).State\"";
        var output = await _executor.RunPowerShellWithOutputAsync(arguments);

        return output.Trim().Equals("Enabled", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<(WindowsFeature Feature, bool IsInstalled)>> CheckFeaturesInstalledAsync(List<WindowsFeature> features)
    {
        var names = string.Join(",", features.Select(f => $"'{f.DesktopName}'"));
        var command = $"-Command \"$n = @({names}); Get-WindowsOptionalFeature -Online | Where-Object {{ $n -contains $_.FeatureName }} | ForEach-Object {{ $_.FeatureName + '|' + $_.State }}\"";

        var output = await _executor.RunPowerShellWithOutputAsync(command);

        var states = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(output))
        {
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Trim().Split('|');
                if (parts.Length == 2)
                    states[parts[0].Trim()] = parts[1].Trim().Equals("Enabled", StringComparison.OrdinalIgnoreCase);
            }
        }

        return features.Select(f => (f, states.GetValueOrDefault(f.DesktopName, false))).ToList();
    }

    public async Task<bool> InstallFeatureAsync(WindowsFeature feature, string? sxsPath = null)
    {
        ArgumentNullException.ThrowIfNull(feature);
        var offlineArgs = string.IsNullOrWhiteSpace(sxsPath)
            ? ""
            : $" -Source \"{sxsPath}\" -LimitAccess";

        var command = $"Enable-WindowsOptionalFeature -Online -FeatureName {feature.DesktopName} -All -NoRestart{offlineArgs}";

        return await _executor.RunPowerShellCommandAsync(command, feature.FriendlyName);
    }

    public async Task<List<(WindowsFeature Feature, bool Success)>> InstallFeaturesAsync(List<WindowsFeature> features, string? sxsPath = null)
    {
        ArgumentNullException.ThrowIfNull(features);

        var results = new List<(WindowsFeature Feature, bool Success)>(features.Count);
        var sync = new object();

        await Parallel.ForEachAsync(
            features,
            new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrency },
            async (feature, ct) =>
            {
                var ok = await InstallFeatureAsync(feature, sxsPath);
                lock (sync)
                {
                    results.Add((feature, ok));
                }
            });

        return results;
    }
}
