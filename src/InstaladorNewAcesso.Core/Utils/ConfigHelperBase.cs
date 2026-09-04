using System.Xml;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class ConfigHelperBase
{
    /// <summary>
    /// Normaliza o caminho de um diretorio, removendo barras finais para evitar erros em Path.GetDirectoryName.
    /// </summary>
    public static string NormalizeDirectoryPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    /// <summary>
    /// Garante que as secoes &lt;configuration&gt; e &lt;appSettings&gt; existam no documento XML
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
    /// Remove entradas duplicadas da mesma chave e exibe antes/depois se o valor mudou.
    /// </summary>
    public static void SetKey(XmlElement appSettings, string key, string value)
    {
        ArgumentNullException.ThrowIfNull(appSettings);
        XmlElement? primaryElement = null;
        var duplicateElements = new List<XmlElement>();

        foreach (XmlNode child in appSettings.ChildNodes)
        {
            if (child is XmlElement element &&
                element.Name == "add" &&
                element.GetAttribute("key").Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                if (primaryElement == null)
                {
                    primaryElement = element;
                }
                else
                {
                    duplicateElements.Add(element);
                }
            }
        }

        // Remove duplicatas encontradas
        foreach (var dup in duplicateElements)
        {
            appSettings.RemoveChild(dup);
            UIScope.WriteMessage($"   [gray][[INFO]] Chave duplicada removida do XML: {MarkupHelper.Escape(key)}[/]");
        }

        if (primaryElement != null)
        {
            var oldValue = primaryElement.GetAttribute("value");
            if (oldValue != value)
            {
                primaryElement.SetAttribute("value", value);
                UIScope.WriteMessage($"   [gray][[INFO]] {MarkupHelper.Escape(key)} atualizado:[/]");
                UIScope.WriteMessage($"         [red]Antes: {MarkupHelper.Escape(oldValue)}[/]");
                UIScope.WriteMessage($"         [green]Depois: {MarkupHelper.Escape(value)}[/]");
            }
            else
            {
                UIScope.WriteMessage($"   [gray][[INFO]] {MarkupHelper.Escape(key)} ja esta correto.[/]");
            }
            return;
        }

        var addElement = appSettings.OwnerDocument!.CreateElement("add");
        addElement.SetAttribute("key", key);
        addElement.SetAttribute("value", value);
        appSettings.AppendChild(addElement);
        UIScope.WriteMessage($"   [green][[OK]] {MarkupHelper.Escape(key)} adicionado: {MarkupHelper.Escape(value)}[/]");
    }
}