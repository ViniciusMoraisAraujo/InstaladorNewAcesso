using System.Runtime.InteropServices;
using InstaladorNewAcesso.Implementations;
using InstaladorNewAcesso.Interfaces;
using Microsoft.Win32;
using Spectre.Console;

namespace InstaladorNewAcesso.Factories;

public static class InstallerFactory
{
    public static IFeatureInstaller Create()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("Este programa roda apenas no Windows");
        }

        if (IsWindowsServer())
        {
            AnsiConsole.MarkupLine("[cyan]AMBIENTE:[/] [green]WINDOWS SERVER (ServerManage)[/]");
            return new WindowsServerInstaller();
        }

        AnsiConsole.MarkupLine("[cyan]AMBIENTE:[/] [green]WINDOWS DESKTOP (DISM)[/]");
        return new WindowsDesktopInstaller();
    }

    private static bool IsWindowsServer()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion");
            var installationType = key.GetValue("InstallationType")?.ToString();
            return installationType != null && installationType.Contains("Server", StringComparison.OrdinalIgnoreCase);
        }
        catch 
        {
            return RuntimeInformation.OSDescription.Contains("Server", StringComparison.OrdinalIgnoreCase);        
        }
    }
}