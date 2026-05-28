using InstaladorNewAcesso.Models;

namespace InstaladorNewAcesso.Configurations;

public class FeatureSetup
{
    public List<WindowsFeature> Features { get; private set; }
    
    public FeatureSetup()
    {
        Features = new List<WindowsFeature>
        {
            new("Extensibilidade .NET 3.5", "Web-Net-Ext", "IIS-NetFxExtensibility"),
            new("Extensibilidade .NET 4.6/4.8", "Web-Net-Ext45", "IIS-NetFxExtensibility45"),
            new("Inicialização de Aplicativos", "Web-App-Init", "IIS-ApplicationInit"),
            new("ASP", "Web-ASP", "IIS-ASP"),
            new("ASP.NET 3.5", "Web-Asp-Net", "IIS-ASPNET"),
            new("ASP.NET 4.5/4.6+", "Web-Asp-Net45", "IIS-ASPNET45"),
            new("Extensões ISAPI", "Web-ISAPI-Ext", "IIS-ISAPIExtensions"),
            new("Filtros ISAPI", "Web-ISAPI-Filter", "IIS-ISAPIFilters"),
            new("Inclusões do Lado do Servidor (SSI)", "Web-Includes", "IIS-ServerSideIncludes"),
            new("Protocolo WebSocket", "Web-WebSockets", "IIS-WebSockets"),
            new("Console de Gerenciamento do IIS", "Web-Mgmt-Console", "IIS-ManagementConsole"),
            new("Compatibilidade Metabase do IIS 6", "Web-Lgcy-Metabase", "IIS-Metabase"),
            new("Console de Gerenciamento do IIS 6", "Web-Lgcy-Mgmt-Console", "IIS-LegacyManagementConsole"),
            new("Ferramentas de Script do IIS 6", "Web-Scripting-Tools", "IIS-LegacyScripts"),
            new("Compatibilidade com WMI do IIS 6", "Web-WMI", "IIS-WMICompatibility"),
            new("Serviço de Gerenciamento", "Web-Mgmt-Service", "IIS-ManagementService"),
            new(".NET Framework 3.5 Core", "NET-Framework-Core", "NetFx3"),
            new("Ativação HTTP (Framework 3.5)", "NET-HTTP-Activation", "WCF-HTTP-Activation"),
            new("Ativação Não-HTTP (Framework 3.5)", "NET-Non-HTTP-Activ", "WCF-NonHTTP-Activation"),
            new("Serviços WCF - Ativação HTTP", "NET-WCF-HTTP-Activation45", "WCF-HTTP-Activation45"),
            new("Ativação de Pipe Nomeado WCF", "NET-WCF-Pipe-Activation45", "WCF-Pipe-Activation45"),
            new("Ativação TCP WCF", "NET-WCF-TCP-Activation45", "WCF-TCP-Activation45"),
            new("Compartilhamento de Porta TCP WCF", "NET-WCF-TCP-PortSharing45", "WCF-TCP-PortSharing45"),
            new("Windows PowerShell 2.0 Engine", "PowerShell-V2", "MicrosoftWindowsPowerShellV2"),
            new("Cliente Telnet", "Telnet-Client", "TelnetClient"),
            new("Cliente TFTP", "TFTP-Client", "TFTP"),
            new("Serviços de Enfileiramento de Mensagens (MSMQ)", "MSMQ-Services", "MSMQ-Container")
        };
    }
}