using System.Xml;
using Spectre.Console;

namespace InstaladorNewAcesso.Utils;

public static class ConfigHelperBase
{
    /// <summary>
    /// Garante que as seções &lt;configuration&gt; e &lt;appSettings&gt; existam no documento XML
    /// e retorna o elemento &lt;appSettings&gt;.
    /// </summary>
    public static XmlElement EnsureAppSettings(XmlDocument doc)
    {
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
                    AnsiConsole.MarkupLine($"   [gray][[INFO]] {key.EscapeMarkup()} atualizado:[/]");
                    AnsiConsole.MarkupLine($"         [red]Antes: {oldValue.EscapeMarkup()}[/]");
                    AnsiConsole.MarkupLine($"         [green]Depois: {value.EscapeMarkup()}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"   [gray][[INFO]] {key.EscapeMarkup()} já está correto.[/]");
                }
                return;
            }
        }

        var addElement = appSettings.OwnerDocument!.CreateElement("add");
        addElement.SetAttribute("key", key);
        addElement.SetAttribute("value", value);
        appSettings.AppendChild(addElement);
        AnsiConsole.MarkupLine($"   [green][[OK]] {key.EscapeMarkup()} adicionado: {value.EscapeMarkup()}[/]");
    }
}
