using System.Security.Principal;
using InstaladorNewAcesso.Views;
using Spectre.Console;

using var identity = WindowsIdentity.GetCurrent();
var principal = new WindowsPrincipal(identity);
bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
if (!isAdmin)
{
    AnsiConsole.Write(
        new FigletText("ERRO")
            .Centered()
            .Color(Color.Red));

    AnsiConsole.MarkupLine("\n[red][ERRO CRÍTICO][/] Este instalador precisa de privilégios de [bold]Administrador[/]!");
    AnsiConsole.MarkupLine("[red]Por favor, execute o terminal como [bold]Administrador[/].[/]");
    return;
}

var view = new MainMenuView();
await view.ExecuteAsync();
