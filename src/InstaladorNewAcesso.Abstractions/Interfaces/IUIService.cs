namespace InstaladorNewAcesso.Abstractions.Interfaces;

/// <summary>
/// Abstração de interface do usuário para o Console (CLI).
/// Todas as operações de UI devem passar por esta interface.
/// </summary>
public interface IUIService
{
    // ── Output ──────────────────────────────────────────────

    /// <summary>
    /// Limpa a tela do console.
    /// </summary>
    void Clear();

    /// <summary>
    /// Escreve uma linha formatada com cores (suporta markup Spectre).
    /// Ex: WriteMessage("[green]OK[/]") ou WriteMessage("texto simples", "green")
    /// </summary>
    void WriteMessage(string text, string? color = null);

    /// <summary>
    /// Escreve uma linha vazia (newline).
    /// </summary>
    void WriteEmptyLine();

    /// <summary>
    /// Exibe um separador visual com título.
    /// </summary>
    void WriteRule(string title, string color = "cyan");

    /// <summary>
    /// Exibe um painel com conteúdo e título opcional.
    /// </summary>
    void WritePanel(string content, string? title = null, string color = "cyan");

    /// <summary>
    /// Exibe uma tabela formatada.
    /// </summary>
    /// <param name="headers">Cabeçalhos das colunas</param>
    /// <param name="rows">Linhas de dados (cada linha é um array de strings)</param>
    void WriteTable(string[] headers, List<string[]> rows);

    /// <summary>
    /// Exibe texto grande (Figlet).
    /// </summary>
    void WriteFiglet(string text, string color = "red");

    // ── Input ───────────────────────────────────────────────

    /// <summary>
    /// Solicita um texto do usuário.
    /// </summary>
    string AskInput(string prompt, string? defaultValue = null);

    /// <summary>
    /// Solicita uma senha (input oculto).
    /// </summary>
    string AskPassword(string prompt);

    /// <summary>
    /// Confirmação Sim/Não.
    /// </summary>
    bool Confirm(string prompt, bool defaultValue = true);

    /// <summary>
    /// Seleção de opção múltipla (menu).
    /// Retorna o índice da opção escolhida (0-based).
    /// </summary>
    int AskOption(string prompt, string[] options);

    /// <summary>
    /// Espera o usuário pressionar Enter para continuar.
    /// </summary>
    void WaitForEnter(string message = "Pressione ENTER para continuar...");

    /// <summary>
    /// Seleção de opção com múltiplas escolhas (texto).
    /// Ex: "SQLServer" ou "Oracle"
    /// </summary>
    string AskChoice(string prompt, string[] choices, string? defaultChoice = null);

    // ── Progress/Status ─────────────────────────────────────

    /// <summary>
    /// Executa uma ação com barra de progresso.
    /// </summary>
    Task ShowProgress(string title, Func<Action<double, string>, Task> action);

    /// <summary>
    /// Executa uma ação com indicador de status (spinner).
    /// O callback pode atualizar o texto do status dinamicamente.
    /// </summary>
    Task ShowStatus(string title, Func<Action<string>, Task> action);

    /// <summary>
    /// Exibe mensagem de erro formatada.
    /// </summary>
    void WriteError(string message);

    /// <summary>
    /// Exibe mensagem de sucesso formatada.
    /// </summary>
    void WriteSuccess(string message);

    /// <summary>
    /// Exibe mensagem de aviso formatada.
    /// </summary>
    void WriteWarning(string message);

    /// <summary>
    /// Exibe mensagem de informação formatada.
    /// </summary>
    void WriteInfo(string message);

    /// <summary>
    /// Escreve um IRenderable diretamente (para Spectre.Console Table, Panel, etc).
    /// Usado para operações avançadas que não cabem nas abstrações simples.
    /// </summary>
    void WriteRaw(string markupText);

    /// <summary>
    /// Escreve texto inline (sem newline) - para uso em mensagens parciais.
    /// </summary>
    void WriteInline(string text);
}
