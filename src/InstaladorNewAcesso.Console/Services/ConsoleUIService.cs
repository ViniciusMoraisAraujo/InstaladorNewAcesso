using InstaladorNewAcesso.Abstractions.Interfaces;
using Spectre.Console;

namespace InstaladorNewAcesso.Console.Services;

/// <summary>
/// Implementação de IUIService para Console usando Spectre.Console.
/// Fornece output colorido, inputs, menus, progresso e status.
/// </summary>
public class ConsoleUIService : IUIService
{
    // ── Output ─────────────────────────────────────────────────

    public void Clear()
    {
        AnsiConsole.Clear();
    }

    public void WriteMessage(string text, string? color = null)
    {
        if (color != null)
            AnsiConsole.MarkupLine($"[{color}]{text.EscapeMarkup()}[/]");
        else
            AnsiConsole.MarkupLine(text);
    }

    public void WriteEmptyLine()
    {
        AnsiConsole.WriteLine();
    }

    public void WriteRule(string title, string color = "cyan")
    {
        AnsiConsole.Write(new Rule($"[{color}]{title.EscapeMarkup()}[/]")
        {
            Style = Style.Parse(color)
        });
    }

    public void WritePanel(string content, string? title = null, string color = "cyan")
    {
        var panel = new Panel(content)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse(color)
        };

        if (!string.IsNullOrWhiteSpace(title))
            panel.Header = new PanelHeader($"[{color}]{title.EscapeMarkup()}[/]");

        AnsiConsole.Write(panel);
    }

    public void WriteTable(string[] headers, List<string[]> rows)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderStyle(Style.Parse("cyan"));

        foreach (var header in headers)
            table.AddColumn(new TableColumn($"[bold cyan]{header.EscapeMarkup()}[/]"));

        foreach (var row in rows)
            table.AddRow(row.Select(cell => cell.EscapeMarkup()).ToArray());

        AnsiConsole.Write(table);
    }

    public void WriteFiglet(string text, string color = "red")
    {
        AnsiConsole.Write(
            new FigletText(text)
                .Centered()
                .Color(ParseColor(color)));
    }

    private static Color ParseColor(string color)
    {
        return color?.ToLowerInvariant() switch
        {
            "red" => Color.Red,
            "green" => Color.Green,
            "blue" => Color.Blue,
            "yellow" => Color.Yellow,
            "cyan" => Color.Cyan,
            "magenta" => Color.Magenta,
            "white" => Color.White,
            "gray" or "grey" => Color.Grey,
            "silver" => Color.Silver,
            "maroon" => Color.Maroon,
            "purple" => Color.Purple,
            "teal" => Color.Teal,
            "orange" => Color.Orange1,
            _ => Color.Default
        };
    }

    // ── Input ──────────────────────────────────────────────────

    public string AskInput(string prompt, string? defaultValue = null)
    {
        if (defaultValue != null)
            return AnsiConsole.Ask($"{prompt} ", defaultValue);

        return AnsiConsole.Ask<string>($"{prompt} ");
    }

    public string AskPassword(string prompt)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>(prompt)
                .PromptStyle("yellow")
                .Secret('*'));
    }

    public bool Confirm(string prompt, bool defaultValue = true)
    {
        return AnsiConsole.Confirm(prompt, defaultValue);
    }

    public int AskOption(string prompt, string[] options)
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(prompt)
                .PageSize(10)
                .AddChoices(options));

        return Array.IndexOf(options, choice);
    }

    public void WaitForEnter(string message = "Pressione ENTER para continuar...")
    {
        AnsiConsole.MarkupLine($"\n[gray]{message.EscapeMarkup()}[/]");
        System.Console.ReadLine();
    }

    public string AskChoice(string prompt, string[] choices, string? defaultChoice = null)
    {
        // Spectre.Console 0.57.x SelectionPrompt always selects the first item.
        // The defaultChoice parameter is accepted for interface compatibility
        // but not used since there's no API to pre-select an item.
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(prompt)
                .PageSize(10)
                .AddChoices(choices));
    }

    // ── Progress/Status ────────────────────────────────────────

    public async Task ShowProgress(string title, Func<Action<double, string>, Task> action)
    {
        await AnsiConsole.Progress()
            .Columns(
            [
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn()
            ])
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[cyan]{title.EscapeMarkup()}[/]");
                await action((progress, status) =>
                {
                    task.Value = Math.Clamp(progress * 100, 0, 100);
                    task.Description = $"[cyan]{status.EscapeMarkup()}[/]";
                });
                task.Value = 100;
                task.StopTask();
            });
    }

    public async Task ShowStatus(string title, Func<Action<string>, Task> action)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan"))
            .StartAsync($"[cyan]{title.EscapeMarkup()}[/]", async ctx =>
            {
                await action(status => ctx.Status($"[cyan]{status.EscapeMarkup()}[/]"));
            });
    }

    // ── Specialized Messages ───────────────────────────────────

    public void WriteError(string message)
    {
        AnsiConsole.MarkupLine($"[red bold][/] [red]{message.EscapeMarkup()}[/]");
    }

    public void WriteSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green bold]✔[/] [green]{message.EscapeMarkup()}[/]");
    }

    public void WriteWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow bold]⚠[/] [yellow]{message.EscapeMarkup()}[/]");
    }

    public void WriteInfo(string message)
    {
        AnsiConsole.MarkupLine($"[blue bold]ℹ[/] [blue]{message.EscapeMarkup()}[/]");
    }

    public void WriteRaw(string markupText)
    {
        AnsiConsole.MarkupLine(markupText);
    }

    public void WriteInline(string text)
    {
        AnsiConsole.Markup(text);
    }
}
