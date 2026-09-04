namespace InstaladorNewAcesso.Abstractions.Models;

public class UnattendedConfig
{
    public string BasePath { get; set; } = "C:\\SoftPrime";
    public string InstallersPath { get; set; } = "C:\\SoftPrime\\Installers";
    
    public bool InstallWindowsFeatures { get; set; } = true;
    public bool CreateDirectories { get; set; } = true;
    public bool ConfigureIIS { get; set; } = true;
    
    /// <summary>
    /// Lista de nomes de MSIs a instalar (ex: "PrimeAcesso.msi"). 
    /// Se estiver vazio ou contiver "*", tenta instalar todos os encontrados.
    /// </summary>
    public List<string> MsisToInstall { get; set; } = new();
    
    public bool InstallWebApps { get; set; } = true;
    
    public DatabaseConfig Database { get; set; } = new();
    
    public TaskSchedulerConfig TaskScheduler { get; set; } = new();
}

public class DatabaseConfig
{
    public string Server { get; set; } = "localhost";
    public string User { get; set; } = "sa";
    public string Password { get; set; } = "masterkey";
}

public class TaskSchedulerConfig
{
    public bool Install { get; set; }
    public string TaskName { get; set; } = "NewAcessoTask";
    public string ExecutablePath { get; set; } = "";
    public string IntervalMinutes { get; set; } = "5";
}
