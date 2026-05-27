using System.Security.Principal;
using InstaladorNewAcesso.Views;

using var identity = WindowsIdentity.GetCurrent();
var principal = new WindowsPrincipal(identity);
bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
if (!isAdmin)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("[ERRO CRÍTICO] Este instalador precisa de privilégios de Administrador!");
    Console.WriteLine("Por favor, execute o Rider ou o terminal como Administrador.");
    Console.ResetColor();
    return;
}

var view = new ResourceView();
await view.ExecuteInstallAsync();