using InstaladorNewAcesso.Abstractions.Interfaces;

namespace InstaladorNewAcesso.Core.Services;

public class RollbackManager
{
    private readonly Stack<Func<Task>> _rollbackActions = new();
    private readonly IUIService _ui;

    public RollbackManager(IUIService ui)
    {
        _ui = ui;
    }

    /// <summary>
    /// Quantidade de ações registradas na pilha de rollback.
    /// </summary>
    public int Count => _rollbackActions.Count;

    /// <summary>
    /// Indica se há ações registradas para reversão.
    /// </summary>
    public bool HasActions => _rollbackActions.Count > 0;

    /// <summary>
    /// Adiciona uma ação de desfazer ao topo da pilha.
    /// </summary>
    public void Push(Func<Task> rollbackAction)
    {
        ArgumentNullException.ThrowIfNull(rollbackAction);
        _rollbackActions.Push(rollbackAction);
    }

    /// <summary>
    /// Executa todas as ações de rollback registradas na ordem LIFO (Last-In, First-Out).
    /// </summary>
    public async Task ExecuteRollbackAsync()
    {
        if (_rollbackActions.Count == 0)
            return;

        _ui.WriteRule("INICIANDO ROLLBACK ATIVO", "red");
        _ui.WriteMessage($"[yellow]Desfazendo {_rollbackActions.Count} operação(ões)...[/]");

        while (_rollbackActions.Count > 0)
        {
            var action = _rollbackActions.Pop();
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                _ui.WriteWarning($"Falha parcial no rollback: {ex.Message}");
            }
        }
        
        _ui.WriteMessage("[green]Rollback concluído.[/]");
    }
    
    public void Clear()
    {
        _rollbackActions.Clear();
    }
}
