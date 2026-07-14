namespace InstaladorNewAcesso.Abstractions.Models;

/// <summary>
/// Nomes de tela fortemente tipados para navegação.
/// Use estas constantes em vez de strings mágicas para registrar e navegar entre telas.
/// </summary>
public static class ScreenName
{
    /// <summary>Menu principal</summary>
    public const string MainMenu = "MainMenu";

    /// <summary>Download de instaladores do Google Drive</summary>
    public const string Download = "Download";

    /// <summary>Instalação de Recursos do Windows (IIS, .NET)</summary>
    public const string Resources = "Resources";

    /// <summary>Criação de estrutura de diretórios</summary>
    public const string Directory = "Directory";

    /// <summary>Configuração de Application Pools e Sites no IIS</summary>
    public const string Iis = "Iis";

    /// <summary>Instalação de aplicações via MSI</summary>
    public const string Msi = "Msi";

    /// <summary>Instalação de WebApps (UI e DS)</summary>
    public const string WebApp = "WebApp";

    /// <summary>Edição de agendamento de equipamentos offline</summary>
    public const string Schedule = "Schedule";

    /// <summary>Desinstalação do NewAcesso</summary>
    public const string Uninstall = "Uninstall";

    /// <summary>
    /// Retorna todos os nomes de tela registrados.
    /// </summary>
    public static IReadOnlyList<string> All => new[]
    {
        MainMenu, Download, Resources, Directory, Iis, Msi, WebApp, Schedule, Uninstall
    };

    /// <summary>
    /// Retorna o display name (com emoji) para uma tela.
    /// </summary>
    public static string GetDisplayName(string screenName) => screenName switch
    {
        MainMenu => "🏠 Menu Principal",
        Download => "📥 Download",
        Resources => "🌐 Recursos do Windows",
        Directory => "📂 Diretórios",
        Iis => "⚙️ Configuração IIS",
        Msi => "📦 Aplicações (MSIs)",
        WebApp => "🌍 Web Apps",
        Schedule => "⏰ Agendamento",
        Uninstall => "🗑️ Desinstalação",
        _ => $"🚀 {screenName}"
    };
}
