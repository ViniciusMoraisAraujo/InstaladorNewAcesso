using InstaladorNewAcesso.Abstractions.Interfaces;
using InstaladorNewAcesso.Abstractions.Models;
using InstaladorNewAcesso.Core.Services;
using InstaladorNewAcesso.Core.Utils;
using Spectre.Console;

namespace InstaladorNewAcesso.Console.Views;

public class DownloadView
{
    private readonly IUIService _ui;
    private readonly SummaryPanelView _summaryView;

    public DownloadView(IUIService ui)
    {
        _ui = ui;
        _summaryView = new SummaryPanelView(ui);
    }

    public async Task ExecuteAsync(InstallationPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _ui.WriteRule("DOWNLOAD DOS INSTALADORES", "cyan");
        _ui.WriteEmptyLine();

        _ui.WriteMessage("[gray]Esta opção baixa os instaladores do NewAcesso do Google Drive.[/]");
        _ui.WriteMessage($"[gray]Os arquivos serão salvos em: {paths.InstallationPath.EscapeMarkup()}[/]");
        _ui.WriteEmptyLine();

        // ── Google Drive Folder URL/ID ──
        var folderInput = _ui.AskInput("[bold yellow]URL ou ID da pasta do Google Drive:[/]");

        var folderId = GoogleDriveDownloader.ExtractFolderId(folderInput);

        if (string.IsNullOrWhiteSpace(folderId))
        {
            _ui.WriteError("Não foi possível extrair o ID da pasta do URL informado.");
            _ui.WriteWarning("Certifique-se de usar um URL válido do Google Drive (ex: https://drive.google.com/drive/folders/...)");
            _ui.WaitForEnter();
            return;
        }

        _ui.WriteMessage($"[green]ID da pasta detectado:[/] [cyan]{folderId.EscapeMarkup()}[/]");
        _ui.WriteEmptyLine();

        // ── API Key ──
        var apiKey = _ui.AskPassword("[bold yellow]Chave de API do Google Drive (API Key):[/]");

        // ── Nome da subpasta de versão ──
        var versionName = _ui.AskInput(
            "[bold yellow]Nome da versão dos instaladores[/] ([gray]ENTER para padrão: PrimeAcesso V5.9[/]):",
            "PrimeAcesso V5.9");

        if (string.IsNullOrWhiteSpace(versionName))
            versionName = "PrimeAcesso V5.9";

        var targetDir = Path.Combine(paths.InstallationPath, versionName);
        _ui.WriteMessage($"[gray]Os instaladores serão salvos em: {targetDir.EscapeMarkup()}[/]");
        _ui.WriteEmptyLine();

        // ── Confirmação ──
        if (!_ui.Confirm("[bold yellow]Iniciar download?[/]", true))
        {
            _ui.WriteMessage("[gray]Download cancelado.[/]");
            _ui.WaitForEnter();
            return;
        }

        // ── Download com progresso ──
        _ui.WriteEmptyLine();

        try
        {
            using var downloader = new GoogleDriveDownloader(apiKey);

            await _ui.ShowProgress("Baixando instaladores...", async update =>
            {
                var progress = new Progress<string>(message =>
                {
                    if (message.StartsWith("Pasta:", StringComparison.OrdinalIgnoreCase))
                        update(0, message);
                    else
                        update(2, message);
                });

                await downloader.DownloadFolderAsync(folderId, targetDir, progress);
                update(100, "Download concluído!");
            });

            // ── Resumo ──
            _ui.WriteEmptyLine();
            var fileCount = Directory.Exists(targetDir)
                ? Directory.GetFiles(targetDir, "*.*", SearchOption.AllDirectories).Length
                : 0;

            _ui.WritePanel(
                $"[green]✅ Download concluído com sucesso![/]\n\n[cyan]{fileCount}[/] arquivo(s) baixado(s) em:\n[gray]{targetDir.EscapeMarkup()}[/]",
                "RESUMO",
                "green");

            SummaryStore.Add("Download", versionName, true, $"{fileCount} arquivos em {targetDir}");
        }
        catch (HttpRequestException ex)
        {
            _ui.WriteError($"Falha na conexão com o Google Drive: {ex.Message.EscapeMarkup()}");
            _ui.WriteWarning("Verifique sua conexão com a internet e se a chave de API está correta.");
            SummaryStore.Add("Download", "Google Drive", false, ex.Message);
        }
        catch (Exception ex)
        {
            _ui.WriteError($"Falha ao baixar instaladores: {ex.Message.EscapeMarkup()}");
            SummaryStore.Add("Download", "Google Drive", false, ex.Message);
        }

        _ui.WriteEmptyLine();
        _ui.WaitForEnter();
    }
}
