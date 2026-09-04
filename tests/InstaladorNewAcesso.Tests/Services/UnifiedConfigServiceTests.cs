using FluentAssertions;
using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Services;
using NSubstitute;

namespace InstaladorNewAcesso.Tests.Services;

public class UnifiedConfigServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly InstallationPaths _paths;
    private readonly IUIService _ui;

    public UnifiedConfigServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "UnifiedConfigTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        _paths = new InstallationPaths(_tempRoot);
        _ui = Substitute.For<IUIService>();

        // Create base directories
        Directory.CreateDirectory(_paths.ConnectionRecord);
        Directory.CreateDirectory(_paths.ControleAcesso);
        Directory.CreateDirectory(_paths.CoreWs);
        Directory.CreateDirectory(_paths.Task);
        Directory.CreateDirectory(_paths.ControllerOfflineWinServiceEx);
        Directory.CreateDirectory(_paths.ControllerOfflineWinServiceIn);
        Directory.CreateDirectory(_paths.WebAppUI);
        Directory.CreateDirectory(_paths.WebAppDS);
        Directory.CreateDirectory(_paths.Win);
        Directory.CreateDirectory(_paths.AutoAtendimentoWebAPI);
        Directory.CreateDirectory(_paths.AutoAtendimentoWebAPP);
    }

    [Fact]
    public async Task ConfigureAllAsync_WithValidPaths_ExecutesAllSteps()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_paths.ConnectionRecord, "PrimeAcesso.ConnectionRecord.exe.config"), @"<configuration><appSettings/></configuration>");
        File.WriteAllText(Path.Combine(_paths.ControleAcesso, "PrimeAcesso.ControleAcesso.ini"), @"[GERAL]");
        File.WriteAllText(Path.Combine(_paths.CoreWs, "NewAcesso.Controlador.Ws.exe.config"), @"<configuration><appSettings/></configuration>");
        File.WriteAllText(Path.Combine(_paths.CoreWs, "NewAcesso.Controlador.WatchDog.exe.config"), @"<configuration><appSettings/></configuration>");
        File.WriteAllText(Path.Combine(_paths.Task, "PrimeAcesso.Controller.Task.exe.config"), @"<configuration><appSettings/></configuration>");
        File.WriteAllText(Path.Combine(_paths.ControllerOfflineWinServiceEx, "PrimeAcesso.Controller.StandAloneEx.exe.config"), @"<configuration><appSettings/></configuration>");
        File.WriteAllText(Path.Combine(_paths.ControllerOfflineWinServiceIn, "PrimeAcesso.Controller.StandAloneIn.exe.config"), @"<configuration><appSettings/></configuration>");
        File.WriteAllText(Path.Combine(_paths.WebAppUI, "web.config"), @"<configuration><appSettings/></configuration>");
        File.WriteAllText(Path.Combine(_paths.WebAppDS, "web.config"), @"<configuration><appSettings/></configuration>");
        File.WriteAllText(Path.Combine(_paths.Win, "PrimeAcesso.Win.exe.config"), @"<configuration><appSettings/></configuration>");
        File.WriteAllText(Path.Combine(_paths.Win, "PrimeAcesso.Win.ini"), @"[GERAL]");
        File.WriteAllText(Path.Combine(_paths.AutoAtendimentoWebAPI, "appsettings.json"), @"{}");
        File.WriteAllText(Path.Combine(_paths.AutoAtendimentoWebAPP, "appsettings.json"), @"{}");

        var service = new UnifiedConfigService(_ui);
        var options = new UnifiedConfigOptions
        {
            IdConexao = "1",
            DsServiceUri = "http://localhost:8080/DSPrimeAcesso.svc",
            BiometricServer = "192.168.0.200",
            AutoAtendimentoDbConnectionString = "Server=sqlserver;Database=AutoAtendimento;User Id=sa;Password=secret;"
        };

        // Act
        var result = await service.ConfigureAllAsync(_paths, options);

        // Assert
        result.Should().BeTrue();

        // Verify AutoAtendimento WebAPI connection string
        var apiJson = File.ReadAllText(Path.Combine(_paths.AutoAtendimentoWebAPI, "appsettings.json"));
        apiJson.Should().Contain("AutoAtendimentoSqlServer");
        apiJson.Should().Contain("Server=sqlserver");
    }

    [Fact]
    public async Task ConfigureAllAsync_RealEnvironment_ExecutesAgainstSoftPrime()
    {
        if (!Directory.Exists(@"C:\SoftPrime\NewAcesso"))
            return;

        var realPaths = new InstallationPaths(@"C:\SoftPrime");
        var service = new UnifiedConfigService(_ui);
        var options = new UnifiedConfigOptions
        {
            IdConexao = "1",
            DbPath = @"C:\SoftPrime\NewAcesso\ConnectionRecord\DataBase\NewAcessoConnection.s3db",
            DsServiceUri = "http://localhost:8080/DSPrimeAcesso.svc",
            BiometricServer = "localhost",
            FabricanteFacial = "TopData",
            HoraExclusaoFacial = "17:00",
            AutoAtendimentoApiUrl = "http://localhost:8082",
            AutoAtendimentoUiUrl = "http://localhost:8081"
        };

        var result = await service.ConfigureAllAsync(realPaths, options);
        result.Should().BeTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, true); }
            catch { }
        }
    }
}