namespace InstaladorNewAcesso.Interfaces;

public interface IProcessExecutor
{
    Task<bool> RunPowerShellCommandAsync(string arguments, string featureName);
    Task<string> RunPowerShellWithOutputAsync(string arguments);
}
