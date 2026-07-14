using System.Xml;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class ConfigHelperBase
{
    /// <summary>
    /// Garante que as seções &lt;configuration&gt; e &lt;appSettings&gt; existam no documento XML
    /// e retorna o elemento &lt;appSettings&gt;.
    /// </summary>
    public static XmlElement EnsureAppSettings(XmlDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        var config = doc.DocumentElement ?? doc.CreateElement("configuration");
        if (doc.DocumentElement == null)
            doc.AppendChild(config);

        var appSettings = config["appSettings"];
        if (appSettings == null)
        {
            appSettings = doc.CreateElement("appSettings");
            config.AppendChild(appSettings);
        }

        return appSettings;
    }

    /// <summary>
    /// Define ou cria uma chave &lt;add key="..." value="..." /&gt; dentro de &lt;appSettings&gt;.
    /// Exibe antes/depois se o valor mudou.
    /// </summary>
    public static void SetKey(XmlElement appSettings, string key, string value)
    {
        ArgumentNullException.ThrowIfNull(appSettings);
        foreach (XmlNode child in appSettings.ChildNodes)
        {
            if (child is XmlElement element &&
                element.Name == "add" &&
                element.GetAttribute("key").Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                var oldValue = element.GetAttribute("value");
                if (oldValue != value)
                {
                    element.SetAttribute("value", value);
                    UIScope.WriteMessage($"   [gray][[INFO]] {MarkupHelper.Escape(key)} atualizado:[/]");
                    UIScope.WriteMessage($"         [red]Antes: {MarkupHelper.Escape(oldValue)}[/]");
                    UIScope.WriteMessage($"         [green]Depois: {MarkupHelper.Escape(value)}[/]");
                }
                else
                {
                    UIScope.WriteMessage($"   [gray][[INFO]] {MarkupHelper.Escape(key)} já está correto.[/]");
                }
                return;
            }
        }

        var addElement = appSettings.OwnerDocument!.CreateElement("add");
        addElement.SetAttribute("key", key);
        addElement.SetAttribute("value", value);
        appSettings.AppendChild(addElement);
        UIScope.WriteMessage($"   [green][[OK]] {MarkupHelper.Escape(key)} adicionado: {MarkupHelper.Escape(value)}[/]");
    }
}
