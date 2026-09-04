using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Utils;

namespace InstaladorNewAcesso.Core.Services;

public record UnifiedConfigOptions
{
    public string IdConexao { get; init; } = "1";
    public string? DbPath { get; init; }
    public string DsServiceUri { get; init; } = "http://localhost:8080/DSPrimeAcesso.svc";
    public string BiometricServer { get; init; } = "localhost";
    public string FabricanteFacial { get; init; } = "TopData";
    public string HoraExclusaoFacial { get; init; } = "17:00";
    public string AutoAtendimentoApiUrl { get; init; } = "http://localhost:8082";
    public string AutoAtendimentoUiUrl { get; init; } = "http://localhost:8081";
    public string AutoAtendimentoApiKey { get; init; } = "jaPAcmbGZTXnIVsXUqAN0V1nmbMIVuWOs5oD5BN4kybKGOI91EWIDt4qkcQdh9N7vxgAKd24NSbe3l60BlzUAQfhnYia329VZ0oWCPLftGjpGD1wgY3IYW8oPMVeIVif";
    public string? AutoAtendimentoDbConnectionString { get; init; }
}

public class UnifiedConfigService
{
    private readonly IUIService _ui;

    public UnifiedConfigService(IUIService ui)
    {
        _ui = ui;
    }

    public async Task<bool> ConfigureAllAsync(InstallationPaths paths, UnifiedConfigOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(paths);
        options ??= new UnifiedConfigOptions();

        UIScope.Current = _ui;

        _ui.WriteRule("PADRONIZACAO DE CONFIGURACOES (.CONFIG, .INI, .XML, .JSON)", "cyan");
        _ui.WriteMessage($"\n[bold yellow]Caminho Base:[/] [cyan]{MarkupHelper.Escape(paths.NewAcessoRoot)}[/]");
        _ui.WriteMessage($"[bold yellow]ID Conexao:[/] [green]{MarkupHelper.Escape(options.IdConexao)}[/]");
        _ui.WriteMessage($"[bold yellow]Base SQLite:[/] [gray]{MarkupHelper.Escape(options.DbPath ?? paths.ConnectionRecordDb)}[/]");
        _ui.WriteEmptyLine();

        var resolvedDbPath = options.DbPath ?? paths.ConnectionRecordDb;
        var totalSuccess = 0;
        var totalAttempted = 0;

        await Task.Yield();

        // 1. ConnectionRecord
        _ui.WriteMessage("[bold cyan]1. Configurando ConnectionRecord...[/]");
        totalAttempted++;
        if (ConnectionRecordConfigHelper.UpdateConfig(paths.ConnectionRecord, resolvedDbPath))
            totalSuccess++;

        // 2. Controller - ControleAcesso (.ini)
        _ui.WriteMessage("\n[bold cyan]2. Configurando ControleAcesso (.ini)...[/]");
        totalAttempted++;
        if (ControleAcessoConfigHelper.UpdateConfig(paths.ControleAcesso, options.IdConexao, resolvedDbPath))
            totalSuccess++;

        // 3. Controller - CoreWs (.config, WatchDog)
        _ui.WriteMessage("\n[bold cyan]3. Configurando CoreWs (.config e WatchDog)...[/]");
        totalAttempted++;
        if (CoreWsConfigHelper.UpdateConfig(paths.CoreWs, paths.ControleAcesso, Path.Combine(paths.CoreWs, "Logs"), paths.Fabricantes, options.BiometricServer))
            totalSuccess++;

        // 4. Controller - Task (.config)
        _ui.WriteMessage("\n[bold cyan]4. Configurando Controller Task...[/]");
        totalAttempted++;
        if (TaskConfigHelper.UpdateConfig(paths.Task, options.IdConexao, resolvedDbPath, options.FabricanteFacial, options.HoraExclusaoFacial))
            totalSuccess++;

        // 5. ControllerOffline - WinService_Ex (.config)
        _ui.WriteMessage("\n[bold cyan]5. Configurando StandAloneEx (.config)...[/]");
        totalAttempted++;
        if (StandAloneExConfigHelper.UpdateConfig(paths.ControllerOfflineWinServiceEx, options.IdConexao, resolvedDbPath))
            totalSuccess++;

        // 6. ControllerOffline - WinService_In (.config)
        _ui.WriteMessage("\n[bold cyan]6. Configurando StandAloneIn (.config)...[/]");
        totalAttempted++;
        if (StandAloneImConfigHelper.UpdateConfig(paths.ControllerOfflineWinServiceIn, options.IdConexao, resolvedDbPath))
            totalSuccess++;

        // 7. WebAppUI (Web.config)
        _ui.WriteMessage("\n[bold cyan]7. Configurando WebAppUI (Web.config)...[/]");
        totalAttempted++;
        if (WebAppConfigHelper.UpdateWebAppUIConfig(paths.WebAppUI, options.IdConexao, resolvedDbPath, options.DsServiceUri, paths.Fabricantes))
            totalSuccess++;

        // 8. WebAppDS (Web.config)
        _ui.WriteMessage("\n[bold cyan]8. Configurando WebAppDS (Web.config)...[/]");
        totalAttempted++;
        if (WebAppConfigHelper.UpdateWebAppDSConfig(paths.WebAppDS, options.IdConexao, resolvedDbPath))
            totalSuccess++;

        // 9. Win Desktop (.config e .ini)
        _ui.WriteMessage("\n[bold cyan]9. Configurando Win Desktop (.config e .ini)...[/]");
        totalAttempted++;
        if (WinConfigHelper.UpdateConfig(paths.Win, options.DsServiceUri, options.BiometricServer))
            totalSuccess++;

        // 10. AutoAtendimento (WebAPI e WebAPP appsettings.json)
        _ui.WriteMessage("\n[bold cyan]10. Configurando AutoAtendimento (appsettings.json)...[/]");
        totalAttempted++;
        if (AutoAtendimentoConfigHelper.UpdateConfig(paths.AutoAtendimento, options.AutoAtendimentoApiUrl, options.AutoAtendimentoUiUrl, options.AutoAtendimentoApiKey, options.AutoAtendimentoDbConnectionString))
            totalSuccess++;

        _ui.WriteEmptyLine();
        _ui.WriteRule("RESUMO DA CONFIGURACAO", totalSuccess > 0 ? "green" : "yellow");
        _ui.WriteMessage($"[bold]Modulos processados com sucesso:[/] [green]{totalSuccess}[/] de [cyan]{totalAttempted}[/]");

        return totalSuccess > 0;
    }
}
