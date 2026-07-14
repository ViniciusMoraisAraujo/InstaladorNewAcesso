using FluentAssertions;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Tests.Services;

public class WebAppInstallerTests : IDisposable
{
    private readonly string _tempRoot;

    public WebAppInstallerTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "WebAppInstallerTests_" + Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void HasDeployableFiles_WhenDirectoryNotExists_ShouldReturnFalse()
    {
        var nonExistent = Path.Combine(_tempRoot, "NonExistent");
        var result = WebAppInstaller.HasDeployableFiles(nonExistent);
        result.Should().BeFalse();
    }

    [Fact]
    public void HasDeployableFiles_WhenEmptyDirectory_ShouldReturnFalse()
    {
        Directory.CreateDirectory(_tempRoot);
        var result = WebAppInstaller.HasDeployableFiles(_tempRoot);
        result.Should().BeFalse();
    }

    [Fact]
    public void HasDeployableFiles_WithDllFiles_ShouldReturnTrue()
    {
        Directory.CreateDirectory(_tempRoot);
        File.Create(Path.Combine(_tempRoot, "app.dll")).Dispose();

        var result = WebAppInstaller.HasDeployableFiles(_tempRoot);
        result.Should().BeTrue();
    }

    [Fact]
    public void HasDeployableFiles_WithAspxFiles_ShouldReturnTrue()
    {
        Directory.CreateDirectory(_tempRoot);
        File.Create(Path.Combine(_tempRoot, "default.aspx")).Dispose();

        var result = WebAppInstaller.HasDeployableFiles(_tempRoot);
        result.Should().BeTrue();
    }

    [Fact]
    public void HasDeployableFiles_WithConfigFiles_ShouldReturnTrue()
    {
        Directory.CreateDirectory(_tempRoot);
        File.Create(Path.Combine(_tempRoot, "web.config")).Dispose();

        var result = WebAppInstaller.HasDeployableFiles(_tempRoot);
        result.Should().BeTrue();
    }

    [Fact]
    public void HasDeployableFiles_WithNonDeployableFiles_ShouldReturnFalse()
    {
        Directory.CreateDirectory(_tempRoot);
        File.Create(Path.Combine(_tempRoot, "file1.txt")).Dispose();
        File.Create(Path.Combine(_tempRoot, "file2.txt")).Dispose();
        File.Create(Path.Combine(_tempRoot, "file3.txt")).Dispose();

        var result = WebAppInstaller.HasDeployableFiles(_tempRoot);
        result.Should().BeFalse();
    }

    [Fact]
    public void HasDeployableFiles_WithMixedDeployableAndNonDeployable_ShouldReturnTrue()
    {
        Directory.CreateDirectory(_tempRoot);
        File.Create(Path.Combine(_tempRoot, "app.dll")).Dispose();
        File.Create(Path.Combine(_tempRoot, "readme.txt")).Dispose();
        File.Create(Path.Combine(_tempRoot, "notes.md")).Dispose();

        var result = WebAppInstaller.HasDeployableFiles(_tempRoot);
        result.Should().BeTrue();
    }

    [Fact]
    public void LocateInstalledPath_WhenForcedPathHasDeployableFiles_ShouldReturnForcedPath()
    {
        Directory.CreateDirectory(_tempRoot);
        File.Create(Path.Combine(_tempRoot, "app.dll")).Dispose();

        var result = WebAppInstaller.LocateInstalledPath(_tempRoot, "WebAppDS");
        result.Should().Be(_tempRoot);
    }

    [Fact]
    public void LocateInstalledPath_WhenSubfolderHasDeployableFiles_ShouldReturnSubfolder()
    {
        var subfolder = Path.Combine(_tempRoot, "WebAppDS");
        Directory.CreateDirectory(subfolder);
        File.Create(Path.Combine(subfolder, "app.dll")).Dispose();

        var result = WebAppInstaller.LocateInstalledPath(_tempRoot, "WebAppDS");
        result.Should().Be(subfolder);
    }

    [Fact]
    public void LocateInstalledPath_WhenNoDeployableFiles_ShouldReturnNull()
    {
        Directory.CreateDirectory(_tempRoot);
        // Criar subpasta vazia também
        Directory.CreateDirectory(Path.Combine(_tempRoot, "WebAppDS"));

        var result = WebAppInstaller.LocateInstalledPath(_tempRoot, "WebAppDS");
        result.Should().BeNull();
    }

    [Fact]
    public void LocateInstalledPath_ForcedPathTakesPriorityOverSubfolder()
    {
        // Ambos têm arquivos deployable - deve retornar ForcedPath (prioridade)
        Directory.CreateDirectory(_tempRoot);
        File.Create(Path.Combine(_tempRoot, "app.dll")).Dispose();

        var subfolder = Path.Combine(_tempRoot, "WebAppDS");
        Directory.CreateDirectory(subfolder);
        File.Create(Path.Combine(subfolder, "index.aspx")).Dispose();

        var result = WebAppInstaller.LocateInstalledPath(_tempRoot, "WebAppDS");
        result.Should().Be(_tempRoot);
    }

    [Fact]
    public void LocateInstalledPath_WithNestedFilesInSubfolder_ShouldDetectSubfolder()
    {
        var subfolder = Path.Combine(_tempRoot, "WebAppUI");
        Directory.CreateDirectory(subfolder);
        // Apenas arquivos .txt em subpasta aninhada
        var nested = Path.Combine(subfolder, "bin");
        Directory.CreateDirectory(nested);
        File.Create(Path.Combine(nested, "lib.dll")).Dispose();
        File.Create(Path.Combine(nested, "data.dll")).Dispose();

        var result = WebAppInstaller.LocateInstalledPath(_tempRoot, "WebAppUI");
        result.Should().Be(subfolder);
    }

    // ============================================================
    //  CopyFabricanteConfigDll — error paths
    // ============================================================

    [Fact]
    public void CopyFabricanteConfigDll_SourceDirNotFound_ReturnsFalse()
    {
        var paths = new InstallationPaths(Path.Combine(_tempRoot, "SoftPrime"));
        // paths.Fabricantes doesn't exist

        var result = WebAppInstaller.CopyFabricanteConfigDll(paths);

        result.Should().BeFalse();
    }

    [Fact]
    public void CopyFabricanteConfigDll_NoMatchingDlls_ReturnsFalse()
    {
        var paths = new InstallationPaths(Path.Combine(_tempRoot, "SoftPrime"));
        Directory.CreateDirectory(paths.Fabricantes);
        // Create non-matching files
        File.Create(Path.Combine(paths.Fabricantes, "readme.txt")).Dispose();
        File.Create(Path.Combine(paths.Fabricantes, "other.dll")).Dispose();

        var result = WebAppInstaller.CopyFabricanteConfigDll(paths);

        result.Should().BeFalse();
    }

    [Fact]
    public void CopyFabricanteConfigDll_WithMatchingDll_CopiesAndReturnsTrue()
    {
        var paths = new InstallationPaths(Path.Combine(_tempRoot, "SoftPrime"));
        Directory.CreateDirectory(paths.Fabricantes);
        File.WriteAllText(Path.Combine(paths.Fabricantes, "fabricante.Configuracao.dll"), "content");

        var result = WebAppInstaller.CopyFabricanteConfigDll(paths);

        result.Should().BeTrue();
        File.Exists(Path.Combine(paths.WebAppUIFabricantes, "fabricante.Configuracao.dll")).Should().BeTrue();
    }

    [Fact]
    public void CopyFabricanteConfigDll_PartialCopy_ReturnsTrue()
    {
        var paths = new InstallationPaths(Path.Combine(_tempRoot, "SoftPrime"));
        Directory.CreateDirectory(paths.Fabricantes);
        File.WriteAllText(Path.Combine(paths.Fabricantes, "fabricante.Configuracao.dll"), "content");
        File.WriteAllText(Path.Combine(paths.Fabricantes, "fabricante.Configuracao.B.dll"), "content");

        var result = WebAppInstaller.CopyFabricanteConfigDll(paths);

        result.Should().BeTrue();
        Directory.GetFiles(paths.WebAppUIFabricantes, "*.dll").Length.Should().Be(2);
    }

    // ============================================================
    //  LocateInstalledPath — additional edge cases
    // ============================================================

    [Fact]
    public void LocateInstalledPath_ForcedPathNotExists_SubfolderExists_ReturnsSubfolder()
    {
        // forcedInstallPath doesn't have files, but subfolder does
        var subfolder = Path.Combine(_tempRoot, "WebAppDS");
        Directory.CreateDirectory(subfolder);
        File.Create(Path.Combine(subfolder, "app.dll")).Dispose();

        var result = WebAppInstaller.LocateInstalledPath(_tempRoot, "WebAppDS");

        result.Should().Be(subfolder);
    }

    [Fact]
    public void LocateInstalledPath_ForcedPathNotExists_SubfolderNotExists_ReturnsNull()
    {
        // Neither forced path nor subfolder have deployable files
        Directory.CreateDirectory(_tempRoot);

        var result = WebAppInstaller.LocateInstalledPath(_tempRoot, "WebAppDS");

        result.Should().BeNull();
    }

    // ============================================================
    //  HasDeployableFiles — SearchOption.AllDirectories
    // ============================================================

    [Fact]
    public void HasDeployableFiles_WithDeepNestedDlls_SearchAllDirectories_ReturnsTrue()
    {
        var deepDir = Path.Combine(_tempRoot, "bin", "x64", "release");
        Directory.CreateDirectory(deepDir);
        File.Create(Path.Combine(deepDir, "app.dll")).Dispose();

        var result = WebAppInstaller.HasDeployableFiles(_tempRoot, SearchOption.AllDirectories);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasDeployableFiles_WithDeepNestedDlls_SearchTopDirectoryOnly_ReturnsFalse()
    {
        var deepDir = Path.Combine(_tempRoot, "bin", "x64", "release");
        Directory.CreateDirectory(deepDir);
        File.Create(Path.Combine(deepDir, "app.dll")).Dispose();

        var result = WebAppInstaller.HasDeployableFiles(_tempRoot, SearchOption.TopDirectoryOnly);

        result.Should().BeFalse();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, true); }
            catch { /* cleanup on best-effort basis */ }
        }
    }
}
