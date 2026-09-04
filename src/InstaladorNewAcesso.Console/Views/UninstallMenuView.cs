using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Configurations;
using InstaladorNewAcesso.Core.Factories;
using InstaladorNewAcesso.Core.Implementations;
using InstaladorNewAcesso.Core.Services;
using InstaladorNewAcesso.Core.Utils;
using Spectre.Console;

namespace InstaladorNewAcesso.Console.Views;

public class UninstallMenuView
{
    private readonly IUIService _ui;
    private readonly IisInstaller _iisInstaller = new();
    private readonly MsiUninstaller _msiUninstaller = new();
    private readonly SummaryPanelView _summaryView;
    private readonly ViewHelper _viewHelper;
    private InstallationPaths? _paths;

    public UninstallMenuView(IUIService ui)
    {
        _ui = ui;
        _summaryView = new SummaryPanelView(ui);
        _viewHelper = new ViewHelper(ui);
    }

    public async Task ExecuteAsync()
    {
        if (!EnsurePaths())
        {
            AuditLogger.Finish();
            return;
        }

        AuditLogger.Start(_paths!.BasePath, AuditType.Uninstall);
        var auditPath = AuditLogger.CurrentLogPath;
        _ui.WriteMessage($" [gray]Log de auditoria: {auditPath?.EscapeMarkup() ?? "?"}[/]");

        while (true)
        {
            _ui.Clear();
            _ui.WriteFiglet("NEW ACESSO", "red");
            _ui.WriteFiglet("UNINSTALL", "orange1");
            _ui.WriteEmptyLine();
            _ui.WriteRule("SISTEMA DE DESINSTALAÇÃO", "red");
            _ui.WriteEmptyLine();

            var opcao = _ui.AskChoice(
                "[bold red]Selecione uma opção de desinstalação:[/]",
                [
                    "1 - Scan: Verificar o que está instalado",
                    "2 - Remover Sites IIS (WebAppDS, WebAppUI)",
                    "3 - Remover Application Pools IIS (WebAppDS, WebAppUI)",
                    "4 - Desinstalar Aplicações (MSIs)",
                    "5 - Remover Diretórios do NewAcesso",
                    "6 - Desinstalação Completa (IIS + MSIs + Diretórios)",
                    "7 - [[AVANÇADO]] Desabilitar Recursos do Windows",
                    "0 - Voltar ao Menu Principal"
                ]);

            switch (opcao[..1])
            {
                case "1":
                    await ScanInstalledAsync();
                    break;
                case "2":
                    await RemoveIisSitesAsync();
                    break;
                case "3":
                    await RemoveIisAppPoolsAsync();
                    break;
                case "4":
                    await UninstallMsIsAsync();
                    break;
                case "5":
                    RemoveDirectories();
                    break;
                case "6":
                    await FullUninstallAsync();
                    break;
                case "7":
                    await DisableWindowsFeaturesAsync();
                    break;
                case "0":
                    AuditLogger.Finish();
                    return;
            }
        }
    }

    private bool EnsurePaths()
    {
        var basePath = _ui.AskInput(
            "\n[bold yellow]Digite o caminho base do NewAcesso[/] ([gray]Ex: C:\\SoftPrime ou D:\\SoftPrime[/]):");

        if (string.IsNullOrWhiteSpace(basePath) || !Directory.Exists(Path.GetPathRoot(basePath)))
        {
            _ui.WriteError("Caminho inválido.");
            _ui.WaitForEnter();
            return false;
        }

        _paths = new InstallationPaths(basePath);
        return true;
    }

    // =========================================
    // SCAN
    // =========================================
    private async Task ScanInstalledAsync()
    {
        _ui.WriteRule("SCAN: Verificando instalação atual", "yellow");
        _ui.WriteEmptyLine();

        if (_paths == null) return;

        var appPoolNames = new[] { "WebAppDS", "WebAppUI" };
        var siteNames = new[] { "WebAppDS", "WebAppUI" };

        Dictionary<string, bool> appPoolsStatus = new();
        Dictionary<string, bool> sitesStatus = new();

        await _ui.ShowStatus("Escaneando...", async update =>
        {
            appPoolsStatus = await _iisInstaller.CheckAppPoolsExistAsync(appPoolNames);
            sitesStatus = await _iisInstaller.CheckSitesExistAsync(siteNames);
        });

        // Tabela IIS
        _ui.WriteMessage("\n[bold]IIS:[/]");
        var iisHeaders = new[] { "Recurso", "Status" };
        var iisRows = new List<string[]>();

        foreach (var name in appPoolNames)
        {
            var exists = appPoolsStatus.GetValueOrDefault(name, false);
            iisRows.Add([$"AppPool {name}", exists ? "[green]Instalado[/]" : "[gray]Não encontrado[/]"]);
        }
        foreach (var name in siteNames)
        {
            var exists = sitesStatus.GetValueOrDefault(name, false);
            iisRows.Add([$"Site {name}", exists ? "[green]Instalado[/]" : "[gray]Não encontrado[/]"]);
        }
        _ui.WriteTable(iisHeaders, iisRows);

        // Diretórios e aplicações
        _ui.WriteMessage("\n[bold]Diretórios / Aplicações:[/]");
        var dirHeaders = new[] { "Diretório", "Status" };
        var dirRows = new List<string[]>();

        var dirs = DirectorySetup.GetAllPaths(_paths).ToList();
        var dirsExistentes = 0;
        var appsInstaladas = 0;
        foreach (var dir in dirs)
        {
            var exists = Directory.Exists(dir);
            if (exists) dirsExistentes++;
            if (MsiUninstaller.IsInstalled(dir)) appsInstaladas++;
            dirRows.Add([dir, exists ? "[green]Existe[/]" : "[gray]Não existe[/]"]);
        }
        _ui.WriteTable(dirHeaders, dirRows);

        _ui.WriteEmptyLine();
        _ui.WriteMessage($"[green]{dirsExistentes}/{dirs.Count} diretórios existentes.[/]");
        _ui.WriteMessage($"[cyan]{appsInstaladas} aplicação(ões) com arquivos instalados.[/]");
        _ui.WriteEmptyLine();

        _ui.WriteRule("Fim do Scan", "cyan");
        _ui.WaitForEnter();
    }

    /// <summary>
    /// Confirmação extra: usuário deve digitar "SIM" para prosseguir.
    /// </summary>
    private bool ConfirmDestructive(string message)
    {
        var resposta = _ui.AskInput(
            "\n[bold red]" + message + " (digite [yellow]SIM[/] para confirmar ou [gray]ENTER[/] para cancelar)[/]");

        return resposta?.Trim().Equals("SIM", StringComparison.OrdinalIgnoreCase) == true;
    }

    // =========================================
    // REMOVER IIS SITES
    // =========================================
    private async Task RemoveIisSitesAsync()
    {
        _ui.WriteRule("REMOÇÃO DE SITES IIS", "red");
        _ui.WriteEmptyLine();

        var siteNames = new[] { "WebAppDS", "WebAppUI" };
        var sitesStatus = await _iisInstaller.CheckSitesExistAsync(siteNames);

        var existentes = sitesStatus.Where(s => s.Value).ToList();
        if (existentes.Count == 0)
        {
            _ui.WriteWarning("Nenhum site IIS encontrado para remover.");
            _ui.WaitForEnter();
            return;
        }

        _ui.WriteMessage($"[yellow]{existentes.Count} site(s) encontrado(s):[/]");
        foreach (var site in existentes)
            _ui.WriteMessage($"  - [cyan]{site.Key.EscapeMarkup()}[/]");

        if (!ConfirmDestructive("Tem certeza que deseja remover estes sites?"))
        {
            _ui.WriteMessage("[gray]Operação cancelada.[/]");
            _ui.WaitForEnter();
            return;
        }

        AuditLogger.Separator("Removendo Sites IIS");

        var resultados = new List<SummaryResult>();
        foreach (var site in existentes)
        {
            _ui.WriteMessage($" Removendo site [yellow]{site.Key.EscapeMarkup()}[/]... ");
            var ok = await _iisInstaller.RemoveSiteAsync(site.Key);
            _ui.WriteMessage(ok ? "[green][[OK]][/]" : "[red][[FALHA]][/]");
            AuditLogger.Log("Remover Site IIS", site.Key, ok);
            resultados.Add(SummaryStore.Add("Desinstalação", $"Site {site.Key}", ok, ok ? "Removido" : "Falha ao remover"));
        }

        _ui.WriteEmptyLine();
        _summaryView.ExibirEtapa("Desinstalação IIS", "🗑️", resultados);
        _ui.WaitForEnter();
    }

    // =========================================
    // REMOVER IIS APP POOLS
    // =========================================
    private async Task RemoveIisAppPoolsAsync()
    {
        _ui.WriteRule("REMOÇÃO DE APPLICATION POOLS IIS", "red");
        _ui.WriteEmptyLine();

        var poolNames = new[] { "WebAppDS", "WebAppUI" };
        var poolsStatus = await _iisInstaller.CheckAppPoolsExistAsync(poolNames);

        var existentes = poolsStatus.Where(p => p.Value).ToList();
        if (existentes.Count == 0)
        {
            _ui.WriteWarning("Nenhum Application Pool encontrado para remover.");
            _ui.WaitForEnter();
            return;
        }

        _ui.WriteMessage($"[yellow]{existentes.Count} pool(s) encontrado(s):[/]");
        foreach (var pool in existentes)
            _ui.WriteMessage($"  - [cyan]{pool.Key.EscapeMarkup()}[/]");

        if (!ConfirmDestructive("Tem certeza que deseja remover estes Application Pools?"))
        {
            _ui.WriteMessage("[gray]Operação cancelada.[/]");
            _ui.WaitForEnter();
            return;
        }

        AuditLogger.Separator("Removendo Application Pools IIS");

        var resultados = new List<SummaryResult>();
        foreach (var pool in existentes)
        {
            _ui.WriteMessage($" Removendo AppPool [yellow]{pool.Key.EscapeMarkup()}[/]... ");
            var ok = await _iisInstaller.RemoveAppPoolAsync(pool.Key);
            _ui.WriteMessage(ok ? "[green][[OK]][/]" : "[red][[FALHA]][/]");
            AuditLogger.Log("Remover AppPool IIS", pool.Key, ok);
            resultados.Add(SummaryStore.Add("Desinstalação", $"AppPool {pool.Key}", ok, ok ? "Removido" : "Falha ao remover"));
        }

        _ui.WriteEmptyLine();
        _summaryView.ExibirEtapa("Desinstalação IIS", "🗑️", resultados);
        _ui.WaitForEnter();
    }

    // =========================================
    // REMOVER DIRETÓRIOS (síncrono)
    // =========================================
    private void RemoveDirectories()
    {
        _ui.WriteRule("REMOÇÃO DE DIRETÓRIOS", "red");
        _ui.WriteEmptyLine();

        if (_paths == null) return;

        var dirs = DirectorySetup.GetAllPaths(_paths).ToList();
        var existentes = dirs.Where(Directory.Exists).ToList();

        if (existentes.Count == 0)
        {
            _ui.WriteWarning("Nenhum diretório encontrado para remover.");
            _ui.WaitForEnter();
            return;
        }

        _ui.WriteMessage($"[yellow]{existentes.Count} diretório(s) encontrado(s).[/]");
        foreach (var dir in existentes)
            _ui.WriteMessage($"  - [cyan]{dir.EscapeMarkup()}[/]");

        if (!ConfirmDestructive("Tem certeza que deseja remover estes diretórios?"))
        {
            _ui.WriteMessage("[gray]Operação cancelada.[/]");
            _ui.WaitForEnter();
            return;
        }

        AuditLogger.Separator("Removendo Diretórios");

        var resultados = new List<SummaryResult>();
        foreach (var dir in existentes)
        {
            _ui.WriteMessage($" Removendo [yellow]{dir.EscapeMarkup()}[/]... ");
            try
            {
                Directory.Delete(dir, true);
                _ui.WriteMessage("[green][[OK]][/]");
                AuditLogger.Log("Remover Diretório", dir, true);
                resultados.Add(SummaryStore.Add("Desinstalação", $"Diretório {dir}", true, "Removido"));
            }
            catch (Exception ex)
            {
                _ui.WriteMessage($"[red][[FALHA]] {ex.Message.EscapeMarkup()}[/]");
                AuditLogger.Log("Remover Diretório", dir, false, ex.Message);
                resultados.Add(SummaryStore.Add("Desinstalação", $"Diretório {dir}", false, ex.Message));
            }
        }

        // Tentar remover diretório raiz
        if (Directory.Exists(_paths.NewAcessoRoot))
        {
            try
            {
                Directory.Delete(_paths.NewAcessoRoot, true);
                AuditLogger.Log("Remover Diretório Raiz", _paths.NewAcessoRoot, true);
            }
            catch (Exception ex)
            {
                AuditLogger.Log("Remover Diretório Raiz", _paths.NewAcessoRoot, false, ex.Message);
                _ui.WriteMessage($"  [yellow][[AVISO]] Não foi possível remover a raiz: {ex.Message.EscapeMarkup()}[/]");
            }
        }

        _ui.WriteEmptyLine();
        _summaryView.ExibirEtapa("Desinstalação Diretórios", "🗑️", resultados);
        _ui.WaitForEnter();
    }

    // =========================================
    // DESINSTALAR MSIs (com scan + msiexec /x)
    // =========================================
    private async Task UninstallMsIsAsync()
    {
        _ui.WriteRule("DESINSTALAÇÃO DE APLICAÇÕES (MSIs)", "red");
        _ui.WriteEmptyLine();

        if (_paths == null) return;

        // Auto-detectar versões disponíveis no diretório de instaladores
        var msiRoot = _viewHelper.ResolveInstallerPath(_paths.InstallationPath, "instaladores MSI");

        if (!Directory.Exists(msiRoot))
        {
            _ui.WriteWarning("Diretório de instaladores não encontrado. A desinstalação removerá apenas os arquivos dos diretórios.");

            // Fallback: remover apenas diretórios
            var dirs = DirectorySetup.GetAllPaths(_paths).ToList();
            var instalados = dirs.Where(d => MsiUninstaller.IsInstalled(d)).ToList();

            if (instalados.Count == 0)
            {
                _ui.WriteWarning("Nenhuma aplicação com arquivos instalados encontrada.");
                _ui.WaitForEnter();
                return;
            }

            _ui.WriteMessage($"\n[yellow]{instalados.Count} diretório(s) com arquivos encontrado(s).[/]");
            if (!ConfirmDestructive("Remover apenas os arquivos (sem desinstalação formal do MSI)?"))
            {
                _ui.WriteMessage("[gray]Operação cancelada.[/]");
                _ui.WaitForEnter();
                return;
            }

            AuditLogger.Separator("Removendo arquivos MSIs (sem msiexec)");

            var resultados = new List<SummaryResult>();
            foreach (var dir in instalados)
            {
                _ui.WriteMessage($" Removendo [yellow]{dir.EscapeMarkup()}[/]... ");
                var ok = MsiUninstaller.RemoveTargetDirectory(dir);
                _ui.WriteMessage(ok ? "[green][[OK]][/]" : "[red][[FALHA]][/]");
                AuditLogger.Log("Remover Arquivos MSI", dir, ok);
                resultados.Add(SummaryStore.Add("Desinstalação", $"Arquivos {dir}", ok, ok ? "Removido" : "Falha ao remover"));
            }

            _ui.WriteEmptyLine();
            _summaryView.ExibirEtapa("Desinstalação MSIs", "🗑️", resultados);
            _ui.WaitForEnter();
            return;
        }

        // Scan: usar MsiScanner para listar MSIs
        var dbChoice = _ui.AskChoice(
            "[bold yellow]Banco de dados:[/]",
            ["SQLServer", "Oracle"]);

        var resultadosMsIs = new List<SummaryResult>();
        var scanner = new MsiScanner(_paths, dbChoice, msiRoot);

        var todosMsi = new List<MsiInstallationModel>();
        try
        {
            await _ui.ShowStatus("Escaneando MSIs...", async update =>
            {
                todosMsi = scanner.Scan();
            });
        }
        catch (Exception ex)
        {
            _ui.WriteError(ex.Message.EscapeMarkup());
            _ui.WaitForEnter();
            return;
        }

        if (todosMsi.Count == 0)
        {
            _ui.WriteWarning("Nenhum MSI encontrado para desinstalação.");
            _ui.WaitForEnter();
            return;
        }

        // Mostrar tabela de MSIs encontrados e verificar quais estão instalados
        var instaladosMsi = new List<(MsiInstallationModel model, int index)>();
        var estadosInstalacao = new Dictionary<int, bool>();

        await _ui.ShowStatus("Verificando status dos MSIs...", async update =>
        {
            for (var i = 0; i < todosMsi.Count; i++)
            {
                var msi = todosMsi[i];
                var installed = MsiUninstaller.IsInstalled(msi.TargetDirectory)
                                 || await _msiUninstaller.IsRegisteredAsync(msi.TargetDirectory);
                estadosInstalacao[i] = installed;
                if (installed) instaladosMsi.Add((msi, i));
            }
        });

        var headers = new[] { "#", "MSI", "Destino", "Instalado?" };
        var rows = new List<string[]>();

        for (var i = 0; i < todosMsi.Count; i++)
        {
            var msi = todosMsi[i];
            var nome = Path.GetFileName(msi.MsiPath) ?? "";
            var installed = estadosInstalacao.GetValueOrDefault(i);

            rows.Add([
                $"{i + 1}",
                nome,
                msi.TargetDirectory,
                installed ? "[green]Sim[/]" : "[gray]Não[/]"
            ]);
        }

        _ui.WriteTable(headers, rows);
        _ui.WriteEmptyLine();

        if (instaladosMsi.Count == 0)
        {
            _ui.WriteWarning("Nenhum MSI parece estar instalado (diretórios vazios/inexistentes e nenhum registro no Windows Installer).");
            _ui.WaitForEnter();
            return;
        }

        _ui.WriteMessage($"[yellow]{instaladosMsi.Count} MSI(s) instalado(s) encontrado(s).[/]");
        _ui.WriteMessage("[gray]A desinstalação irá: 1) Executar msiexec /x 2) Remover diretório de instalação[/]");
        _ui.WriteMessage("[gray]Os arquivos .msi originais NÃO serão removidos.[/]");

        if (!ConfirmDestructive("Deseja desinstalar todos os MSIs instalados?"))
        {
            _ui.WriteMessage("[gray]Operação cancelada.[/]");
            _ui.WaitForEnter();
            return;
        }

        AuditLogger.Separator("Desinstalando MSIs via msiexec /x");

        foreach (var (model, _) in instaladosMsi)
        {
            var nome = Path.GetFileName(model.MsiPath) ?? "";

            // Passo 1: msiexec /x
            _ui.WriteMessage($"\n Desinstalando [yellow]{nome.EscapeMarkup()}[/] via msiexec... ");
            var uninstalled = await MsiUninstaller.UninstallByMsiPathAsync(model.MsiPath);
            _ui.WriteMessage(uninstalled ? "[green][[OK]][/]" : "[yellow][[IGNORADO]][/]");
            AuditLogger.Log("msiexec /x", nome, uninstalled);

            // Passo 2: remover diretório
            _ui.WriteMessage($" Removendo diretório [yellow]{model.TargetDirectory.EscapeMarkup()}[/]... ");
            var removed = MsiUninstaller.RemoveTargetDirectory(model.TargetDirectory);
            _ui.WriteMessage(removed ? "[green][[OK]][/]" : "[yellow][[IGNORADO]][/]");
            AuditLogger.Log("Remover Diretório MSI", model.TargetDirectory, removed);

            var sucesso = uninstalled || removed;
            resultadosMsIs.Add(SummaryStore.Add("Desinstalação", nome, sucesso,
                sucesso ? "MSI desinstalado" : "Falha ao desinstalar"));
        }

        _ui.WriteEmptyLine();
        _summaryView.ExibirEtapa("Desinstalação MSIs", "🗑️", resultadosMsIs);
        _ui.WaitForEnter();
    }

    // =========================================
    // DESINSTALAÇÃO COMPLETA
    // =========================================
    private async Task FullUninstallAsync()
    {
        _ui.WriteRule("DESINSTALAÇÃO COMPLETA", "red");
        _ui.WriteEmptyLine();

        _ui.WriteMessage("\n[red]Isso irá:[/]");
        _ui.WriteMessage("  [red]1.[/] Remover Sites IIS (WebAppDS, WebAppUI)");
        _ui.WriteMessage("  [red]2.[/] Remover Application Pools IIS (WebAppDS, WebAppUI)");
        _ui.WriteMessage("  [red]3.[/] Desinstalar MSIs (msiexec /x)");
        _ui.WriteMessage("  [red]4.[/] Remover diretórios do NewAcesso");
        _ui.WriteMessage("\n[gray]  Os instaladores (.msi) NÃO serão removidos.[/]");

        if (!ConfirmDestructive("PRIMEIRA CONFIRMAÇÃO: Tem certeza absoluta? Esta operação não pode ser desfeita!"))
        {
            _ui.WriteMessage("[gray]Operação cancelada.[/]");
            _ui.WaitForEnter();
            return;
        }

        if (!ConfirmDestructive("CONFIRMAÇÃO FINAL: Deseja realmente prosseguir?"))
        {
            _ui.WriteMessage("[gray]Operação cancelada.[/]");
            _ui.WaitForEnter();
            return;
        }

        AuditLogger.Separator("DESINSTALAÇÃO COMPLETA INICIADA");

        // Fase 1: Remover Sites
        _ui.WriteEmptyLine();
        _ui.WriteRule("Fase 1/4: Removendo Sites IIS", "yellow");
        var sitesStatus = await _iisInstaller.CheckSitesExistAsync(["WebAppDS", "WebAppUI"]);
        foreach (var site in sitesStatus.Where(s => s.Value))
        {
            _ui.WriteMessage($" Removendo site [yellow]{site.Key.EscapeMarkup()}[/]... ");
            var ok = await _iisInstaller.RemoveSiteAsync(site.Key);
            _ui.WriteMessage(ok ? "[green][[OK]][/]" : "[red][[FALHA]][/]");
            AuditLogger.Log("Full - Remover Site", site.Key, ok);
        }

        // Fase 2: Remover AppPools
        _ui.WriteEmptyLine();
        _ui.WriteRule("Fase 2/4: Removendo Application Pools", "yellow");
        var poolsStatus = await _iisInstaller.CheckAppPoolsExistAsync(["WebAppDS", "WebAppUI"]);
        foreach (var pool in poolsStatus.Where(p => p.Value))
        {
            _ui.WriteMessage($" Removendo AppPool [yellow]{pool.Key.EscapeMarkup()}[/]... ");
            var ok = await _iisInstaller.RemoveAppPoolAsync(pool.Key);
            _ui.WriteMessage(ok ? "[green][[OK]][/]" : "[red][[FALHA]][/]");
            AuditLogger.Log("Full - Remover AppPool", pool.Key, ok);
        }

        // Fase 3: Desinstalar MSIs via msiexec /x
        if (_paths != null)
        {
            _ui.WriteEmptyLine();
            _ui.WriteRule("Fase 3/4: Desinstalando MSIs (msiexec /x)", "yellow");

            // Auto-detectar versão disponível
            var msiRoot = _viewHelper.ResolveInstallerPath(_paths.InstallationPath, "instaladores MSI");

            if (Directory.Exists(msiRoot))
            {
                var dbChoice = _ui.AskChoice(
                    "   [bold yellow]Banco de dados da instalação original:[/]",
                    ["SQLServer", "Oracle"]);

                try
                {
                    var scanner = new MsiScanner(_paths, dbChoice, msiRoot);
                    var todosMsi = scanner.Scan();

                    var instalados = new List<MsiInstallationModel>();
                    foreach (var m in todosMsi)
                    {
                        if (MsiUninstaller.IsInstalled(m.TargetDirectory) ||
                            await _msiUninstaller.IsRegisteredAsync(m.TargetDirectory))
                        {
                            instalados.Add(m);
                        }
                    }

                    if (instalados.Count == 0)
                    {
                        _ui.WriteWarning("  Nenhum MSI instalado encontrado. Pulando etapa.");
                    }
                    else
                    {
                        _ui.WriteMessage($"[yellow]  {instalados.Count} MSI(s) instalado(s) encontrado(s). Desinstalando...[/]");

                        foreach (var model in instalados)
                        {
                            var nome = Path.GetFileName(model.MsiPath) ?? "";
                            _ui.WriteMessage($"  Desinstalando [yellow]{nome.EscapeMarkup()}[/]... ");
                            var ok = await MsiUninstaller.UninstallByMsiPathAsync(model.MsiPath);
                            _ui.WriteMessage(ok ? "[green][[OK]][/]" : "[yellow][[IGNORADO]][/]");
                            AuditLogger.Log("Full - msiexec /x", nome, ok);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ui.WriteWarning($"  Falha ao escanear MSIs: {ex.Message.EscapeMarkup()}");
                    _ui.WriteMessage("  [yellow]  Prosseguindo com remoção dos diretórios...[/]");
                }
            }
            else
            {
                _ui.WriteMessage($"[gray]  Diretório de instaladores não encontrado: {msiRoot.EscapeMarkup()}[/]");
                _ui.WriteWarning("  Pulando desinstalação formal dos MSIs (sem arquivo .msi não é possível executar msiexec /x).");
                _ui.WriteMessage("  [yellow]  Os diretórios serão removidos na próxima etapa.[/]");
            }
        }

        // Fase 4: Remover diretórios + arquivos
        if (_paths != null)
        {
            _ui.WriteEmptyLine();
            _ui.WriteRule("Fase 4/4: Removendo diretórios e arquivos", "yellow");
            var dirs = DirectorySetup.GetAllPaths(_paths).ToList();
            foreach (var dir in dirs.Where(Directory.Exists))
            {
                _ui.WriteMessage($" Removendo [yellow]{dir.EscapeMarkup()}[/]... ");
                try
                {
                    Directory.Delete(dir, true);
                    _ui.WriteMessage("[green][[OK]][/]");
                    AuditLogger.Log("Full - Remover Diretório", dir, true);
                }
                catch (Exception ex)
                {
                    _ui.WriteMessage($"[red][[FALHA]] {ex.Message.EscapeMarkup()}[/]");
                    AuditLogger.Log("Full - Remover Diretório", dir, false, ex.Message);
                }
            }

            // Raiz
            if (Directory.Exists(_paths.NewAcessoRoot))
            {
                try
                {
                    Directory.Delete(_paths.NewAcessoRoot, true);
                    AuditLogger.Log("Full - Remover Raiz", _paths.NewAcessoRoot, true);
                }
                catch (Exception ex)
                {
                    AuditLogger.Log("Full - Remover Raiz", _paths.NewAcessoRoot, false, ex.Message);
                    _ui.WriteMessage($"  [yellow][[AVISO]] Não foi possível remover a raiz: {ex.Message.EscapeMarkup()}[/]");
                }
            }
        }

        _ui.WriteEmptyLine();
        _ui.WriteMessage("\n[green]Desinstalação completa finalizada![/]");
        _ui.WaitForEnter();
    }

    // =========================================
    // AVANÇADO: DESABILITAR WINDOWS FEATURES
    // =========================================
    private async Task DisableWindowsFeaturesAsync()
    {
        _ui.WriteRule("AVANÇADO: DESABILITAR RECURSOS DO WINDOWS", "red");
        _ui.WriteEmptyLine();

        _ui.WriteMessage("\n[bold red]⚠️  ATENÇÃO: Esta operação desabilita recursos do Windows[/]");
        _ui.WriteMessage("[red]Isto pode afetar outros programas que dependam destes recursos.[/]");
        _ui.WriteMessage("[red]Recomendado apenas para ambientes de teste/desenvolvimento.[/]");

        if (!ConfirmDestructive("Deseja realmente continuar? Esta ação desabilita recursos do Windows!"))
        {
            _ui.WriteMessage("[gray]Operação cancelada.[/]");
            _ui.WaitForEnter();
            return;
        }

        var installer = InstallerFactory.Create();
        var setup = new FeatureSetup();
        var features = setup.Features;

        // Scan: verificar quais estão habilitados
        _ui.WriteEmptyLine();
        _ui.WriteWarning("Verificando recursos instalados...");
        var scanResults = await installer.CheckFeaturesInstalledAsync(features);
        var instalados = scanResults.Where(r => r.IsInstalled).Select(r => r.Feature).ToList();

        if (instalados.Count == 0)
        {
            _ui.WriteWarning("Nenhum recurso do Windows está habilitado (dentre os listados).");
            _ui.WaitForEnter();
            return;
        }

        _ui.WriteMessage($"[yellow]{instalados.Count} recurso(s) habilitado(s) encontrado(s).[/]");

        if (!ConfirmDestructive("CONFIRMAÇÃO FINAL: Deseja desabilitar estes recursos?"))
        {
            _ui.WriteMessage("[gray]Operação cancelada.[/]");
            _ui.WaitForEnter();
            return;
        }

        AuditLogger.Separator("Desabilitando Recursos do Windows");

        // Implementação real de desabilitação em paralelo
        var resultados = new List<SummaryResult>();
        var isDesktop = installer is WindowsDesktopInstaller;
        var sync = new object();

        _ui.WriteMessage($"[yellow]Desabilitando {instalados.Count} recurso(s) em paralelo...[/]");
        _ui.WriteEmptyLine();

        await Parallel.ForEachAsync(
            instalados,
            new ParallelOptions { MaxDegreeOfParallelism = 4 },
            async (feature, ct) =>
            {
                var featureName = isDesktop ? feature.DesktopName : feature.ServerName;

                string command;
                if (isDesktop)
                    command = $"Disable-WindowsOptionalFeature -Online -FeatureName {featureName} -NoRestart";
                else
                    command = $"Remove-WindowsFeature -Name {featureName}";

                var procExecutor = new ProcessExecutorService();
                var ok = await procExecutor.RunPowerShellCommandAsync(command, feature.FriendlyName);

                lock (sync)
                {
                    _ui.WriteMessage(ok
                        ? $"   [green][[OK]] {feature.FriendlyName.EscapeMarkup()}[/]"
                        : $"   [red][[FALHA]] {feature.FriendlyName.EscapeMarkup()}[/]");
                    AuditLogger.Log("Desabilitar Windows Feature", feature.FriendlyName, ok);
                    resultados.Add(SummaryStore.Add("Desinstalação", $"Feature {feature.FriendlyName}", ok,
                        ok ? "Desabilitado" : "Falha ao desabilitar"));
                }
            });

        _ui.WriteEmptyLine();
        _summaryView.ExibirEtapa("Windows Features", "⚠️", resultados);
        _ui.WaitForEnter();
    }
}
