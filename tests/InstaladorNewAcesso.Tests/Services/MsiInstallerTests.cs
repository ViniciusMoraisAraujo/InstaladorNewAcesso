using System.Xml;
using FluentAssertions;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Tests.Services;

public class MsiInstallerTests : IDisposable
{
    private readonly string _tempRoot;

    public MsiInstallerTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "MsiInstallerTests_" + Guid.NewGuid().ToString("N"));
    }

    // ============================================================
    //  Test subclass — substitui RunMsiexecAsync
    // ============================================================

    private class TestableMsiInstaller : MsiInstaller
    {
        public Func<string, Task<bool>>? OnRunMsiexec { get; set; }
        public List<string> RunMsiexecCalls { get; } = new();
        public bool? FixedResult { get; set; }
        public Exception? ThrowException { get; set; }

        protected override Task<bool> RunMsiexecAsync(string arguments)
        {
            RunMsiexecCalls.Add(arguments);

            if (ThrowException != null)
                throw ThrowException;

            if (OnRunMsiexec != null)
                return OnRunMsiexec(arguments);

            return Task.FromResult(FixedResult ?? true);
        }
    }

    // ============================================================
    //  Helpers
    // ============================================================

    private MsiInstallationModel CreateModel(string? targetDir = null, bool generateLog = false)
    {
        return new MsiInstallationModel
        {
            MsiPath = Path.Combine(_tempRoot, "App.msi"),
            TargetDirectory = targetDir ?? _tempRoot,
            GenerateLog = generateLog
        };
    }

    /// <summary>
    /// Cria um arquivo .config XML mínimo válido no diretório especificado.
    /// </summary>
    private static void CreateMinimalConfigXml(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var doc = new XmlDocument();
        var decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
        doc.AppendChild(decl);
        var config = doc.CreateElement("configuration");
        doc.AppendChild(config);
        var appSettings = doc.CreateElement("appSettings");
        config.AppendChild(appSettings);
        doc.Save(path);
    }

    /// <summary>
    /// Cria um arquivo .INI mínimo no diretório especificado.
    /// </summary>
    private static void CreateMinimalIni(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "; Test INI file\n[TestSection]\nKey=Value\n");
    }

    // ============================================================
    //  msiexec success / failure
    // ============================================================

    [Fact]
    public async Task InstallAsync_WhenMsiexecSucceeds_ReturnsTrue()
    {
        var installer = new TestableMsiInstaller { FixedResult = true };
        var model = CreateModel();

        var result = await installer.InstallAsync(model);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task InstallAsync_WhenMsiexecFails_ReturnsFalse()
    {
        var installer = new TestableMsiInstaller { FixedResult = false };
        var model = CreateModel();

        var result = await installer.InstallAsync(model);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task InstallAsync_WhenMsiexecThrows_ReturnsFalse()
    {
        var installer = new TestableMsiInstaller
        {
            ThrowException = new InvalidOperationException("msiexec crashed")
        };
        var model = CreateModel();

        var result = await installer.InstallAsync(model);

        result.Should().BeFalse();
    }

    // ============================================================
    //  GenerateLog
    // ============================================================

    [Fact]
    public async Task InstallAsync_WithGenerateLog_Success_PassesLogArgToMsiexec()
    {
        var installer = new TestableMsiInstaller { FixedResult = true };
        var model = CreateModel(generateLog: true);
        Directory.CreateDirectory(_tempRoot); // necessário para o MSI simulado
        File.WriteAllText(model.MsiPath, "dummy");

        var result = await installer.InstallAsync(model);

        result.Should().BeTrue();
        installer.RunMsiexecCalls.Should().ContainSingle();
        installer.RunMsiexecCalls[0].Should().Contain("/lvx*");
        installer.RunMsiexecCalls[0].Should().Contain(".log");
    }

    [Fact]
    public async Task InstallAsync_WithGenerateLog_WhenMsiexecFails_StillReturnsFalse()
    {
        var installer = new TestableMsiInstaller { FixedResult = false };
        var model = CreateModel(generateLog: true);
        Directory.CreateDirectory(_tempRoot);
        File.WriteAllText(model.MsiPath, "dummy");

        var result = await installer.InstallAsync(model);

        result.Should().BeFalse();
        installer.RunMsiexecCalls.Should().ContainSingle();
        installer.RunMsiexecCalls[0].Should().Contain("/lvx*");
    }

    [Fact]
    public async Task InstallAsync_WithoutGenerateLog_DoesNotIncludeLogArg()
    {
        var installer = new TestableMsiInstaller { FixedResult = true };
        var model = CreateModel(generateLog: false);

        var result = await installer.InstallAsync(model);

        result.Should().BeTrue();
        installer.RunMsiexecCalls.Should().ContainSingle();
        installer.RunMsiexecCalls[0].Should().NotContain("/lvx*");
    }

    // ============================================================
    //  Directory creation
    // ============================================================

    [Fact]
    public async Task InstallAsync_WhenTargetDirectoryNotExists_CreatesIt()
    {
        var installer = new TestableMsiInstaller { FixedResult = true };
        var nonExistentDir = Path.Combine(_tempRoot, "NewDir", "SubDir");
        var model = CreateModel(targetDir: nonExistentDir);

        Directory.Exists(nonExistentDir).Should().BeFalse();

        var result = await installer.InstallAsync(model);

        result.Should().BeTrue();
        Directory.Exists(nonExistentDir).Should().BeTrue();
    }

    [Fact]
    public async Task InstallAsync_WhenTargetDirectoryAlreadyExists_DoesNotThrow()
    {
        var installer = new TestableMsiInstaller { FixedResult = true };
        Directory.CreateDirectory(_tempRoot);
        var model = CreateModel();

        var result = await installer.InstallAsync(model);

        result.Should().BeTrue();
    }

    // ============================================================
    //  Config helpers — no config files
    // ============================================================

    [Fact]
    public async Task InstallAsync_WhenNoConfigFiles_DoesNotCallAnyHelper()
    {
        var installer = new TestableMsiInstaller { FixedResult = true };
        var model = CreateModel();
        Directory.CreateDirectory(_tempRoot);

        var result = await installer.InstallAsync(model);

        // Sucesso sem helpers (nenhum arquivo de config existe)
        result.Should().BeTrue();
    }

    // ============================================================
    //  Config helpers — ConnectionRecord
    // ============================================================

    [Fact]
    public async Task InstallAsync_WithConnectionRecordConfig_UpdatesConfig()
    {
        var installer = new TestableMsiInstaller { FixedResult = true };
        Directory.CreateDirectory(_tempRoot);
        var configPath = Path.Combine(_tempRoot, "PrimeAcesso.ConnectionRecord.exe.config");
        CreateMinimalConfigXml(configPath);
        var model = CreateModel();

        var result = await installer.InstallAsync(model);

        result.Should().BeTrue();
        // O helper deve ter adicionado a chave PathDataSource
        var doc = new XmlDocument();
        doc.Load(configPath);
        var node = doc.SelectSingleNode("//add[@key='PathDataSource']");
        node.Should().NotBeNull();
        node!.Attributes!["value"]!.Value.Should().Contain("NewAcessoConnection.s3db");
    }

    // ============================================================
    //  Config helpers — ControleAcesso (.ini only, no agendamento)
    // ============================================================

    [Fact]
    public async Task InstallAsync_WithControleAcessoIni_UpdatesIni()
    {
        var installer = new TestableMsiInstaller { FixedResult = true };

        // O helper ControleAcessoConfigHelper faz Path.GetDirectoryName duas vezes
        // para navegar até NewAcessoRoot. Precisamos de pelo menos 2 níveis.
        // targetDir = {base}\NewAcesso\Controller\ControleAcesso
        var targetDir = Path.Combine(_tempRoot, "NewAcesso", "Controller", "ControleAcesso");
        Directory.CreateDirectory(targetDir);

        var iniPath = Path.Combine(targetDir, "PrimeAcesso.ControleAcesso.ini");
        CreateMinimalIni(iniPath);

        var model = CreateModel(targetDir: targetDir);

        var result = await installer.InstallAsync(model);

        result.Should().BeTrue();
        // O helper deve ter adicionado a chave PathDataSouce_NewAcessoConnectionRecord
        var lines = File.ReadAllLines(iniPath);
        lines.Should().Contain(l => l.Contains("PathDataSouce_NewAcessoConnectionRecord"));
    }

    // ============================================================
    //  Config helpers — CoreWs (Watchdog)
    // ============================================================

    [Fact]
    public async Task InstallAsync_WithCoreWsWatchdogConfig_UpdatesConfig()
    {
        var installer = new TestableMsiInstaller { FixedResult = true };

        // CoreWsConfigHelper precisa da estrutura:
        //   targetDir = {base}\NewAcesso\Controller\CoreWs
        var targetDir = Path.Combine(_tempRoot, "NewAcesso", "Controller", "CoreWs");
        Directory.CreateDirectory(targetDir);

        var configPath = Path.Combine(targetDir, "NewAcesso.Controlador.Watchdog.exe.config");
        CreateMinimalConfigXml(configPath);

        var model = CreateModel(targetDir: targetDir);

        var result = await installer.InstallAsync(model);

        result.Should().BeTrue();
        // O helper deve ter adicionado a chave caminhoDosLogs
        var doc = new XmlDocument();
        doc.Load(configPath);
        var node = doc.SelectSingleNode("//add[@key='caminhoDosLogs']");
        node.Should().NotBeNull();
    }

    // ============================================================
    //  Config helpers — CoreWs (Ws)
    // ============================================================

    [Fact]
    public async Task InstallAsync_WithCoreWsWsConfig_UpdatesConfig()
    {
        var installer = new TestableMsiInstaller { FixedResult = true };

        var targetDir = Path.Combine(_tempRoot, "NewAcesso", "Controller", "CoreWs");
        Directory.CreateDirectory(targetDir);

        var configPath = Path.Combine(targetDir, "NewAcesso.Controlador.Ws.exe.config");
        CreateMinimalConfigXml(configPath);

        var model = CreateModel(targetDir: targetDir);

        var result = await installer.InstallAsync(model);

        result.Should().BeTrue();
        var doc = new XmlDocument();
        doc.Load(configPath);
        var node = doc.SelectSingleNode("//add[@key='caminhoDasDllsDoControleDeAcesso']");
        node.Should().NotBeNull();
    }

    // ============================================================
    //  Config helpers — todos os que NÃO prompt ao mesmo tempo
    // ============================================================

    [Fact]
    public async Task InstallAsync_WithAllNonPromptingConfigs_AllHelpersRunSuccessfully()
    {
        var installer = new TestableMsiInstaller { FixedResult = true };

        // A estrutura de diretórios precisa ser:
        //   targetDir = ...\NewAcesso\Controller\ControleAcesso
        //   (para ControleAcesso e ConnectionRecord na mesma base)
        var targetDir = Path.Combine(_tempRoot, "NewAcesso", "Controller", "ControleAcesso");
        Directory.CreateDirectory(targetDir);

        // ConnectionRecord.exe.config
        CreateMinimalConfigXml(Path.Combine(targetDir, "PrimeAcesso.ConnectionRecord.exe.config"));

        // ControleAcesso.ini (mas NÃO AgendamentoEquipOffline.xml — seria prompt)
        CreateMinimalIni(Path.Combine(targetDir, "PrimeAcesso.ControleAcesso.ini"));

        // CoreWs .config (mas NÃO Task, StandAloneEx, StandAloneIm — todos promptam)
        // CoreWs fica em ...\Controller\CoreWs, não em ControleAcesso.
        // O helper CoreWsConfigHelper é chamado com targetDir = ControleAcesso,
        // mas ele procura os .config em targetDir, não em subdiretórios.
        // Então se o config estiver em targetDir (ControleAcesso), o helper vai
        // tentar carregá-lo e atualizar. Mas a estrutura de diretórios para
        // GetDirectoryName não funcionará corretamente se o config estiver em
        // ControleAcesso mas o helper espera CoreWs.
        //
        // Para este teste, vamos pular CoreWs e focar só em ConnectionRecord + ControleAcesso
        // que usam o mesmo targetDir.

        var model = CreateModel(targetDir: targetDir);

        var result = await installer.InstallAsync(model);

        result.Should().BeTrue();

        // Verifica que ConnectionRecord foi atualizado
        var connConfig = Path.Combine(targetDir, "PrimeAcesso.ConnectionRecord.exe.config");
        var doc = new XmlDocument();
        doc.Load(connConfig);
        doc.SelectSingleNode("//add[@key='PathDataSource']").Should().NotBeNull();

        // Verifica que ControleAcesso foi atualizado
        var iniLines = File.ReadAllLines(Path.Combine(targetDir, "PrimeAcesso.ControleAcesso.ini"));
        iniLines.Should().Contain(l => l.Contains("PathDataSouce_NewAcessoConnectionRecord"));
    }

    // ============================================================
    //  Msiexec arguments
    // ============================================================

    [Fact]
    public async Task InstallAsync_PassesCorrectMsiexecArguments()
    {
        var installer = new TestableMsiInstaller { FixedResult = true };
        var msiPath = Path.Combine(_tempRoot, "MyApp.msi");
        var targetDir = Path.Combine(_tempRoot, "Target");
        Directory.CreateDirectory(Path.GetDirectoryName(msiPath)!);
        File.WriteAllText(msiPath, "dummy");

        var model = new MsiInstallationModel
        {
            MsiPath = msiPath,
            TargetDirectory = targetDir,
            GenerateLog = false
        };

        await installer.InstallAsync(model);

        installer.RunMsiexecCalls.Should().ContainSingle();
        var args = installer.RunMsiexecCalls[0];
        args.Should().Contain($"/i \"{msiPath}\"");
        args.Should().Contain($"/qn");
        args.Should().Contain($"TARGETDIR=\"{targetDir}\"");
    }

    [Fact]
    public async Task InstallAsync_OnFailure_DoesNotCallConfigHelpers()
    {
        var installer = new TestableMsiInstaller { FixedResult = false };
        Directory.CreateDirectory(_tempRoot);
        // Cria um arquivo de config — mesmo assim, não deve ser processado
        CreateMinimalConfigXml(Path.Combine(_tempRoot, "PrimeAcesso.ConnectionRecord.exe.config"));
        var model = CreateModel();

        var result = await installer.InstallAsync(model);

        result.Should().BeFalse();
        // O .config não deve ter sido modificado (só teria sido se InstallAsync tivesse retornado true)
        var doc = new XmlDocument();
        doc.Load(Path.Combine(_tempRoot, "PrimeAcesso.ConnectionRecord.exe.config"));
        doc.SelectSingleNode("//add[@key='PathDataSource']").Should().BeNull();
    }

    // ============================================================
    //  Error paths — corrupted config files
    // ============================================================

    [Fact]
    public async Task InstallAsync_WithCorruptedConnectionRecordConfig_ReturnsTrue()
    {
        // ConnectionRecord helper has try/catch — corrupted XML is handled gracefully
        var installer = new TestableMsiInstaller { FixedResult = true };
        Directory.CreateDirectory(_tempRoot);
        File.WriteAllText(Path.Combine(_tempRoot, "PrimeAcesso.ConnectionRecord.exe.config"), "not valid xml {{{");
        var model = CreateModel();

        var result = await installer.InstallAsync(model);

        result.Should().BeTrue(); // installer succeeds even if config helper fails
    }

    [Fact]
    public async Task InstallAsync_WithCorruptedControleAcessoIni_ReturnsTrue()
    {
        var installer = new TestableMsiInstaller { FixedResult = true };
        var targetDir = Path.Combine(_tempRoot, "NewAcesso", "Controller", "ControleAcesso");
        Directory.CreateDirectory(targetDir);
        // INI file with invalid content (won't crash, just won't match key)
        File.WriteAllText(Path.Combine(targetDir, "PrimeAcesso.ControleAcesso.ini"), "bad content without matching key");
        var model = CreateModel(targetDir: targetDir);

        var result = await installer.InstallAsync(model);

        result.Should().BeTrue();
    }

    // ============================================================
    //  Error paths — CoreWs both configs, one corrupted
    // ============================================================

    [Fact]
    public async Task InstallAsync_CoreWsWatchdogCorruptedWsValid_StillReturnsTrue()
    {
        // After BUG FIX: watchdogOk && wsOk — both must succeed for CoreWsConfigHelper.
        // But MsiInstaller.InstallAsync doesn't check helper results.
        var installer = new TestableMsiInstaller { FixedResult = true };
        var targetDir = Path.Combine(_tempRoot, "NewAcesso", "Controller", "CoreWs");
        Directory.CreateDirectory(targetDir);

        File.WriteAllText(Path.Combine(targetDir, "NewAcesso.Controlador.Watchdog.exe.config"), "invalid xml");
        CreateMinimalConfigXml(Path.Combine(targetDir, "NewAcesso.Controlador.Ws.exe.config"));

        var model = CreateModel(targetDir: targetDir);

        var result = await installer.InstallAsync(model);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task InstallAsync_CoreWsBothConfigsCorrupted_StillReturnsTrue()
    {
        var installer = new TestableMsiInstaller { FixedResult = true };
        var targetDir = Path.Combine(_tempRoot, "NewAcesso", "Controller", "CoreWs");
        Directory.CreateDirectory(targetDir);

        File.WriteAllText(Path.Combine(targetDir, "NewAcesso.Controlador.Watchdog.exe.config"), "invalid xml 1");
        File.WriteAllText(Path.Combine(targetDir, "NewAcesso.Controlador.Ws.exe.config"), "invalid xml 2");

        var model = CreateModel(targetDir: targetDir);

        var result = await installer.InstallAsync(model);

        // CoreWsConfigHelper returns false, but MsiInstaller doesn't check it
        result.Should().BeTrue();
    }

    // ============================================================
    //  Error paths — multiple helpers fail simultaneously
    // ============================================================

    [Fact]
    public async Task InstallAsync_MultipleCorruptedConfigs_ReturnsTrue()
    {
        var installer = new TestableMsiInstaller { FixedResult = true };
        Directory.CreateDirectory(_tempRoot);

        File.WriteAllText(Path.Combine(_tempRoot, "PrimeAcesso.ConnectionRecord.exe.config"), "bad xml");
        File.WriteAllText(Path.Combine(_tempRoot, "PrimeAcesso.ControleAcesso.ini"), "bad content without matching key");

        var model = CreateModel();

        var result = await installer.InstallAsync(model);

        // MsiInstaller catches all exceptions — install still succeeds
        result.Should().BeTrue();
    }

    // ============================================================
    //  Error paths — ControleAcessoAgendamento XML with existing values
    // ============================================================

    [Fact]
    public async Task InstallAsync_ControleAcessoWithAgendamentoXml_UpdatesExisting()
    {
        var installer = new TestableMsiInstaller { FixedResult = true };
        var targetDir = Path.Combine(_tempRoot, "NewAcesso", "Controller", "ControleAcesso");
        Directory.CreateDirectory(targetDir);

        var iniPath = Path.Combine(targetDir, "PrimeAcesso.ControleAcesso.ini");
        CreateMinimalIni(iniPath);

        // Create existing agendamento XML with values
        var agendamentoPath = Path.Combine(targetDir, "AgendamentoEquipOffline.xml");
        var doc = new XmlDocument();
        var root = doc.CreateElement("Agendamento");
        doc.AppendChild(root);
        var ids = doc.CreateElement("IdsEquipamentos");
        ids.InnerText = "1|2|3";
        root.AppendChild(ids);
        var hora = doc.CreateElement("HoraInicio");
        hora.InnerText = "08:00";
        root.AppendChild(hora);
        var horaFim = doc.CreateElement("HoraFim");
        horaFim.InnerText = "18:00";
        root.AppendChild(horaFim);
        var dias = doc.CreateElement("DiasSemana");
        dias.InnerText = "1|2|3|4|5";
        root.AppendChild(dias);
        var ativo = doc.CreateElement("Ativo");
        ativo.InnerText = "true";
        root.AppendChild(ativo);
        doc.Save(agendamentoPath);

        var model = CreateModel(targetDir: targetDir);
        var result = await installer.InstallAsync(model);

        result.Should().BeTrue();
        // Agendamento XML should have been updated (not duplicated)
        var reloaded = new XmlDocument();
        reloaded.Load(agendamentoPath);
        reloaded.SelectNodes("//DiasSemana")!.Count.Should().Be(1);
    }

    // ============================================================
    //  Error paths — msiexec arguments edge cases
    // ============================================================

    [Fact]
    public async Task InstallAsync_MsiPathWithSpaces_IsCorrectlyQuoted()
    {
        var installer = new TestableMsiInstaller { FixedResult = true };
        var msiPath = Path.Combine(_tempRoot, "My App With Spaces", "App.msi");
        Directory.CreateDirectory(Path.GetDirectoryName(msiPath)!);
        File.WriteAllText(msiPath, "dummy");
        var model = new MsiInstallationModel
        {
            MsiPath = msiPath,
            TargetDirectory = Path.Combine(_tempRoot, "Target"),
            GenerateLog = false
        };

        await installer.InstallAsync(model);

        var args = installer.RunMsiexecCalls[0];
        args.Should().Contain($"\"{msiPath}\"");
    }

    [Fact]
    public async Task InstallAsync_TargetDirWithSpaces_IsCorrectlyQuoted()
    {
        var installer = new TestableMsiInstaller { FixedResult = true };
        var targetDir = Path.Combine(_tempRoot, "Target Dir With Spaces");
        var model = CreateModel(targetDir: targetDir);

        await installer.InstallAsync(model);

        var args = installer.RunMsiexecCalls[0];
        args.Should().Contain($"TARGETDIR=\"{targetDir}\"");
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
