namespace InstaladorNewAcesso.Abstractions.Models;

/// <summary>
/// Representa o status de uma etapa individual do instalador.
/// Usado pelo <c>StatusPanel</c> para exibir o progresso em tempo real.
/// </summary>
public class StepStatus
{
    /// <summary>Nome curto da etapa (ex: "Instalar MSI - ControleAcesso")</summary>
    public string StepName { get; set; } = string.Empty;

    /// <summary>Descrição detalhada da etapa</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Estado atual da etapa</summary>
    public StepState State { get; set; } = StepState.Pending;

    /// <summary>Detalhe do erro, se houver</summary>
    public string? ErrorDetail { get; set; }

    /// <summary>Momento em que a etapa iniciou</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>Momento em que a etapa terminou</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Duração total da etapa, se concluída</summary>
    public TimeSpan? Duration =>
        StartedAt.HasValue && CompletedAt.HasValue
            ? CompletedAt.Value - StartedAt.Value
            : null;

    /// <summary>
    /// Marca a etapa como em execução.
    /// </summary>
    public void Start()
    {
        State = StepState.Running;
        StartedAt = DateTime.Now;
    }

    /// <summary>
    /// Marca a etapa como concluída com sucesso.
    /// </summary>
    public void Complete()
    {
        State = StepState.Success;
        CompletedAt = DateTime.Now;
    }

    /// <summary>
    /// Marca a etapa como falha.
    /// </summary>
    public void Fail(string? detail = null)
    {
        State = StepState.Failed;
        ErrorDetail = detail;
        CompletedAt = DateTime.Now;
    }

    /// <summary>
    /// Marca a etapa como concluída com aviso.
    /// </summary>
    public void Warn(string? detail = null)
    {
        State = StepState.Warning;
        ErrorDetail = detail;
        CompletedAt = DateTime.Now;
    }
}
