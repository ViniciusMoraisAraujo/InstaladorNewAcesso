using InstaladorNewAcesso.Interfaces;

namespace InstaladorNewAcesso.Utils;

public class IisInstaler : IIisInstaler
{
    public async Task<bool> CreateApplicationPoolAsync(string name, string runtimeVersion, string pipelineMode)
    {
        var command = $"""
                       -Command "
                       Import-Module WebAdministration;
                       New-WebAppPool -Name '{name}';
                       Set-ItemProperty 'IIS:\\AppPools\\{name}' -Name managedRuntimeVersion -Value '{runtimeVersion}';
                       Set-ItemProperty 'IIS:\\AppPools\\{name}' -Name managedPipelineMode -Value '{pipelineMode}'
                       "
                       """;

        return await ProcessExecutor.RunPowerShellCommandAsync(command, $"AppPool: {name}");
    }

    public async Task<bool> CreateSiteAsync(string name, string poolName, string physicalPath, int port)
    {
        var command = $"""
                       -Command "
                       Import-Module WebAdministration;
                       New-Website -Name '{name}' -ApplicationPool '{poolName}' -PhysicalPath '{physicalPath}' -Port {port} -Force
                       "
                       """;

        return await ProcessExecutor.RunPowerShellCommandAsync(command, $"Site: {name}");
    }

    public async Task<bool> SiteExistsAsync(string name)
    {
        var output = await ProcessExecutor.RunPowerShellWithOutputAsync(
            $"-Command \"Import-Module WebAdministration; Test-Path 'IIS:\\Sites\\{name}'\"");
        return output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
    }
    

    public async Task<bool> AppPoolExistsAsync(string name)
    {
        var output = await ProcessExecutor.RunPowerShellWithOutputAsync(
            $"-Command \"Import-Module WebAdministration; Test-Path 'IIS:\\AppPools\\{name}'\"");
        return output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
    }
}