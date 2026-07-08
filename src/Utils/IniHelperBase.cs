using Spectre.Console;

namespace InstaladorNewAcesso.Utils;

public static class IniHelperBase
{
    /// <summary>
    /// Define ou cria uma chave key=value em um arquivo .INI (sem seção).
    /// Retorna true se o arquivo foi modificado.
    /// </summary>
    public static bool SetIniKey(List<string> lines, string key, string value, bool useQuotes = true)
    {
        var found = false;
        var modified = false;

        for (int i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].Trim();

            // Ignora linhas de comentário ou vazias
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(';') || trimmed.StartsWith('#'))
                continue;

            // Verifica se a linha começa com a chave (case-insensitive)
            var equalsPos = trimmed.IndexOf('=');
            if (equalsPos <= 0) continue;

            var lineKey = trimmed[..equalsPos].Trim();
            if (!lineKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                continue;

            found = true;

            // Extrai o valor atual (remove aspas simples/duplas se houver)
            var currentValue = trimmed[(equalsPos + 1)..].Trim().Trim('\'', '"');
            var newLine = useQuotes ? $"{key} = '{value}'" : $"{key} = {value}";

            if (!currentValue.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                var oldValueDisplay = currentValue;
                lines[i] = newLine;
                modified = true;                    AnsiConsole.MarkupLine($"[gray]   [[INFO]] {key.EscapeMarkup()} atualizado:[/]");
                AnsiConsole.MarkupLine($"         [red]Antes: {oldValueDisplay.EscapeMarkup()}[/]");
                AnsiConsole.MarkupLine($"         [green]Depois: {value.EscapeMarkup()}[/]");
            }
            else
            {                    AnsiConsole.MarkupLine($"[gray]   [[INFO]] {key.EscapeMarkup()} já está correto.[/]");
            }

            break;
        }

        if (!found)
        {
            var newLine = useQuotes ? $"{key} = '{value}'" : $"{key} = {value}";
            lines.Add(string.Empty);
            lines.Add(newLine);
            modified = true;
            AnsiConsole.MarkupLine($"[green]   [[OK]] {key.EscapeMarkup()} adicionado: {value.EscapeMarkup()}[/]");
        }

        return modified;
    }
}
