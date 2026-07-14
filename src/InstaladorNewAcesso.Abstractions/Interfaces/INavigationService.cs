namespace InstaladorNewAcesso.Abstractions.Interfaces;

/// <summary>
/// Serviço de navegação tipado entre telas.
/// Abstrai o NavigationManager concreto para permitir testes e desacoplamento.
/// O registro das telas (setup) é feito no NavigationManager concreto, fora desta interface.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Indica se há uma tela anterior no histórico (botão "voltar" habilitado).
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Navega para a tela registrada com o nome especificado.
    /// </summary>
    void NavigateTo(string screenName);

    /// <summary>
    /// Navega para a tela anterior no histórico.
    /// </summary>
    void GoBack();

    /// <summary>
    /// Substitui a tela atual sem adicionar ao histórico (útil para transições).
    /// </summary>
    void ReplaceWith(string screenName);

    /// <summary>
    /// Disparado quando a navegação ocorre, informando o nome da tela atual.
    /// </summary>
    event Action<string>? NavigationChanged;

    /// <summary>
    /// Disparado quando o estado do botão "voltar" muda.
    /// </summary>
    event Action<bool>? CanGoBackChanged;
}
