using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Configurations;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Console.Views;

public class DirectoryView
{
    private readonly IUIService _ui;
    private readonly SummaryPanelView _summaryView;

    public DirectoryView(IUIService ui)
    {
        _ui = ui;
        _summaryView = new SummaryPanelView(ui);
    }

    public void Execute(InstallationPaths basePath)
    {
        _ui.WriteRule("Criando estrutura de diretórios...", "magenta");
        _ui.WriteEmptyLine();

        var resultadosDaEtapa = new List<SummaryResult>();

        foreach (var path in DirectorySetup.GetAllPaths(basePath))
        {
            if (Directory.Exists(path))
            {
                _ui.WriteMessage($" [cyan]IGNORADO[/] {path}");
                resultadosDaEtapa.Add(SummaryStore.Add("Diretórios", path, true, "Já existe"));
            }
            else
            {
                Directory.CreateDirectory(path);
                _ui.WriteMessage($" [green][[CRIADO]][/]   {path}");
                resultadosDaEtapa.Add(SummaryStore.Add("Diretórios", path, true, "Criado"));
            }
        }

        _ui.WriteEmptyLine();
        _summaryView.ExibirEtapa("Diretórios", "📂", resultadosDaEtapa);
        _ui.WriteRule("Fim da etapa de Diretórios", "cyan");
    }
}
