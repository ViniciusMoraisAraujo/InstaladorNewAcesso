using InstaladorNewAcesso.Configurations;
using InstaladorNewAcesso.Models;
using InstaladorNewAcesso.Utils;
using Spectre.Console;

namespace InstaladorNewAcesso.Views;

public class DirectoryView
{
    public void ExecuteAsync(InstallationPaths basePath)
    {
        var setup = new DirectorySetup();

        AnsiConsole.Write(new Rule("[magenta]Criando estrutura de diretórios...[/]") { Style = Style.Parse("magenta") });
        AnsiConsole.WriteLine();

        var resultadosDaEtapa = new List<SummaryResult>();

        foreach (var path in setup.GetAllPaths(basePath))
        {
            if (Directory.Exists(path))
            {
                AnsiConsole.MarkupLine($" [cyan][IGNORADO][/] {path}");
                resultadosDaEtapa.Add(SummaryStore.Add("Diretórios", path, true, "Já existe"));
            }
            else
            {
                Directory.CreateDirectory(path);
                AnsiConsole.MarkupLine($" [green][CRIADO][/]   {path}");
                resultadosDaEtapa.Add(SummaryStore.Add("Diretórios", path, true, "Criado"));
            }
        }

        AnsiConsole.WriteLine();
        SummaryPanelView.ExibirEtapa("Diretórios", "📂", resultadosDaEtapa);
        AnsiConsole.Write(new Rule("[cyan]Fim da etapa de Diretórios[/]") { Style = Style.Parse("cyan") });
    }
}
