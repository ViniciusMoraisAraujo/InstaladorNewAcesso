using System.Security.Principal;
using InstaladorNewAcesso.Implementations;
using InstaladorNewAcesso.Interfaces;
using InstaladorNewAcesso.Models;

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


Console.WriteLine("Carregando Recursos");
IFeatureInstaller create = InstallerFactory.Create();
var setup = new FeatureSetup();
foreach (var feature in setup.Features)
{
    Console.WriteLine($"\n Verificando: {feature.FriendlyName}");
    var alreadyExists = await create.IsFeatureInstalledAsync(feature);
    if (alreadyExists)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[IGNORADO] {feature.FriendlyName} já está ativo no sistema.");
        Console.ResetColor();
        continue; 
    }
    Console.WriteLine($"\n Processando: {feature.FriendlyName}");
    await create.InstallFeatureAsync(feature);
}