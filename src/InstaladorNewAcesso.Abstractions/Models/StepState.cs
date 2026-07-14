namespace InstaladorNewAcesso.Abstractions.Models;

/// <summary>
/// Representa o estado atual de uma etapa do instalador.
/// </summary>
public enum StepState
{
    /// <summary>Aguardando execução</summary>
    Pending,

    /// <summary>Em execução</summary>
    Running,

    /// <summary>Concluída com sucesso</summary>
    Success,

    /// <summary>Concluída com falha</summary>
    Failed,

    /// <summary>Concluída com aviso</summary>
    Warning
}
