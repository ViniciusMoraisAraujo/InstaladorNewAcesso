
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Core.Utils;

public static class IniHelperBase
{
    /// <summary>
    /// Define ou cria uma chave key=value em um arquivo .INI.
    /// Se <paramref name="section"> for fornecido, a chave só será
    /// atualizada quando estiver dentro do cabeçalho [section] correspondente.
    /// Retorna true se o arquivo foi modificado.
    /// </summary>
    public static bool SetIniKey(List<string> lines, string key, string value, bool useQuotes = true, string? section = null)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var foundInTarget = false;
        var keyExistsAnywhere = false;
        var everEnteredTarget = section == null;
        var modified = false;
        var inTargetSection = section == null; // sem seção = aceita qualquer lugar

        for (var i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].Trim();

            // Rastrear cabeçalhos de seção
            if (section != null && trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                var currentSection = trimmed[1..^1].Trim();
                inTargetSection = currentSection.Equals(section, StringComparison.OrdinalIgnoreCase);
                if (inTargetSection) everEnteredTarget = true;
                continue;
            }

            // Ignora linhas de comentário ou vazias
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(';') || trimmed.StartsWith('#'))
                continue;

            // Detecta chave em qualquer lugar (para keyExistsAnywhere)
            var equalsPos = trimmed.IndexOf('=');
            if (equalsPos <= 0) continue;

            var lineKey = trimmed[..equalsPos].Trim();
            if (!lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                continue;

            keyExistsAnywhere = true;

            // Só modifica dentro da seção-alvo
            if (!inTargetSection) continue;

            foundInTarget = true;

            // Extrai o valor atual (remove aspas simples/duplas se houver)
            var currentValue = trimmed[(equalsPos + 1)..].Trim().Trim('\'', '"');
            var newLine = useQuotes ? $"{key} = '{value}'" : $"{key} = {value}";

            if (!currentValue.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                var oldValueDisplay = currentValue;
                lines[i] = newLine;
                modified = true;
                UIScope.WriteMessage($"[gray]   [[INFO]] {MarkupHelper.Escape(key)} atualizado:[/]");
                UIScope.WriteMessage($"         [red]Antes: {MarkupHelper.Escape(oldValueDisplay)}[/]");
                UIScope.WriteMessage($"         [green]Depois: {MarkupHelper.Escape(value)}[/]");
            }
            else
            {
                UIScope.WriteMessage($"[gray]   [[INFO]] {MarkupHelper.Escape(key)} já está correto.[/]");
            }

            break;
        }

        // Se chave não encontrada na seção-alvo e não existe em nenhuma seção, insere na seção-alvo
        if (!foundInTarget && !keyExistsAnywhere && everEnteredTarget)
        {
            var newLine = useQuotes ? $"{key} = '{value}'" : $"{key} = {value}";
            lines.Add(string.Empty);
            lines.Add(newLine);
            modified = true;
            UIScope.WriteMessage($"[green]   [[OK]] {MarkupHelper.Escape(key)} adicionado: {MarkupHelper.Escape(value)}[/]");
        }

        return modified;
    }
}
