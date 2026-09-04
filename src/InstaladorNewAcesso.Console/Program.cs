#pragma warning disable CA1416

using System.Security.Principal;
using InstaladorNewAcesso.Console.Services;
using InstaladorNewAcesso.Console.Views;
using Spectre.Console;

// ── Verificação de Administrador ─────────────────────────────
using var identity = WindowsIdentity.GetCurrent();
var principal = new WindowsPrincipal(identity);
var isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

if (!isAdmin)
{
    AnsiConsole.Write(
        new FigletText("ERRO")
            .Centered()
            .Color(Color.Red));

    AnsiConsole.MarkupLine("\n[red][[ERRO CRÍTICO]][/] Este instalador precisa de privilégios de [bold]Administrador[/]!");
    AnsiConsole.MarkupLine("[red]Por favor, execute o terminal como [bold]Administrador[/].[/]");
    return;
}

// ── Inicialização ────────────────────────────────────────────
try
{
    var ui = new ConsoleUIService();
    InstaladorNewAcesso.Core.Services.UIScope.Current = ui;
    
    var argsList = Environment.GetCommandLineArgs().ToList();
    var unattendedIndex = argsList.IndexOf("--unattended");
    
    if (unattendedIndex >= 0 && unattendedIndex + 1 < argsList.Count)
    {
        var configPath = argsList[unattendedIndex + 1];
        var runner = new UnattendedRunner(ui);
        await runner.RunAsync(configPath);
    }
    else
    {
        var view = new MainMenuView(ui);
        await view.ExecuteAsync();
    }
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"\n[red][[ERRO FATAL]][/] {ex.Message.EscapeMarkup()}");
    AnsiConsole.MarkupLine("[gray]Detalhes:[/]");
    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
}
finally
{
    AnsiConsole.MarkupLine("\n[gray]Pressione ENTER para fechar...[/]");
    System.Console.ReadLine();
}

#pragma warning restore CA1416
