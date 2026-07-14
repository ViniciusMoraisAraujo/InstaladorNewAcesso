using InstaladorNewAcesso.Abstractions.Interfaces;

namespace InstaladorNewAcesso.Core.Services;

/// <summary>
/// Escopo estático que permite Services/Utils do Core escreverem output
/// através de IUIService sem precisar de injeção de dependência explícita.
///
/// Funciona como fallback: se Current estiver setado, usa IUIService;
/// caso contrário, usa System.Console (comportamento original simplificado).
///
/// Uso:
///   UIScope.Current = new ConsoleUIService(...);    // no Console Program
/// </summary>
public static class UIScope
{
    /// <summary>
    /// Define a instância ativa de IUIService para todo o Core.
    /// Configure no startup da aplicação Console.
    /// </summary>
    public static IUIService? Current { get; set; }

    // ── Output ────────────────────────────────────────────────

    public static void WriteMessage(string text, string? color = null)
    {
        if (Current != null)
            Current.WriteMessage(text, color);
        else
            Console.WriteLine(text);
    }

    public static void WriteInfo(string message)
    {
        if (Current != null)
            Current.WriteInfo(message);
        else
            Console.WriteLine($"INFO: {message}");
    }

    public static void WriteSuccess(string message)
    {
        if (Current != null)
            Current.WriteSuccess(message);
        else
            Console.WriteLine($"OK: {message}");
    }

    public static void WriteWarning(string message)
    {
        if (Current != null)
            Current.WriteWarning(message);
        else
            Console.WriteLine($"AVISO: {message}");
    }

    public static void WriteError(string message)
    {
        if (Current != null)
            Current.WriteError(message);
        else
            Console.Error.WriteLine($"ERRO: {message}");
    }

    public static void WriteEmptyLine()
    {
        if (Current != null)
            Current.WriteEmptyLine();
        else
            Console.WriteLine();
    }

    // ── Input ─────────────────────────────────────────────────

    public static string AskInput(string prompt, string? defaultValue = null)
    {
        if (Current != null)
            return Current.AskInput(prompt, defaultValue);

        Console.Write($"{prompt} ");
        var input = Console.ReadLine() ?? string.Empty;
        return string.IsNullOrEmpty(input) && defaultValue != null ? defaultValue : input;
    }

    public static bool Confirm(string prompt, bool defaultValue = true)
    {
        if (Current != null)
            return Current.Confirm(prompt, defaultValue);

        Console.Write($"{prompt} (s/N): ");
        var input = Console.ReadLine()?.Trim().ToLowerInvariant();
        return input switch
        {
            "s" or "sim" or "y" or "yes" => true,
            _ => defaultValue
        };
    }
}
