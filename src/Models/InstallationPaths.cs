namespace InstaladorNewAcesso.Models;

public class InstallationPaths
{
    public string BasePath { get; private set; }
    public string InstallationPath => Path.Combine(BasePath, "Instaladores");
    public string NewAcessoRoot => Path.Combine(BasePath, "NewAcesso");
    public string AutoAtendimento => Path.Combine(NewAcessoRoot, "AutoAtendimento");
    public string ConnectionRecord => Path.Combine(NewAcessoRoot, "ConnectionRecord");
    public string Controller => Path.Combine(NewAcessoRoot, "Controller");
    public string ControllerOffline => Path.Combine(NewAcessoRoot, "ControllerOffline");
    public string VisitAuthorization => Path.Combine(NewAcessoRoot, "VisitAuthorization");
    public string WebAppDS => Path.Combine(NewAcessoRoot, "WebAppDS");
    public string WebAppUI => Path.Combine(NewAcessoRoot, "WebAppUI");
    public string Win => Path.Combine(NewAcessoRoot, "Win");
    public string Manufacturers => Path.Combine(Controller, "Fabricantes");
    public InstallationPaths(string basePath)
    {
        BasePath = basePath;
    }
   
    public IEnumerable<string> GetBaseFolders() =>
    [
        AutoAtendimento,
        ConnectionRecord,
        VisitAuthorization,
        WebAppDS,
        WebAppUI,
        Win
    ];
}