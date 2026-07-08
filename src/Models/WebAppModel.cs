namespace InstaladorNewAcesso.Models;

public class WebAppModel
{
    public string MsiPath { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public string AppPoolName { get; set; } = string.Empty;

    /// <summary>
    /// Caminho onde o MSI realmente instala os arquivos (via Custom Action MSVBDPCADLL).
    /// A DLL MSVBDPCADLL ignora TARGETDIR e usa o physicalPath do IIS.
    /// </summary>
    public string ForcedInstallPath { get; set; } = string.Empty;
    
    public string TargetDirectory { get; set; } = string.Empty;

    public int Port { get; set; }

    /// <summary>
    /// Quando true, gera um log verbose (/lvx*) da instalação para diagnóstico.
    /// O log é salvo em %TEMP%\InstaladorNewAcesso\Logs\
    /// </summary>
    public bool GenerateLog { get; set; }
}