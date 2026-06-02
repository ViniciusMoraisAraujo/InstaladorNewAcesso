namespace InstaladorNewAcesso.Models;

public class WebAppModel
{
    public string MsiPath { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public string AppPoolName { get; set; } = string.Empty;

    [Obsolete("Não utilizado na estratégia atual. A DLL MSVBDPCADLL instala no physicalPath do IIS.")]
    public string ForcedInstallPath { get; set; } = string.Empty;
    
    public string TargetDirectory { get; set; } = string.Empty;

    public int Port { get; set; }
}