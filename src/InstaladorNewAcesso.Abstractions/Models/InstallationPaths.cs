namespace InstaladorNewAcesso.Abstractions.Models;

public class InstallationPaths
{
    public string BasePath { get; private set; }
    public string InstallationPath => Path.Combine(BasePath, "Instaladores");
    public string NewAcessoRoot => Path.Combine(BasePath, "NewAcesso");

    public string AutoAtendimento => Path.Combine(NewAcessoRoot, "AutoAtendimento");
    public string ConexBridge => Path.Combine(NewAcessoRoot, "ConexBridge");
    public string ConnectionRecord => Path.Combine(NewAcessoRoot, "ConnectionRecord");
    public string Controller => Path.Combine(NewAcessoRoot, "Controller");
    public string ControllerOffline => Path.Combine(NewAcessoRoot, "ControllerOffline");
    public string VisitAuthorization => Path.Combine(NewAcessoRoot, "VisitAuthorization");
    public string Win => Path.Combine(NewAcessoRoot, "Win");

    public string ControleAcesso => Path.Combine(Controller, "ControleAcesso");
    public string CoreWs => Path.Combine(Controller, "CoreWs");
    public string Fabricantes => Path.Combine(Controller, "Fabricantes");
    public string Task => Path.Combine(Controller, "Task");

    public string ControllerOfflineArquivos => Path.Combine(ControllerOffline, "Arquivos");
    public string ControllerOfflineWinServiceEx => Path.Combine(ControllerOffline, "WinService_Ex");
    public string ControllerOfflineWinServiceIn => Path.Combine(ControllerOffline, "WinService_In");
    public string WebAppDS => Path.Combine(NewAcessoRoot, "WebAppDS");
    public string WebAppUI => Path.Combine(NewAcessoRoot, "WebAppUI");
    public string WebAppUIFabricantes => Path.Combine(WebAppUI, "Fabricantes");
    public InstallationPaths(string basePath)
    {
        BasePath = basePath;
    }

    public IEnumerable<string> GetBaseFolders() =>
    [
        AutoAtendimento,
        ConexBridge,
        ConnectionRecord,
        Controller,
        ControllerOffline,
        VisitAuthorization,
        WebAppDS,
        WebAppUI,
        Win
    ];
}
