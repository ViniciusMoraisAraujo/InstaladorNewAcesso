namespace InstaladorNewAcesso.Core.Utils;

/// <summary>
/// Helper simples para escapar texto para markup, substituindo a dependência
/// de Spectre.Console.EscapeMarkup() nos arquivos que foram migrados para UIScope.
/// 
/// Escapa caracteres especiais '[' e ']' substituindo por '[[' e ']]'
/// para evitar que sejam interpretados como tags de markup.
/// </summary>
internal static class MarkupHelper
{
    /// <summary>
    /// Escapa o texto para uso seguro em markup (substitui '[' por '[[' e ']' por ']]').
    /// </summary>
    public static string Escape(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Replace("[", "[[").Replace("]", "]]");
    }
}
