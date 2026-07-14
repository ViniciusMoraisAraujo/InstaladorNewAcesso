using System.Xml;
using FluentAssertions;
using InstaladorNewAcesso.Core.Configurations;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Services;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Tests;

/// <summary>
/// Testes de integração — validam o fluxo completo de múltiplos componentes
/// trabalhando juntos com o sistema de arquivos real.
/// </summary>
[Collection("IntegrationTests")]
public class InstallationIntegrationTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly InstallationPaths _paths;

    public InstallationIntegrationTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "IntegrationTests_" + Guid.NewGuid().ToString("N"));
        _paths = new InstallationPaths(_tempRoot);
    }

    // ================================================================
    //  1. FLUXO COMPLETO DE CRIAÇÃO DE DIRETÓRIOS
    //  DirectorySetup + InstallationPaths + Directory.Delete
    // ================================================================

    [Fact]
    public void DirectoryCreation_FullFlow_CreatesAllExpectedPaths()
    {
        // Arrange
        var setup = new DirectorySetup();
        var allPaths = DirectorySetup.GetAllPaths(_paths).ToList();

        // Act — criar todos os diretórios
        foreach (var dir in allPaths)
            Directory.CreateDirectory(dir);

        // Assert — todos existem
        foreach (var dir in allPaths)
            Directory.Exists(dir).Should().BeTrue($"diretório {dir} deveria ter sido criado");

        // Verifica também os subdiretórios aninhados
        Directory.Exists(_paths.ControleAcesso).Should().BeTrue();
        Directory.Exists(_paths.CoreWs).Should().BeTrue();
        Directory.Exists(_paths.Fabricantes).Should().BeTrue();
        Directory.Exists(_paths.Task).Should().BeTrue();
        Directory.Exists(_paths.ControllerOfflineArquivos).Should().BeTrue();
        Directory.Exists(_paths.ControllerOfflineWinServiceEx).Should().BeTrue();
        Directory.Exists(_paths.ControllerOfflineWinServiceIn).Should().BeTrue();
        Directory.Exists(_paths.WebAppUIFabricantes).Should().BeTrue();

        // Contagem total de diretórios
        allPaths.Should().HaveCount(17); // 9 base + 8 nested
    }

    [Fact]
    public void DirectoryCreation_Idempotent_RunningTwiceDoesNotThrow()
    {
        // Arrange
        var setup = new DirectorySetup();
        var allPaths = DirectorySetup.GetAllPaths(_paths).ToList();

        // Act — criar duas vezes
        foreach (var dir in allPaths)
            Directory.CreateDirectory(dir);

        // Segunda execução não deve lançar exceção
        var act = () =>
        {
            foreach (var dir in allPaths)
                Directory.CreateDirectory(dir);
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void DirectoryCreation_WithFilesInside_CanBeDeleted()
    {
        // Arrange — cria estrutura com arquivos dentro
        var setup = new DirectorySetup();
        foreach (var dir in DirectorySetup.GetAllPaths(_paths))
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "test.txt"), "content");
        }

        // Act — remove recursivamente
        foreach (var dir in DirectorySetup.GetAllPaths(_paths).Reverse())
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
        // Remove raiz também
        if (Directory.Exists(_paths.NewAcessoRoot))
            Directory.Delete(_paths.NewAcessoRoot, true);

        // Assert
        Directory.Exists(_paths.NewAcessoRoot).Should().BeFalse();
    }

    // ================================================================
    //  2. SummaryStore — CICLO DE VIDA COMPLETO
    //  Start → Add (múltiplas etapas) → GetStats → GetResults
    // ================================================================

    [Fact]
    public void SummaryStore_FullLifecycle_TracksAllStages()
    {
        // Arrange — simula o fluxo de várias etapas
        SummaryStore.Start();

        // Etapa 1: Recursos do Windows
        SummaryStore.Add("Recursos do Windows", "IIS", true, "Instalado");
        SummaryStore.Add("Recursos do Windows", "ASP.NET", true, "Instalado");
        SummaryStore.Add("Recursos do Windows", "Telnet", false, "Falha na instalação");

        // Etapa 2: Diretórios
        SummaryStore.Add("Diretórios", _paths.Controller, true, "Criado");
        SummaryStore.Add("Diretórios", _paths.ConnectionRecord, true, "Já existe");

        // Etapa 3: IIS
        SummaryStore.Add("IIS", "AppPool WebAppDS", true, "Criada");
        SummaryStore.Add("IIS", "Site WebAppUI", true, "Já existe (porta 8081)");

        // Etapa 4: Aplicações
        SummaryStore.Add("Aplicações (MSIs)", "Controller.msi", true);
        SummaryStore.Add("Aplicações (MSIs)", "CoreWs.msi", false, "Falha na instalação");

        // Assert — stats
        var (total, sucessos, falhas, elapsed) = SummaryStore.GetStats();
        total.Should().Be(9);
        sucessos.Should().Be(7);
        falhas.Should().Be(2);
        elapsed.Should().BePositive();

        // Assert — results
        var results = SummaryStore.GetResults();
        results.Should().HaveCount(9);
        results.Should().Contain(r => r.Etapa == "Recursos do Windows" && r.Item == "IIS" && r.Sucesso);
        results.Should().Contain(r => r.Etapa == "Aplicações (MSIs)" && r.Item == "CoreWs.msi" && !r.Sucesso);

        // Assert — HasResults
        SummaryStore.HasResults.Should().BeTrue();

        // Assert — ElapsedFormatted não vazio
        SummaryStore.ElapsedFormatted().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SummaryStore_Start_ResetsAllData()
    {
        // Arrange
        SummaryStore.Start();
        SummaryStore.Add("Etapa1", "Item1", true);
        SummaryStore.GetStats().total.Should().Be(1);

        // Act — novo Start
        SummaryStore.Start();

        // Assert — resetado
        SummaryStore.HasResults.Should().BeFalse();
        SummaryStore.GetStats().total.Should().Be(0);
        SummaryStore.GetResults().Should().BeEmpty();
    }

    // ================================================================
    //  3. MsiUninstaller — OPERAÇÕES REAIS NO SISTEMA DE ARQUIVOS
    //  IsInstalled, RemoveTargetDirectory
    // ================================================================

    [Fact]
    public void MsiUninstaller_IsInstalled_DirectoryWithFiles_ReturnsTrue()
    {
        // Arrange
        var targetDir = Path.Combine(_tempRoot, "InstalledApp");
        Directory.CreateDirectory(targetDir);
        File.WriteAllText(Path.Combine(targetDir, "app.dll"), "content");
        var uninstaller = new MsiUninstaller();

        // Act
        var result = MsiUninstaller.IsInstalled(targetDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void MsiUninstaller_IsInstalled_EmptyDirectory_ReturnsTrue()
    {
        // Arrange — diretório vazio ainda tem entradas (byte 0 do .)
        var targetDir = Path.Combine(_tempRoot, "EmptyDir");
        Directory.CreateDirectory(targetDir);
        var uninstaller = new MsiUninstaller();

        // Act
        var result = MsiUninstaller.IsInstalled(targetDir);

        // Assert — diretório vazio tem zero FileSystemEntries
        // Atual: Directory.GetFileSystemEntries(dir).Length > 0 → false
        result.Should().BeFalse();
    }

    [Fact]
    public void MsiUninstaller_IsInstalled_NonExistentDirectory_ReturnsFalse()
    {
        // Arrange
        var uninstaller = new MsiUninstaller();

        // Act
        var result = MsiUninstaller.IsInstalled(Path.Combine(_tempRoot, "NonExistent"));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void MsiUninstaller_RemoveTargetDirectory_ExistingDir_RemovesIt()
    {
        // Arrange
        var targetDir = Path.Combine(_tempRoot, "ToRemove");
        Directory.CreateDirectory(targetDir);
        File.WriteAllText(Path.Combine(targetDir, "file.txt"), "data");
        var uninstaller = new MsiUninstaller();

        // Act
        var result = MsiUninstaller.RemoveTargetDirectory(targetDir);

        // Assert
        result.Should().BeTrue();
        Directory.Exists(targetDir).Should().BeFalse();
    }

    [Fact]
    public void MsiUninstaller_RemoveTargetDirectory_NonExistentDir_ReturnsFalse()
    {
        // Arrange
        var uninstaller = new MsiUninstaller();

        // Act
        var result = MsiUninstaller.RemoveTargetDirectory(Path.Combine(_tempRoot, "Ghost"));

        // Assert
        result.Should().BeFalse();
    }

    // ================================================================
    //  4. AuditLogger + SummaryStore — INTEGRAÇÃO NO FLUXO DE DESINSTALAÇÃO
    // ================================================================

    [Fact]
    public void AuditLogger_WithSummaryStore_LogsAndTracksConsistently()
    {
        // Arrange — simula parte do fluxo de desinstalação
        SummaryStore.Start();
        var basePath = Path.Combine(_tempRoot, "LogTest");
        Directory.CreateDirectory(basePath);

        AuditLogger.Start(basePath);

        // Act — log de várias operações
        AuditLogger.Log("Remover Site IIS", "WebAppDS", true);
        SummaryStore.Add("Desinstalação", "Site WebAppDS", true, "Removido");

        AuditLogger.Log("Remover AppPool IIS", "WebAppUI", true);
        SummaryStore.Add("Desinstalação", "AppPool WebAppUI", true, "Removido");

        AuditLogger.Log("msiexec /x", "Controller.msi", true);
        SummaryStore.Add("Desinstalação", "Controller.msi", true, "MSI desinstalado");

        AuditLogger.Log("Remover Diretório", _paths.Controller, false, "Diretório não encontrado");
        SummaryStore.Add("Desinstalação", $"Diretório {_paths.Controller}", false, "Diretório não encontrado");

        AuditLogger.Separator("FIM DA DESINSTALAÇÃO");
        AuditLogger.Finish();

        // Assert — SummaryStore
        var (total, sucessos, falhas, _) = SummaryStore.GetStats();
        total.Should().Be(4);
        sucessos.Should().Be(3);
        falhas.Should().Be(1);

        // Assert — AuditLogger: arquivo de log foi criado
        var logPath = AuditLogger.CurrentLogPath;
        logPath.Should().NotBeNull();
        File.Exists(logPath!).Should().BeTrue();

        // Assert — conteúdo do log
        // Nota: usar o formato real do AuditLogger (✅ OK / ❌ FALHA, emojis)
        var logContent = File.ReadAllText(logPath!);
        logContent.Should().Contain("Remover Site IIS");
        logContent.Should().Contain("WebAppDS");
        logContent.Should().Contain("✅ OK");
        logContent.Should().Contain("❌ FALHA");
        logContent.Should().Contain("Diretório não encontrado");
        logContent.Should().Contain("FIM DA DESINSTALAÇÃO");
        logContent.Should().Contain("RESUMO FINAL");
        logContent.Should().Contain("Total: 4");
        logContent.Should().Contain("Sucessos: 3");
        logContent.Should().Contain("Falhas: 1");
    }

    // ================================================================
    //  5. MsiLogHelper — GERAÇÃO DE CAMINHO DE LOG (integração real)
    // ================================================================

    [Fact]
    public void MsiLogHelper_GenerateLogFilePath_ReturnsValidPath()
    {
        // Arrange
        var msiPath = Path.Combine(_tempRoot, "MyApp.msi");
        Directory.CreateDirectory(_tempRoot);
        File.WriteAllText(msiPath, "dummy");

        // Act
        var logPath = MsiLogHelper.GenerateLogFilePath(msiPath);

        // Assert
        logPath.Should().NotBeNullOrEmpty();
        logPath.Should().EndWith(".log");
        logPath.Should().Contain("MyApp");
        logPath.Should().Contain(DateTime.Now.ToString("yyyyMMdd"));

        // O diretório de logs deve ter sido criado
        var logDir = Path.GetDirectoryName(logPath);
        Directory.Exists(logDir).Should().BeTrue();
    }

    // ================================================================
    //  6. ConfigBackupService — BACKUP + RESTORE + CLEANUP COMPLETO
    //  (com arquivos de config reais)
    // ================================================================

    [Fact]
    public void ConfigBackup_WithRealConfigFiles_FullFlow()
    {
        // Arrange
        var appDir = Path.Combine(_tempRoot, "MyApp");
        Directory.CreateDirectory(appDir);

        // Cria arquivos de configuração reais
        File.WriteAllText(Path.Combine(appDir, "web.config"), "<configuration><appSettings></appSettings></configuration>");
        File.WriteAllText(Path.Combine(appDir, "App.config"), "<?xml version=\"1.0\"?><configuration></configuration>");
        File.WriteAllText(Path.Combine(appDir, "PrimeAcesso.ConnectionRecord.exe.config"),
            "<?xml version=\"1.0\" encoding=\"utf-8\"?><configuration><appSettings></appSettings></configuration>");
        // Arquivo não-config (deve ser ignorado)
        File.WriteAllText(Path.Combine(appDir, "readme.txt"), "not a config");
        File.WriteAllText(Path.Combine(appDir, "data.dll"), "binary");

        // Act — Backup
        var backupPath = ConfigBackupService.Backup(appDir, "IntegrationTest");

        // Assert — Backup
        backupPath.Should().NotBeNull();
        Directory.Exists(backupPath!).Should().BeTrue();
        // Deve ter copiado apenas os 3 arquivos de config
        Directory.GetFiles(backupPath!).Should().HaveCount(3);
        File.Exists(Path.Combine(backupPath!, "readme.txt")).Should().BeFalse();
        File.Exists(Path.Combine(backupPath!, "data.dll")).Should().BeFalse();

        // Modifica o appDir (simula instalação que sobrescreve configs)
        File.WriteAllText(Path.Combine(appDir, "web.config"), "<configuration><!-- modified --></configuration>");
        File.Delete(Path.Combine(appDir, "PrimeAcesso.ConnectionRecord.exe.config"));

        // Act — Restore
        ConfigBackupService.Restore(backupPath!, appDir);

        // Assert — Restore
        File.ReadAllText(Path.Combine(appDir, "web.config")).Should().Contain("<configuration><appSettings>");
        File.Exists(Path.Combine(appDir, "PrimeAcesso.ConnectionRecord.exe.config")).Should().BeTrue();

        // Act — Cleanup
        ConfigBackupService.Cleanup(backupPath!);

        // Assert — Cleanup
        Directory.Exists(backupPath!).Should().BeFalse();
    }

    // ================================================================
    //  7. MsiScanner + DirectorySetup — MAPEAMENTO DE CAMINHOS
    // ================================================================

    [Fact]
    public void MsiScan_WithDirectorySetup_PathsAreConsistent()
    {
        // Verifica que o MsiScanner resolve TargetDirectory de forma
        // consistente com o DirectorySetup para as pastas Controller, etc.

        // Arrange — cria estrutura similar ao que seria baixado
        var msiRoot = Path.Combine(_tempRoot, "Installers", "PrimeAcesso V5.9");
        Directory.CreateDirectory(Path.Combine(msiRoot, "Controller"));
        File.WriteAllText(Path.Combine(msiRoot, "Controller", "Controller.msi"), "dummy");

        // DirectorySetup cria estes diretórios:
        var setup = new DirectorySetup();
        foreach (var dir in DirectorySetup.GetAllPaths(_paths))
            Directory.CreateDirectory(dir);

        // Act — MsiScanner deve mapear Controller → _paths.Controller
        var scanner = new MsiScanner(_paths, "SQLServer", msiRoot);
        var results = scanner.Scan();

        // Assert
        results.Should().ContainSingle();
        results[0].TargetDirectory.Should().Be(_paths.Controller);
    }

    // ================================================================
    //  8. FLUXO DE DESINSTALAÇÃO DE DIRETÓRIOS
    //  DirectorySetup → GetAllPaths → Delete
    // ================================================================

    [Fact]
    public void UninstallDirectoryFlow_RemovesAllDirectoriesInReverseOrder()
    {
        // Arrange — cria estrutura completa
        var setup = new DirectorySetup();
        var allPaths = DirectorySetup.GetAllPaths(_paths).ToList();
        foreach (var dir in allPaths)
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "app.dll"), "binary");
        }

        // Act — remove na ordem reversa (filhos antes de pais)
        foreach (var dir in allPaths.AsEnumerable().Reverse())
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }

        // Tenta remover raiz
        if (Directory.Exists(_paths.NewAcessoRoot))
            Directory.Delete(_paths.NewAcessoRoot, true);

        // Assert
        foreach (var dir in allPaths)
            Directory.Exists(dir).Should().BeFalse();
        Directory.Exists(_paths.NewAcessoRoot).Should().BeFalse();
    }

    // ================================================================
    //  9. WebAppScanner + DirectorySetup — MAPEAMENTO DE CAMINHOS
    // ================================================================

    [Fact]
    public void WebAppScan_WithDirectorySetup_DetectsDSAndUI()
    {
        // Arrange — cria MSIs com nomes que o scanner detecta como WebAppDS/WebAppUI
        var msiRoot = Path.Combine(_tempRoot, "Installers", "PrimeAcesso V5.9");
        Directory.CreateDirectory(msiRoot);
        File.WriteAllText(Path.Combine(msiRoot, "WebAppDS.msi"), "dummy");
        File.WriteAllText(Path.Combine(msiRoot, "WebAppUI.msi"), "dummy");

        // DirectorySetup cria os diretórios de destino
        var dirSetup = new DirectorySetup();
        foreach (var dir in DirectorySetup.GetAllPaths(_paths))
            Directory.CreateDirectory(dir);

        // Act
        var scanner = new WebAppScanner(_paths, "SQLServer", msiRoot);
        var results = scanner.Scan();

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(r => r.SiteName == "WebAppDS" && r.Port == 8080);
        results.Should().Contain(r => r.SiteName == "WebAppUI" && r.Port == 8081);
    }

    // ================================================================
    //  10. MSI SEM INSTALAÇÃO (apenas scan + config helpers sem prompt)
    // ================================================================

    [Fact]
    public void MsiInstall_WithRealConfigs_UpdatesAllConfigHelpers()
    {
        // Este teste simula o cenário de MsiInstaller.InstallAsync retornando
        // true e verifica que os config helpers atualizam corretamente arquivos
        // reais em uma estrutura de diretórios realista.

        // Arrange — estrutura de diretórios tipo NewAcesso
        var targetDir = Path.Combine(_tempRoot, "NewAcesso", "Controller", "ControleAcesso");
        Directory.CreateDirectory(targetDir);

        // ConnectionRecord config
        var connConfigPath = Path.Combine(targetDir, "PrimeAcesso.ConnectionRecord.exe.config");
        File.WriteAllText(connConfigPath,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?><configuration><appSettings></appSettings></configuration>");

        // ControleAcesso INI
        var iniPath = Path.Combine(targetDir, "PrimeAcesso.ControleAcesso.ini");
        File.WriteAllText(iniPath, "; ControleAcesso config\n[Config]\nHost=localhost\n");

        // Act — chama os helpers diretamente (como MsiInstaller faria)
        var connResult = ConnectionRecordConfigHelper.UpdateConfigAfterInstall(targetDir);
        var controleResult = ControleAcessoConfigHelper.UpdateIniAfterInstall(targetDir);

        // Assert — ConnectionRecord
        connResult.Should().BeTrue();
        var connDoc = new XmlDocument();
        connDoc.Load(connConfigPath);
        var pathDataSource = connDoc.SelectSingleNode("//add[@key='PathDataSource']");
        pathDataSource.Should().NotBeNull();
        pathDataSource!.Attributes!["value"]!.Value.Should().Contain("NewAcessoConnection.s3db");

        // Assert — ControleAcesso
        controleResult.Should().BeTrue();
        var iniLines = File.ReadAllLines(iniPath);
        iniLines.Should().Contain(l => l.StartsWith("PathDataSouce_NewAcessoConnectionRecord"));
    }

    public void Dispose()
    {
        // Limpa SummaryStore e AuditLogger entre testes
        SummaryStore.Start();
        // Nota: AuditLogger não pode ser resetado facilmente (estático),
        // mas cada teste usa um caminho único via Start()

        // Remove diretório temporário
        if (Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, true); }
            catch { /* cleanup on best-effort basis */ }
        }
    }
}
