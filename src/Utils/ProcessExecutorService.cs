using InstaladorNewAcesso.Interfaces;

namespace InstaladorNewAcesso.Utils;

/// <summary>
/// Implementação de IProcessExecutor que delega para o ProcessExecutor estático original.
/// Esta camada permite injeção de dependência e testes com mocks.
/// </summary>
public class ProcessExecutorService : IProcessExecutor
{
    public Task<bool> RunPowerShellCommandAsync(string arguments, string featureName)
    {
        return ProcessExecutor.RunPowerShellCommandAsync(arguments, featureName);
    }

    public Task<string> RunPowerShellWithOutputAsync(string arguments)
    {
        return ProcessExecutor.RunPowerShellWithOutputAsync(arguments);
    }
}
