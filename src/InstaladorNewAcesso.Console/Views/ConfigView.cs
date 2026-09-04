using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Console.Views;

public class ConfigView
{
    private readonly IUIService _ui;

    public ConfigView(IUIService ui)
    {
        _ui = ui;
    }

    public async Task ExecuteAsync(InstallationPaths paths)
    {
        _ui.Clear();
        _ui.WriteRule("PADRONIZAR / CONFIGURAR ARQUIVOS DO NEWACESSO", "cyan");
        _ui.WriteEmptyLine();
        _ui.WriteMessage("[gray]Esta operacao ira padronizar os arquivos .config, .ini, .xml e .json de todos os modulos,[/]");
        _ui.WriteMessage("[gray]unificando o caminho da base SQLite, ID de conexao e apontamentos de DLLs.[/]");
        _ui.WriteEmptyLine();

        var defaultDb = paths.ConnectionRecordDb;
        var dbPathInput = _ui.AskInput($"   [bold yellow]Caminho da Base SQLite[/] (padrao: [gray]{defaultDb}[/]):", defaultDb);
        var dbPath = string.IsNullOrWhiteSpace(dbPathInput) ? defaultDb : dbPathInput.Trim();

        var idConexao = _ui.AskInput("   [bold yellow]ID de Conexao no ConnectionRecord[/] (padrao: [gray]1[/]):", "1");
        if (string.IsNullOrWhiteSpace(idConexao)) idConexao = "1";

        var dsUri = _ui.AskInput("   [bold yellow]URL do WebAppDS (WCF Data Service)[/] (padrao: [gray]http://localhost:8080/DSPrimeAcesso.svc[/]):", "http://localhost:8080/DSPrimeAcesso.svc");
        if (string.IsNullOrWhiteSpace(dsUri)) dsUri = "http://localhost:8080/DSPrimeAcesso.svc";

        var bioServer = _ui.AskInput("   [bold yellow]Endereco do Servidor Biometrico[/] (padrao: [gray]localhost[/]):", "localhost");
        if (string.IsNullOrWhiteSpace(bioServer)) bioServer = "localhost";

        var fabricanteFacial = _ui.AskInput("   [bold yellow]Fabricante Equipamento Facial (Task)[/] (padrao: [gray]TopData[/]):", "TopData");
        if (string.IsNullOrWhiteSpace(fabricanteFacial)) fabricanteFacial = "TopData";

        var horaExclusao = _ui.AskInput("   [bold yellow]Hora Exclusao Facial (Task)[/] (padrao: [gray]17:00[/]):", "17:00");
        if (string.IsNullOrWhiteSpace(horaExclusao)) horaExclusao = "17:00";

        var autoApiUrl = _ui.AskInput("   [bold yellow]URL AutoAtendimento WebAPI[/] (padrao: [gray]http://localhost:8082[/]):", "http://localhost:8082");
        if (string.IsNullOrWhiteSpace(autoApiUrl)) autoApiUrl = "http://localhost:8082";

        var autoUiUrl = _ui.AskInput("   [bold yellow]URL AutoAtendimento WebAPP UI[/] (padrao: [gray]http://localhost:8081[/]):", "http://localhost:8081");
        if (string.IsNullOrWhiteSpace(autoUiUrl)) autoUiUrl = "http://localhost:8081";

        var autoApiKey = _ui.AskInput("   [bold yellow]API Key do AutoAtendimento[/] (padrao configurado):", "jaPAcmbGZTXnIVsXUqAN0V1nmbMIVuWOs5oD5BN4kybKGOI91EWIDt4qkcQdh9N7vxgAKd24NSbe3l60BlzUAQfhnYia329VZ0oWCPLftGjpGD1wgY3IYW8oPMVeIVif");
        if (string.IsNullOrWhiteSpace(autoApiKey)) autoApiKey = "jaPAcmbGZTXnIVsXUqAN0V1nmbMIVuWOs5oD5BN4kybKGOI91EWIDt4qkcQdh9N7vxgAKd24NSbe3l60BlzUAQfhnYia329VZ0oWCPLftGjpGD1wgY3IYW8oPMVeIVif";

        var options = new UnifiedConfigOptions
        {
            IdConexao = idConexao,
            DbPath = dbPath,
            DsServiceUri = dsUri,
            BiometricServer = bioServer,
            FabricanteFacial = fabricanteFacial,
            HoraExclusaoFacial = horaExclusao,
            AutoAtendimentoApiUrl = autoApiUrl,
            AutoAtendimentoUiUrl = autoUiUrl,
            AutoAtendimentoApiKey = autoApiKey
        };

        _ui.WriteEmptyLine();
        var service = new UnifiedConfigService(_ui);
        await service.ConfigureAllAsync(paths, options);

        _ui.WriteEmptyLine();
        _ui.WaitForEnter();
    }
}