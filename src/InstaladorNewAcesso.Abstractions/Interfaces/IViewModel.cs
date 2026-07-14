namespace InstaladorNewAcesso.Abstractions.Interfaces;

/// <summary>
/// Interface base para ViewModels seguindo um padrão MVVM simplificado.
/// Gerencia o estado da tela e isola a lógica de negócio da UI.
/// </summary>
public interface IViewModel
{
    /// <summary>
    /// Indica se o ViewModel está executando uma operação assíncrona.
    /// A UI pode usar isso para mostrar/ocultar indicadores de carregamento.
    /// </summary>
    bool IsBusy { get; }

    /// <summary>
    /// Disparado quando o estado IsBusy muda.
    /// </summary>
    event EventHandler<bool>? IsBusyChanged;

    /// <summary>
    /// Mensagem de status atual do ViewModel (ex: "Instalando MSI 3/5...").
    /// </summary>
    string StatusMessage { get; }

    /// <summary>
    /// Disparado quando a mensagem de status muda.
    /// </summary>
    event EventHandler<string>? StatusMessageChanged;

    /// <summary>
    /// Inicializa o ViewModel e carrega dados iniciais.
    /// Chamado quando a tela é ativada.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Limpa recursos quando a tela é desativada.
    /// </summary>
    Task UnloadAsync();
}
