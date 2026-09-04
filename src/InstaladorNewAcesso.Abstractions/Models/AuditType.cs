namespace InstaladorNewAcesso.Abstractions.Models;

/// <summary>
/// Tipo de operação de auditoria, usado para gerar nome de arquivo e cabeçalho dinâmicos.
/// </summary>
public enum AuditType
{
    /// <summary>Auditoria de instalação de componentes.</summary>
    Install,

    /// <summary>Auditoria de desinstalação de componentes.</summary>
    Uninstall,

    /// <summary>Auditoria de manutenção e configuração.</summary>
    Maintenance
}
