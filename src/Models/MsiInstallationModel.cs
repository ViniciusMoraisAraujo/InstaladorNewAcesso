namespace InstaladorNewAcesso.Models;

public class MsiInstallationModel
{
    public string MsiPath { get; set; } = "";
    public string TargetDirectory { get; set; } = "";
    public bool IsWebApp { get; set; }
    public string? SiteName { get; set; }
    public string? AppPoolName { get; set; }
    public string? ExtraArgs { get; set; }
}