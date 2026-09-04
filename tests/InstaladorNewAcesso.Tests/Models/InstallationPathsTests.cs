using FluentAssertions;
using InstaladorNewAcesso.Abstractions.Models;

namespace InstaladorNewAcesso.Tests.Models;

public class InstallationPathsTests
{
    private const string TestBasePath = @"C:\SoftPrime";
    private readonly InstallationPaths _sut = new(TestBasePath);

    [Fact]
    public void Constructor_ShouldSetBasePath()
    {
        _sut.BasePath.Should().Be(TestBasePath);
    }

    [Fact]
    public void InstallationPath_ShouldCombineBasePathWithInstaladores()
    {
        _sut.InstallationPath.Should().Be($@"{TestBasePath}\Instaladores");
    }

    [Fact]
    public void NewAcessoRoot_ShouldCombineBasePathWithNewAcesso()
    {
        _sut.NewAcessoRoot.Should().Be($@"{TestBasePath}\NewAcesso");
    }

    [Fact]
    public void AutoAtendimento_ShouldCombineNewAcessoRootWithAutoAtendimento()
    {
        _sut.AutoAtendimento.Should().Be($@"{TestBasePath}\NewAcesso\AutoAtendimento");
    }

    [Fact]
    public void ConexBridge_ShouldCombineNewAcessoRootWithConexBridge()
    {
        _sut.ConexBridge.Should().Be($@"{TestBasePath}\NewAcesso\ConexBridge");
    }

    [Fact]
    public void ConnectionRecord_ShouldCombineNewAcessoRootWithConnectionRecord()
    {
        _sut.ConnectionRecord.Should().Be($@"{TestBasePath}\NewAcesso\ConnectionRecord");
    }

    [Fact]
    public void Controller_ShouldCombineNewAcessoRootWithController()
    {
        _sut.Controller.Should().Be($@"{TestBasePath}\NewAcesso\Controller");
    }

    [Fact]
    public void ControllerOffline_ShouldCombineNewAcessoRootWithControllerOffline()
    {
        _sut.ControllerOffline.Should().Be($@"{TestBasePath}\NewAcesso\ControllerOffline");
    }

    [Fact]
    public void VisitAuthorization_ShouldCombineNewAcessoRootWithVisitAuthorization()
    {
        _sut.VisitAuthorization.Should().Be($@"{TestBasePath}\NewAcesso\VisitAuthorization");
    }

    [Fact]
    public void Win_ShouldCombineNewAcessoRootWithWin()
    {
        _sut.Win.Should().Be($@"{TestBasePath}\NewAcesso\Win");
    }

    [Fact]
    public void ControleAcesso_ShouldCombineControllerWithControleAcesso()
    {
        _sut.ControleAcesso.Should().Be($@"{TestBasePath}\NewAcesso\Controller\ControleAcesso");
    }

    [Fact]
    public void CoreWs_ShouldCombineControllerWithCoreWs()
    {
        _sut.CoreWs.Should().Be($@"{TestBasePath}\NewAcesso\Controller\CoreWs");
    }

    [Fact]
    public void Fabricantes_ShouldCombineControllerWithFabricantes()
    {
        _sut.Fabricantes.Should().Be($@"{TestBasePath}\NewAcesso\Controller\Fabricantes");
    }

    [Fact]
    public void Task_ShouldCombineControllerWithTask()
    {
        _sut.Task.Should().Be($@"{TestBasePath}\NewAcesso\Controller\Task");
    }

    [Fact]
    public void ControllerOfflineArquivos_ShouldCombineControllerOfflineWithArquivos()
    {
        _sut.ControllerOfflineArquivos.Should().Be($@"{TestBasePath}\NewAcesso\ControllerOffline\Arquivos");
    }

    [Fact]
    public void ControllerOfflineWinServiceEx_ShouldCombineControllerOfflineWithWinService_Ex()
    {
        _sut.ControllerOfflineWinServiceEx.Should().Be($@"{TestBasePath}\NewAcesso\ControllerOffline\WinService_Ex");
    }

    [Fact]
    public void ControllerOfflineWinServiceIn_ShouldCombineControllerOfflineWithWinService_In()
    {
        _sut.ControllerOfflineWinServiceIn.Should().Be($@"{TestBasePath}\NewAcesso\ControllerOffline\WinService_In");
    }

    [Fact]
    public void WebAppDS_ShouldCombineNewAcessoRootWithWebAppDS()
    {
        _sut.WebAppDS.Should().Be($@"{TestBasePath}\NewAcesso\WebAppDS");
    }

    [Fact]
    public void WebAppUI_ShouldCombineNewAcessoRootWithWebAppUI()
    {
        _sut.WebAppUI.Should().Be($@"{TestBasePath}\NewAcesso\WebAppUI");
    }

    [Fact]
    public void GetBaseFolders_ShouldReturnAllNineFolders()
    {
        var folders = _sut.GetBaseFolders().ToList();

        folders.Should().HaveCount(9);
        folders.Should().Contain($@"{TestBasePath}\NewAcesso\AutoAtendimento");
        folders.Should().Contain($@"{TestBasePath}\NewAcesso\ConexBridge");
        folders.Should().Contain($@"{TestBasePath}\NewAcesso\ConnectionRecord");
        folders.Should().Contain($@"{TestBasePath}\NewAcesso\Controller");
        folders.Should().Contain($@"{TestBasePath}\NewAcesso\ControllerOffline");
        folders.Should().Contain($@"{TestBasePath}\NewAcesso\VisitAuthorization");
        folders.Should().Contain($@"{TestBasePath}\NewAcesso\WebAppDS");
        folders.Should().Contain($@"{TestBasePath}\NewAcesso\WebAppUI");
        folders.Should().Contain($@"{TestBasePath}\NewAcesso\Win");
    }

    [Fact]
    public void GetBaseFolders_ShouldNotContainNestedFolders()
    {
        var folders = _sut.GetBaseFolders().ToList();

        folders.Should().NotContain($@"{TestBasePath}\NewAcesso\Controller\ControleAcesso");
        folders.Should().NotContain($@"{TestBasePath}\NewAcesso\ControllerOffline\Arquivos");
    }

    [Fact]
    public void Paths_WithCustomInstallationPath_ShouldUseCustomPath()
    {
        var paths = new InstallationPaths(@"C:\SoftPrime", @"D:\Custom\Installers");

        paths.InstallationPath.Should().Be(@"D:\Custom\Installers");
    }

    [Fact]
    public void Paths_ShouldUseTrailingBasePathCorrectly()
    {
        var paths = new InstallationPaths(@"C:\SoftPrime\");

        paths.InstallationPath.Should().Be(@"C:\SoftPrime\Instaladores");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrWhiteSpace_ShouldThrowArgumentException(string? invalidBasePath)
    {
        var act = () => new InstallationPaths(invalidBasePath!);

        act.Should().Throw<ArgumentException>();
    }
}
