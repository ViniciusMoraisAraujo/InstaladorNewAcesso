using FluentAssertions;
using InstaladorNewAcesso.Services;

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
    public void HasDeployableFiles_WithThreeOrMoreFiles_ShouldReturnTrue()
    {
        Directory.CreateDirectory(_tempRoot);
        File.Create(Path.Combine(_tempRoot, "file1.txt")).Dispose();
        File.Create(Path.Combine(_tempRoot, "file2.txt")).Dispose();
        File.Create(Path.Combine(_tempRoot, "file3.txt")).Dispose();

        var result = WebAppInstaller.HasDeployableFiles(_tempRoot);
        result.Should().BeTrue();
    }

    [Fact]
    public void HasDeployableFiles_WithLessThanThreeFiles_ShouldReturnFalse()
    {
        Directory.CreateDirectory(_tempRoot);
        File.Create(Path.Combine(_tempRoot, "file1.txt")).Dispose();
        File.Create(Path.Combine(_tempRoot, "file2.txt")).Dispose();

        var result = WebAppInstaller.HasDeployableFiles(_tempRoot);
        result.Should().BeFalse();
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

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, true); }
            catch { /* cleanup on best-effort basis */ }
        }
    }
}
