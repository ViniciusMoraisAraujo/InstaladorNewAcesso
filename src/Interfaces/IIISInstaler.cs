namespace InstaladorNewAcesso.Interfaces;

public interface IIISInstaler
{
    Task<bool> CreateApplicationPoolAsync(string name, string runtimeVersion, string pipelineMode);
    Task<bool> CreateSiteAsync(string name, string poolName, string physicalPath, int port);
    Task<bool> SiteExistsAsync(string name);
    Task<bool> AppPoolExistsAsync(string name);
}