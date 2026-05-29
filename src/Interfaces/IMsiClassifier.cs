using InstaladorNewAcesso.Models;

namespace InstaladorNewAcesso.Interfaces;

public interface IMsiClassifier
{
    MsiInstallationModel ClassifyDirectoryMsi(string msiPath, string msiSourceRoot, bool isDbSpecific);
    MsiInstallationModel ClassifyRootMsi(string msiPath);
    string ExtractManufacturerName(string fileName);
}