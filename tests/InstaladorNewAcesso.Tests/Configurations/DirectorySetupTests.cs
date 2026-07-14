using FluentAssertions;
using InstaladorNewAcesso.Core.Configurations;
using InstaladorNewAcesso.Abstractions.Models;

namespace InstaladorNewAcesso.Tests.Configurations;

public class DirectorySetupTests
{
    private const string TestBasePath = @"C:\SoftPrime";
    private readonly InstallationPaths _paths = new(TestBasePath);
    private readonly DirectorySetup _sut = new();

    [Fact]
    public void GetAllPaths_ShouldIncludeAllBaseFolders()
    {
        var paths = DirectorySetup.GetAllPaths(_paths).ToList();

        paths.Should().Contain($@"{TestBasePath}\NewAcesso\AutoAtendimento");
        paths.Should().Contain($@"{TestBasePath}\NewAcesso\ConexBridge");
        paths.Should().Contain($@"{TestBasePath}\NewAcesso\ConnectionRecord");
        paths.Should().Contain($@"{TestBasePath}\NewAcesso\Controller");
        paths.Should().Contain($@"{TestBasePath}\NewAcesso\ControllerOffline");
        paths.Should().Contain($@"{TestBasePath}\NewAcesso\VisitAuthorization");
        paths.Should().Contain($@"{TestBasePath}\NewAcesso\WebAppDS");
        paths.Should().Contain($@"{TestBasePath}\NewAcesso\WebAppUI");
        paths.Should().Contain($@"{TestBasePath}\NewAcesso\Win");
    }

    [Fact]
    public void GetAllPaths_ShouldIncludeNestedFoldersUnderController()
    {
        var paths = DirectorySetup.GetAllPaths(_paths).ToList();

        paths.Should().Contain($@"{TestBasePath}\NewAcesso\Controller\ControleAcesso");
        paths.Should().Contain($@"{TestBasePath}\NewAcesso\Controller\CoreWs");
        paths.Should().Contain($@"{TestBasePath}\NewAcesso\Controller\Fabricantes");
        paths.Should().Contain($@"{TestBasePath}\NewAcesso\Controller\Task");
    }

    [Fact]
    public void GetAllPaths_ShouldIncludeNestedFoldersUnderControllerOffline()
    {
        var paths = DirectorySetup.GetAllPaths(_paths).ToList();

        paths.Should().Contain($@"{TestBasePath}\NewAcesso\ControllerOffline\Arquivos");
        paths.Should().Contain($@"{TestBasePath}\NewAcesso\ControllerOffline\WinService_Ex");
        paths.Should().Contain($@"{TestBasePath}\NewAcesso\ControllerOffline\WinService_In");
    }

    [Fact]
    public void GetAllPaths_ShouldIncludeFabricantesUnderWebAppUI()
    {
        var paths = DirectorySetup.GetAllPaths(_paths).ToList();

        paths.Should().Contain($@"{TestBasePath}\NewAcesso\WebAppUI\Fabricantes");
    }

    [Fact]
    public void GetAllPaths_ShouldHaveCorrectTotalCount()
    {
        // 9 base folders + 4 Controller children + 3 ControllerOffline children + 1 WebAppUI child = 17
        var paths = DirectorySetup.GetAllPaths(_paths).ToList();

        paths.Should().HaveCount(17);
    }

    [Fact]
    public void GetAllPaths_ShouldNotContainDuplicatePaths()
    {
        var paths = DirectorySetup.GetAllPaths(_paths).ToList();

        paths.Should().OnlyHaveUniqueItems();
    }
}
