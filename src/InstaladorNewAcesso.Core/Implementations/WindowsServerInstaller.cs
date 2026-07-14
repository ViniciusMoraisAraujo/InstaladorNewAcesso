using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Core.Implementations;

public class WindowsServerInstaller : IFeatureInstaller
{
    private readonly IProcessExecutor _executor;

    // Nível de concorrência para instalação paralela de recursos.
    // Aumentar acelera a instalação, mas consome mais CPU/disco.
    private const int MaxConcurrency = 4;

    public WindowsServerInstaller() : this(new ProcessExecutorService()) { }

    public WindowsServerInstaller(IProcessExecutor executor)
    {
        _executor = executor;
    }


    public async Task<bool> IsFeatureInstalledAsync(WindowsFeature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        var arguments = $"-Command \"(Get-WindowsFeature -Name {feature.ServerName}).Installed\"";
        var output = await _executor.RunPowerShellWithOutputAsync(arguments);

        return output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<List<(WindowsFeature Feature, bool IsInstalled)>> CheckFeaturesInstalledAsync(List<WindowsFeature> features)
    {
        var names = string.Join(",", features.Select(f => $"'{f.ServerName}'"));
        var command = $"-Command \"$n = @({names}); Get-WindowsFeature | Where-Object {{ $n -contains $_.Name }} | ForEach-Object {{ $_.Name + '|' + $_.Installed }}\"";

        var output = await _executor.RunPowerShellWithOutputAsync(command);

        var states = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(output))
        {
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Trim().Split('|');
                if (parts.Length == 2)
                    states[parts[0].Trim()] = parts[1].Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
            }
        }

        return features.Select(f => (f, states.GetValueOrDefault(f.ServerName, false))).ToList();
    }

    public async Task<bool> InstallFeatureAsync(WindowsFeature feature, string? sxsPath = null)
    {
        ArgumentNullException.ThrowIfNull(feature);
        var offlineArgs = string.IsNullOrWhiteSpace(sxsPath)
            ? ""
            : $" -Source \"{sxsPath}\"";

        var command = $"Install-WindowsFeature -Name {feature.ServerName}{offlineArgs}";

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
