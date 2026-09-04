using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class IniHelperBase
{
    /// <summary>
    /// Define ou cria uma chave key = value em um arquivo .INI.
    /// Se <paramref name="section"/> for fornecido, a chave sera inserida/atualizada
    /// estritamente dentro da secao correspondente. Se a secao nao existir, ela sera criada.
    /// Retorna true se o arquivo foi modificado.
    /// </summary>
    public static bool SetIniKey(List<string> lines, string key, string value, bool useQuotes = true, string? section = null)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var newLine = useQuotes ? $"{key} = '{value}'" : $"{key} = {value}";

        if (section == null)
        {
            // Busca a chave em qualquer lugar
            for (var i = 0; i < lines.Count; i++)
            {
                var trimmed = lines[i].Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(';') || trimmed.StartsWith('#'))
                    continue;

                var equalsPos = trimmed.IndexOf('=');
                if (equalsPos <= 0) continue;

                var lineKey = trimmed[..equalsPos].Trim();
                if (lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    var currentValue = trimmed[(equalsPos + 1)..].Trim().Trim('\'', '"');
                    if (!currentValue.Equals(value, StringComparison.OrdinalIgnoreCase))
                    {
                        var oldValueDisplay = currentValue;
                        lines[i] = newLine;
                        UIScope.WriteMessage($"[gray]   [[INFO]] {MarkupHelper.Escape(key)} atualizado:[/]");
                        UIScope.WriteMessage($"         [red]Antes: {MarkupHelper.Escape(oldValueDisplay)}[/]");
                        UIScope.WriteMessage($"         [green]Depois: {MarkupHelper.Escape(value)}[/]");
                        return true;
                    }
                    UIScope.WriteMessage($"[gray]   [[INFO]] {MarkupHelper.Escape(key)} ja esta correto.[/]");
                    return false;
                }
            }

            // Nao encontrou, adiciona ao final
            if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
                lines.Add(string.Empty);

            lines.Add(newLine);
            UIScope.WriteMessage($"[green]   [[OK]] {MarkupHelper.Escape(key)} adicionado: {MarkupHelper.Escape(value)}[/]");
            return true;
        }

        // Caso com secao especificada
        var sectionHeaderIndex = -1;
        var nextSectionHeaderIndex = -1;

        for (var i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                var currentSection = trimmed[1..^1].Trim();
                if (sectionHeaderIndex == -1)
                {
                    if (currentSection.Equals(section, StringComparison.OrdinalIgnoreCase))
                    {
                        sectionHeaderIndex = i;
                    }
                }
                else
                {
                    nextSectionHeaderIndex = i;
                    break;
                }
            }
        }

        if (sectionHeaderIndex != -1)
        {
            // Secao encontrada: procura chave dentro do bloco da secao
            var endOfSection = nextSectionHeaderIndex != -1 ? nextSectionHeaderIndex : lines.Count;

            for (var i = sectionHeaderIndex + 1; i < endOfSection; i++)
            {
                var trimmed = lines[i].Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(';') || trimmed.StartsWith('#'))
                    continue;

                var equalsPos = trimmed.IndexOf('=');
                if (equalsPos <= 0) continue;

                var lineKey = trimmed[..equalsPos].Trim();
                if (lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    var currentValue = trimmed[(equalsPos + 1)..].Trim().Trim('\'', '"');
                    if (!currentValue.Equals(value, StringComparison.OrdinalIgnoreCase))
                    {
                        var oldValueDisplay = currentValue;
                        lines[i] = newLine;
                        UIScope.WriteMessage($"[gray]   [[INFO]] {MarkupHelper.Escape(key)} atualizado:[/]");
                        UIScope.WriteMessage($"         [red]Antes: {MarkupHelper.Escape(oldValueDisplay)}[/]");
                        UIScope.WriteMessage($"         [green]Depois: {MarkupHelper.Escape(value)}[/]");
                        return true;
                    }
                    UIScope.WriteMessage($"[gray]   [[INFO]] {MarkupHelper.Escape(key)} ja esta correto.[/]");
                    return false;
                }
            }

            // Chave nao encontrada na secao: insere no final da secao (antes da proxima secao)
            var insertIndex = nextSectionHeaderIndex != -1 ? nextSectionHeaderIndex : lines.Count;
            lines.Insert(insertIndex, newLine);
            UIScope.WriteMessage($"[green]   [[OK]] {MarkupHelper.Escape(key)} adicionado na secao [{MarkupHelper.Escape(section)}]: {MarkupHelper.Escape(value)}[/]");
            return true;
        }

        // Secao nao encontrada: cria a secao e adiciona a chave
        if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
            lines.Add(string.Empty);

        lines.Add($"[{section}]");
        lines.Add(newLine);
        UIScope.WriteMessage($"[green]   [[OK]] Secao [{MarkupHelper.Escape(section)}] criada com a chave {MarkupHelper.Escape(key)}: {MarkupHelper.Escape(value)}[/]");
        return true;
    }

    /// <summary>
    /// Atualiza uma chave apenas se ela ja existir no arquivo .INI (dentro da secao informada ou em qualquer lugar).
    /// Nao adiciona a chave caso ela nao exista.
    /// </summary>
    public static bool UpdateKeyIfExists(List<string> lines, string key, string value, bool useQuotes = true, string? section = null)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var inTargetSection = section == null;
        for (var i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].Trim();

            if (section != null && trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                var currentSection = trimmed[1..^1].Trim();
                inTargetSection = currentSection.Equals(section, StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inTargetSection || string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(';') || trimmed.StartsWith('#'))
                continue;

            var equalsPos = trimmed.IndexOf('=');
            if (equalsPos <= 0) continue;

            var lineKey = trimmed[..equalsPos].Trim();
            if (lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                var currentValue = trimmed[(equalsPos + 1)..].Trim().Trim('\'', '"');
                var newLine = useQuotes ? $"{key} = '{value}'" : $"{key} = {value}";

                if (!currentValue.Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    var oldValueDisplay = currentValue;
                    lines[i] = newLine;
                    UIScope.WriteMessage($"[gray]   [[INFO]] {MarkupHelper.Escape(key)} atualizado:[/]");
                    UIScope.WriteMessage($"         [red]Antes: {MarkupHelper.Escape(oldValueDisplay)}[/]");
                    UIScope.WriteMessage($"         [green]Depois: {MarkupHelper.Escape(value)}[/]");
                    return true;
                }
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Remove todas as ocorrencias de uma chave (inclusive duplicadas ou digitadas com erro).
    /// </summary>
    public static bool RemoveKey(List<string> lines, string key)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var removed = false;

        for (var i = lines.Count - 1; i >= 0; i--)
        {
            var trimmed = lines[i].Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(';') || trimmed.StartsWith('#'))
                continue;

            var equalsPos = trimmed.IndexOf('=');
            if (equalsPos <= 0) continue;

            var lineKey = trimmed[..equalsPos].Trim();
            if (lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                lines.RemoveAt(i);
                removed = true;
                UIScope.WriteMessage($"[gray]   [[INFO]] Chave obsoleta/duplicada removida: {MarkupHelper.Escape(key)}[/]");
            }
        }

        return removed;
    }
}