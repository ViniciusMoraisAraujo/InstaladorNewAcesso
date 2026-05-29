namespace InstaladorNewAcesso.Interfaces;

public interface ISimpleLogger
{
    void LogInfo(string message);
    void LogError(string message);
    void LogWarning(string message);
    void LogSuccess(string message);
}