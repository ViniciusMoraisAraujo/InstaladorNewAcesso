namespace InstaladorNewAcesso.Abstractions.Interfaces;

public interface IIisInstaller
{
    Task<bool> CreateApplicationPoolAsync(string name, string runtimeVersion, string pipelineMode);
    Task<bool> CreateSiteAsync(string name, string poolName, string physicalPath, int port);
    Task<bool> SiteExistsAsync(string name);
    Task<bool> AppPoolExistsAsync(string name);
    Task<bool> UpdateSitePhysicalPathAsync(string siteName, string newPhysicalPath);
    Task<Dictionary<string, bool>> CheckAppPoolsExistAsync(string[] names);
    Task<Dictionary<string, bool>> CheckSitesExistAsync(string[] names);
    Task<(Dictionary<string, bool> AppPools, Dictionary<string, bool> Sites)> CheckAppPoolsAndSitesExistAsync(string[] poolNames, string[] siteNames);
    Task<bool> RemoveSiteAsync(string name);
    Task<bool> RemoveAppPoolAsync(string name);
}
