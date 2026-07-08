namespace InstaladorNewAcesso.Models;

public class SummaryResult
{
    public string Etapa { get; set; } = string.Empty;
    public string Item { get; set; } = string.Empty;
    public bool Sucesso { get; set; }
    public string? Detalhe { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
