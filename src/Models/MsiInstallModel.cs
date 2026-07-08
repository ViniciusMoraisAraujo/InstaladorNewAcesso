namespace InstaladorNewAcesso.Models;

public class MsiInstallationModel
{
    public string MsiPath { get; set; } = string.Empty;
    public string TargetDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Quando true, gera um log verbose (/lvx*) da instalação para diagnóstico.
    /// O log é salvo em %TEMP%\InstaladorNewAcesso\Logs\
    /// </summary>
    public bool GenerateLog { get; set; }
}