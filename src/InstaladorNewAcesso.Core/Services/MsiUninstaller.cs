using System.Diagnostics;
using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Core.Services;

public class MsiUninstaller
{
    private readonly IProcessExecutor _executor;

    public MsiUninstaller() : this(new ProcessExecutorService()) { }

    public MsiUninstaller(IProcessExecutor executor)
    {
        _executor = executor;
    }


    /// <summary>
    /// Verifica se um diretório de instalação existe e contém arquivos.
    /// </summary>
    public static bool IsInstalled(string targetDirectory)
    {
        if (!Directory.Exists(targetDirectory))
            return false;

        return Directory.GetFileSystemEntries(targetDirectory).Length > 0;
    }

    /// <summary>
    /// Verifica se o produto MSI ainda está registrado no Windows Installer,
    /// mesmo que o diretório de instalação tenha sido removido.
    /// Consulta o registro do Windows em busca de produtos cujo InstallLocation
    /// corresponda ao diretório alvo.
    /// </summary>
    public async Task<bool> IsRegisteredAsync(string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(targetDirectory))
            return false;

        // Em strings PowerShell com aspas simples, backslashes são literais.
        // Não é necessário escapar — C:\NewAcesso continua C:\NewAcesso.
        var command = $"-Command \"& {{ Get-ChildItem 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall' -Recurse -ErrorAction SilentlyContinue | Get-ItemProperty | Where-Object {{ $_.InstallLocation -like '{targetDirectory}*' }} | Select-Object -First 1 }}\"";

        var output = await _executor.RunPowerShellWithOutputAsync(command);

        // Se encontrou alguma propriedade, o produto está registrado
        return !string.IsNullOrWhiteSpace(output);
    }

    /// <summary>
    /// Desinstala um MSI usando o arquivo .msi original.
    /// Executa: msiexec /x "caminho" /qn
    /// </summary>
    public static async Task<bool> UninstallByMsiPathAsync(string msiPath)
    {
        try
        {
            var args = $"/x \"{msiPath}\" /qn";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "msiexec.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Remove diretório de instalação, se existir.
    /// </summary>
    public static bool RemoveTargetDirectory(string targetDirectory)
    {
        try
        {
            if (Directory.Exists(targetDirectory))
            {
                Directory.Delete(targetDirectory, true);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}
