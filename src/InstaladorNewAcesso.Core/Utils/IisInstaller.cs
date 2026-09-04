using InstaladorNewAcesso.Abstractions.Interfaces;

namespace InstaladorNewAcesso.Core.Utils;

public class IisInstaller : IIisInstaller
{
    private readonly IProcessExecutor _executor;

    public IisInstaller() : this(new ProcessExecutorService()) { }

    public IisInstaller(IProcessExecutor executor)
    {
        _executor = executor;
    }


    public async Task<bool> CreateApplicationPoolAsync(string name, string runtimeVersion, string pipelineMode)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("-Command ").Append('"');
        sb.Append("Import-Module WebAdministration; ");
        sb.Append("New-WebAppPool -Name '").Append(name).Append('\'').Append("; ");
        sb.Append("Set-ItemProperty 'IIS:\\AppPools\\").Append(name).Append("' -Name managedRuntimeVersion -Value '").Append(runtimeVersion).Append("'; ");
        sb.Append("Set-ItemProperty 'IIS:\\AppPools\\").Append(name).Append("' -Name managedPipelineMode -Value '").Append(pipelineMode).Append('\'');
        sb.Append('"');
        var command = sb.ToString();

        return await _executor.RunPowerShellCommandAsync(command, $"AppPool: {name}");
    }

    public async Task<bool> CreateSiteAsync(string name, string poolName, string physicalPath, int port)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("-Command ").Append('"');
        sb.Append("Import-Module WebAdministration; ");
        sb.Append("New-Website -Name '").Append(name).Append("' -ApplicationPool '").Append(poolName).Append('\'').Append(' ');
        sb.Append("-PhysicalPath '").Append(physicalPath).Append("' -Port ").Append(port).Append(" -Force");
        sb.Append('"');
        var command = sb.ToString();

        return await _executor.RunPowerShellCommandAsync(command, $"Site: {name}");
    }

    public async Task<bool> SiteExistsAsync(string name)
    {
        var cmd = "-Command \"Import-Module WebAdministration; Test-Path 'IIS:\\Sites\\" + name + "'\"";
        var output = await _executor.RunPowerShellWithOutputAsync(cmd);
        return output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
    }


    public async Task<bool> AppPoolExistsAsync(string name)
    {
        var cmd = "-Command \"Import-Module WebAdministration; Test-Path 'IIS:\\AppPools\\" + name + "'\"";
        var output = await _executor.RunPowerShellWithOutputAsync(cmd);
        return output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> UpdateSitePhysicalPathAsync(string siteName, string newPhysicalPath)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("-Command ").Append('"');
        sb.Append("Import-Module WebAdministration; ");
        sb.Append("Set-ItemProperty 'IIS:\\Sites\\").Append(siteName).Append("' -Name physicalPath -Value '").Append(newPhysicalPath).Append('\'');
        sb.Append('"');
        var command = sb.ToString();

        return await _executor.RunPowerShellCommandAsync(command, $"Atualizar PhysicalPath: {siteName} -> {newPhysicalPath}");
    }

    public async Task<Dictionary<string, bool>> CheckAppPoolsExistAsync(string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);
        if (names.Length == 0)
            return new Dictionary<string, bool>();

        var namesList = string.Join("','", names);
        var cmd = "-Command \"Import-Module WebAdministration; '" + namesList + "' | ForEach-Object { $_ + '|' + (Test-Path ('IIS:\\AppPools\\' + $_)) }\"";

        var output = await _executor.RunPowerShellWithOutputAsync(cmd);
        return ParseBoolResults(output, names);
    }

    public async Task<Dictionary<string, bool>> CheckSitesExistAsync(string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);
        if (names.Length == 0)
            return new Dictionary<string, bool>();

        var namesList = string.Join("','", names);
        var cmd = "-Command \"Import-Module WebAdministration; '" + namesList + "' | ForEach-Object { $_ + '|' + (Test-Path ('IIS:\\Sites\\' + $_)) }\"";

        var output = await _executor.RunPowerShellWithOutputAsync(cmd);
        return ParseBoolResults(output, names);
    }

    /// <summary>
    /// Verifica AppPools e Sites em um único comando PowerShell,
    /// reduzindo o overhead de spawning múltiplos processos.
    /// Output: "POOL:Nome|True|SITE:Nome|False\n..."
    /// </summary>
    public async Task<(Dictionary<string, bool> AppPools, Dictionary<string, bool> Sites)> CheckAppPoolsAndSitesExistAsync(
        string[] poolNames, string[] siteNames)
    {
        ArgumentNullException.ThrowIfNull(poolNames);
        ArgumentNullException.ThrowIfNull(siteNames);
        var poolsResult = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var sitesResult = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        if (poolNames.Length == 0 && siteNames.Length == 0)
            return (poolsResult, sitesResult);

        var parts = new List<string>();

        if (poolNames.Length > 0)
        {
            var poolList = string.Join("','", poolNames);
            parts.Add("'" + poolList + "' | ForEach-Object { 'POOL:' + $_ + '|' + (Test-Path ('IIS:\\AppPools\\' + $_)) }");
        }

        if (siteNames.Length > 0)
        {
            var siteList = string.Join("','", siteNames);
            parts.Add("'" + siteList + "' | ForEach-Object { 'SITE:' + $_ + '|' + (Test-Path ('IIS:\\Sites\\' + $_)) }");
        }

        var commandsJoined = string.Join("; ", parts);
        var command = "-Command \"Import-Module WebAdministration; " + commandsJoined + "\"";

        var output = await _executor.RunPowerShellWithOutputAsync(command);

        if (!string.IsNullOrWhiteSpace(output))
        {
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                var parts2 = trimmed.Split('|');
                if (parts2.Length != 2) continue;

                var namePart = parts2[0].Trim();
                var value = parts2[1].Trim().Equals("True", StringComparison.OrdinalIgnoreCase);

                if (namePart.StartsWith("POOL:", StringComparison.OrdinalIgnoreCase))
                    poolsResult[namePart[5..]] = value;
                else if (namePart.StartsWith("SITE:", StringComparison.OrdinalIgnoreCase))
                    sitesResult[namePart[5..]] = value;
            }
        }

        foreach (var name in poolNames)
            poolsResult.TryAdd(name, false);
        foreach (var name in siteNames)
            sitesResult.TryAdd(name, false);

        return (poolsResult, sitesResult);
    }

    private static Dictionary<string, bool> ParseBoolResults(string output, string[] expectedNames)
    {
        var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(output))
        {
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Trim().Split('|');
                if (parts.Length == 2)
                    result[parts[0].Trim()] = parts[1].Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
            }
        }

        foreach (var name in expectedNames)
        {
            if (!result.ContainsKey(name))
                result[name] = false;
        }

        return result;
    }

    public async Task<bool> GrantDirectoryPermissionsAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var cmd = $"-Command \"icacls '{path}' /grant 'IIS_IUSRS:(OI)(CI)M' 'IUSR:(OI)(CI)RX' /t /c /q\"";
        return await _executor.RunPowerShellCommandAsync(cmd, $"Permissões NTFS: {path}");
    }

    public async Task<bool> RemoveSiteAsync(string name)
    {
        var cmd = "-Command \"Import-Module WebAdministration; Remove-Website -Name '" + name + "' -Confirm:$false; if ($?) { 'OK' } else { throw }\"";
        var output = await _executor.RunPowerShellWithOutputAsync(cmd);
        return output.Trim().Equals("OK", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> RemoveAppPoolAsync(string name)
    {
        var cmd = "-Command \"Import-Module WebAdministration; Remove-WebAppPool -Name '" + name + "' -Confirm:$false; if ($?) { 'OK' } else { throw }\"";
        var output = await _executor.RunPowerShellWithOutputAsync(cmd);
        return output.Trim().Equals("OK", StringComparison.OrdinalIgnoreCase);
    }
}
