using InstaladorNewAcesso.Abstractions.Interfaces;

namespace InstaladorNewAcesso.Core.Utils;

/// <summary>
/// Métodos auxiliares compartilhados entre as views do instalador.
/// </summary>
public class ViewHelper
{
    private readonly IUIService _ui;

    public ViewHelper(IUIService ui)
    {
        _ui = ui;
    }

    /// <summary>
    /// Converte uma string de índices separados por vírgula (ex: "1,3,5")
    /// em uma lista de índices zero-based válidos (1 a max).
    /// </summary>
    public static List<int> ParseIndices(string? input, int max)
    {
        var indices = new List<int>();
        if (string.IsNullOrWhiteSpace(input))
            return indices;

        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (int.TryParse(part.Trim(), out var num) && num >= 1 && num <= max)
            {
                indices.Add(num - 1);
            }
        }
        return indices.Distinct().OrderBy(i => i).ToList();
    }

    /// <summary>
    /// Exibe o prompt de seleção (T - Todos, S - Selecionar, I - Reinstalar, N - Não instalar)
    /// e retorna a letra da opção escolhida.
    /// </summary>
    public string PromptSelection()
    {
        var opcao = _ui.AskChoice(
            "[bold yellow]Opções de instalação:[/]",
            [
                "T - Instalar TODOS",
                "S - Selecionar manualmente",
                "I - Instalar apenas os que já estão instalados (reinstalar)",
                "N - Não instalar"
            ]);
        return opcao[..1];
    }

    /// <summary>
    /// Exibe um prompt solicitando que o usuário digite números separados por vírgula,
    /// retorna a lista de índices zero-based válidos.
    /// </summary>
    public List<int> AskIndices(string prompt, int max)
    {
        var input = _ui.AskInput($"[bold yellow]{prompt}[/] ([gray]ex: 1,3,5[/]):");
        var indices = ParseIndices(input, max);
        return indices;
    }

    /// <summary>
    /// Escaneia o diretório de instaladores em busca de versões disponíveis (subpastas)
    /// e retorna o caminho completo da versão escolhida.
    ///
    /// Comportamento:
    /// - Se houver apenas uma versão, seleciona automaticamente.
    /// - Se houver múltiplas, exibe um menu de seleção.
    /// - Se não houver versões, solicita entrada manual do usuário.
    /// </summary>
    /// <param name="installationPath">Caminho base onde ficam as pastas de versão (ex: C:\SoftPrime\Instaladores)</param>
    /// <param name="contextName">Nome descritivo para exibição no prompt (ex: "instaladores MSI")</param>
    /// <param name="defaultVersion">Nome da versão padrão caso não encontre nenhuma (ex: "PrimeAcesso V5.9")</param>
    public string ResolveInstallerPath(string installationPath, string contextName, string defaultVersion = "PrimeAcesso V5.9")
    {
        var versions = GetAvailableVersions(installationPath);

        if (versions.Count == 0)
        {
            // Nenhuma versão encontrada — cair para entrada manual
            _ui.WriteMessage($"[yellow]Nenhuma pasta de instaladores encontrada em:[/] [cyan]{MarkupHelper.Escape(installationPath)}[/]");
            _ui.WriteEmptyLine();

            var input = _ui.AskInput(
                $"[bold yellow]Digite o caminho completo da pasta de {contextName}[/] ([gray]ENTER para padrão: {defaultVersion}[/]):");

            if (string.IsNullOrWhiteSpace(input))
                return Path.Combine(installationPath, defaultVersion);

            return input;
        }

        if (versions.Count == 1)
        {
            var path = Path.Combine(installationPath, versions[0]);
            _ui.WriteMessage($"[green]Versão detectada automaticamente:[/] [cyan]{MarkupHelper.Escape(versions[0])}[/]");
            return path;
        }

        // Múltiplas versões — usuário escolhe
        _ui.WriteMessage($"[yellow]Múltiplas versões encontradas em:[/] [cyan]{MarkupHelper.Escape(installationPath)}[/]");
        _ui.WriteEmptyLine();

        var selected = _ui.AskChoice(
            $"[bold yellow]Selecione a versão dos {contextName}:[/]",
            versions.ToArray());

        return Path.Combine(installationPath, selected);
    }

    /// <summary>
    /// Obtém a lista de versões disponíveis (nomes das subpastas) no diretório de instaladores.
    /// </summary>
    private static List<string> GetAvailableVersions(string installationPath)
    {
        if (!Directory.Exists(installationPath))
            return new List<string>();

        return Directory.GetDirectories(installationPath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name)
            .ToList()!;
    }
}
