namespace InstaladorNewAcesso.Abstractions.Interfaces;

/// <summary>
/// Interface base para todas as telas (UserControls) do instalador.
/// Define o ciclo de vida que uma tela deve seguir.
/// </summary>
public interface IView
{
    /// <summary>
    /// Título exibido na barra superior quando esta tela está ativa.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Chamado quando a tela é ativada (navegada).
    /// Use para carregar dados, iniciar timers, etc.
    /// </summary>
    Task ActivateAsync();

    /// <summary>
    /// Chamado quando a tela é desativada (navega para outra).
    /// Use para salvar estado, parar timers, liberar recursos.
    /// </summary>
    Task DeactivateAsync();
}
