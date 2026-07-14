using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using InstaladorNewAcesso.Core.Services;

namespace InstaladorNewAcesso.Tests.Services;

public class GoogleDriveDownloaderTests : IDisposable
{
    private readonly string _tempRoot;
    private const string ApiKey = "fake-api-key";

    public GoogleDriveDownloaderTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "GoogleDriveDownloaderTests_" + Guid.NewGuid().ToString("N"));
    }

    /// <summary>
    /// Um arquivo único na raiz da pasta.
    /// Esperado: arquivo baixado no diretório local.
    /// </summary>
    [Fact]
    public async Task DownloadFolderAsync_SingleFile_ShouldDownloadFile()
    {
        // Arrange
        var handler = new MockHttpHandler();
        var folderId = "root-folder-id";
        var fileId = "file-001";
        var fileName = "Setup.exe";

        // 1) List files → retorna 1 arquivo
        handler.QueueFileListResponse(folderId, [
            new DriveFileStub { Id = fileId, Name = fileName, MimeType = "application/octet-stream" }
        ]);

        // 2) Download do arquivo → conteúdo binário
        var fileContent = "binary-content"u8.ToArray();
        handler.QueueDownloadResponse(fileContent);

        using var downloader = new GoogleDriveDownloader(ApiKey, handler.CreateClient());

        // Act
        await downloader.DownloadFolderAsync(folderId, _tempRoot);

        // Assert
        var downloadedFile = Path.Combine(_tempRoot, fileName);
        File.Exists(downloadedFile).Should().BeTrue();
        var actualBytes = await File.ReadAllBytesAsync(downloadedFile);
        actualBytes.Should().Equal(fileContent);

        // Verifica que fez 2 requisições: 1 list + 1 download
        handler.Requests.Should().HaveCount(2);
        handler.Requests[0].RequestUri!.ToString().Should().Contain("files?q=");
        handler.Requests[1].RequestUri!.ToString().Should().Contain($"/files/{fileId}?alt=media");
    }

    /// <summary>
    /// Uma pasta com uma subpasta contendo um arquivo.
    /// Esperado: arquivo baixado em subdiretório aninhado.
    /// </summary>
    [Fact]
    public async Task DownloadFolderAsync_WithSubfolder_ShouldNestDirectories()
    {
        // Arrange
        var handler = new MockHttpHandler();
        var rootId = "root-folder";
        var subFolderId = "sub-folder-001";
        var subFolderName = "SubPasta";
        var fileId = "file-002";
        var fileName = "data.txt";

        // 1) List root → retorna 1 subpasta
        handler.QueueFileListResponse(rootId, [
            new DriveFileStub { Id = subFolderId, Name = subFolderName, MimeType = "application/vnd.google-apps.folder" }
        ]);

        // 2) List subfolder → retorna 1 arquivo
        handler.QueueFileListResponse(subFolderId, [
            new DriveFileStub { Id = fileId, Name = fileName, MimeType = "text/plain" }
        ]);

        // 3) Download do arquivo
        var content = "hello from subfolder"u8.ToArray();
        handler.QueueDownloadResponse(content);

        using var downloader = new GoogleDriveDownloader(ApiKey, handler.CreateClient());

        // Act
        await downloader.DownloadFolderAsync(rootId, _tempRoot);

        // Assert
        var downloadedFile = Path.Combine(_tempRoot, subFolderName, fileName);
        File.Exists(downloadedFile).Should().BeTrue();
        var actualBytes = await File.ReadAllBytesAsync(downloadedFile);
        actualBytes.Should().Equal(content);

        handler.Requests.Should().HaveCount(3);
    }

    /// <summary>
    /// Pasta vazia (sem arquivos). Nada deve ser baixado.
    /// </summary>
    [Fact]
    public async Task DownloadFolderAsync_EmptyFolder_ShouldNotCreateFiles()
    {
        // Arrange
        var handler = new MockHttpHandler();
        handler.QueueFileListResponse("empty-folder", []);

        using var downloader = new GoogleDriveDownloader(ApiKey, handler.CreateClient());

        // Act
        await downloader.DownloadFolderAsync("empty-folder", _tempRoot);

        // Assert
        Directory.Exists(_tempRoot).Should().BeTrue();
        Directory.GetFiles(_tempRoot, "*", SearchOption.AllDirectories).Should().BeEmpty();
        handler.Requests.Should().ContainSingle();
    }

    /// <summary>
    /// Paginação: duas páginas de resultados.
    /// </summary>
    [Fact]
    public async Task DownloadFolderAsync_WithPagination_ShouldFetchAllPages()
    {
        // Arrange
        var handler = new MockHttpHandler();
        var folderId = "paged-folder";
        var file1Id = "page1-file";
        var file2Id = "page2-file";

        // A ordem importa! O código de produção:
        //   1) List page 1 → encontra 1 arquivo
        //   2) Download do arquivo (antes de buscar página 2)
        //   3) List page 2 (com pageToken) → encontra 1 arquivo
        //   4) Download do arquivo
        handler.QueueFileListResponse(folderId, [new DriveFileStub { Id = file1Id, Name = "file1.txt", MimeType = "text/plain" }], nextPageToken: "page2-token");
        handler.QueueDownloadResponse("file1-content"u8.ToArray());
        handler.QueueFileListResponse(folderId, [new DriveFileStub { Id = file2Id, Name = "file2.txt", MimeType = "text/plain" }]);
        handler.QueueDownloadResponse("file2-content"u8.ToArray());

        using var downloader = new GoogleDriveDownloader(ApiKey, handler.CreateClient());

        // Act
        await downloader.DownloadFolderAsync(folderId, _tempRoot);

        // Assert
        File.Exists(Path.Combine(_tempRoot, "file1.txt")).Should().BeTrue();
        File.Exists(Path.Combine(_tempRoot, "file2.txt")).Should().BeTrue();
        // 2 list calls + 2 downloads = 4 requests
        handler.Requests.Should().HaveCount(4);
        // A ordem: list page1 → download file1 → list page2 (com pageToken) → download file2
        handler.Requests[2].RequestUri!.ToString().Should().Contain("pageToken=page2-token");
    }

    /// <summary>
    /// API retorna erro (ex: 403 Forbidden).
    /// </summary>
    [Fact]
    public async Task DownloadFolderAsync_ApiError_ShouldThrowHttpRequestException()
    {
        // Arrange
        var handler = new MockHttpHandler();
        handler.QueueResponse(new HttpResponseMessage(HttpStatusCode.Forbidden));

        using var downloader = new GoogleDriveDownloader(ApiKey, handler.CreateClient());

        // Act
        var act = () => downloader.DownloadFolderAsync("any-folder", _tempRoot);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    /// <summary>
    /// API retorna JSON inválido.
    /// </summary>
    [Fact]
    public async Task DownloadFolderAsync_InvalidJson_ShouldThrowJsonException()
    {
        // Arrange
        var handler = new MockHttpHandler();
        handler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-valid-json")
        });

        using var downloader = new GoogleDriveDownloader(ApiKey, handler.CreateClient());

        // Act
        var act = () => downloader.DownloadFolderAsync("any-folder", _tempRoot);

        // Assert
        await act.Should().ThrowAsync<JsonException>();
    }

    /// <summary>
    /// Diretório raiz é criado quando não existe.
    /// </summary>
    [Fact]
    public async Task DownloadFolderAsync_ShouldCreateRootDirectory()
    {
        // Arrange
        var handler = new MockHttpHandler();
        handler.QueueFileListResponse("empty", []);

        using var downloader = new GoogleDriveDownloader(ApiKey, handler.CreateClient());

        // _tempRoot não existe ainda
        Directory.Exists(_tempRoot).Should().BeFalse();

        // Act
        await downloader.DownloadFolderAsync("empty", _tempRoot);

        // Assert
        Directory.Exists(_tempRoot).Should().BeTrue();
    }

    /// <summary>
    /// Vários arquivos na mesma pasta.
    /// </summary>
    [Fact]
    public async Task DownloadFolderAsync_MultipleFiles_ShouldDownloadAll()
    {
        // Arrange
        var handler = new MockHttpHandler();
        var folderId = "multi-folder";

        handler.QueueFileListResponse(folderId, [
            new DriveFileStub { Id = "f1", Name = "alpha.dll", MimeType = "application/octet-stream" },
            new DriveFileStub { Id = "f2", Name = "beta.exe", MimeType = "application/octet-stream" },
            new DriveFileStub { Id = "f3", Name = "gamma.config", MimeType = "text/plain" }
        ]);

        handler.QueueDownloadResponse("alpha"u8.ToArray());
        handler.QueueDownloadResponse("beta"u8.ToArray());
        handler.QueueDownloadResponse("gamma"u8.ToArray());

        using var downloader = new GoogleDriveDownloader(ApiKey, handler.CreateClient());

        // Act
        await downloader.DownloadFolderAsync(folderId, _tempRoot);

        // Assert
        File.Exists(Path.Combine(_tempRoot, "alpha.dll")).Should().BeTrue();
        File.Exists(Path.Combine(_tempRoot, "beta.exe")).Should().BeTrue();
        File.Exists(Path.Combine(_tempRoot, "gamma.config")).Should().BeTrue();
    }

    /// <summary>
    /// Progress é reportado corretamente (pastas e arquivos).
    /// </summary>
    [Fact]
    public async Task DownloadFolderAsync_ShouldReportProgress()
    {
        // Arrange
        var handler = new MockHttpHandler();
        var rootId = "progress-root";
        var subId = "progress-sub";
        var fileId = "progress-file";

        // Root → 1 subpasta
        handler.QueueFileListResponse(rootId, [
            new DriveFileStub { Id = subId, Name = "SubFolder", MimeType = "application/vnd.google-apps.folder" }
        ]);
        // Subpasta → 1 arquivo
        handler.QueueFileListResponse(subId, [
            new DriveFileStub { Id = fileId, Name = "doc.txt", MimeType = "text/plain" }
        ]);
        // Download
        handler.QueueDownloadResponse("doc content"u8.ToArray());

        var progressMessages = new List<string>();
        var progress = new Progress<string>(msg => progressMessages.Add(msg));

        using var downloader = new GoogleDriveDownloader(ApiKey, handler.CreateClient());

        // Act
        await downloader.DownloadFolderAsync(rootId, _tempRoot, progress);

        // Assert
        progressMessages.Should().HaveCount(2);
        progressMessages[0].Should().Be("Pasta: SubFolder");
        progressMessages[1].Should().Be("Baixando: doc.txt");
    }

    /// <summary>
    /// DownloadFolderAsync com subpastas aninhadas em múltiplos níveis.
    /// </summary>
    [Fact]
    public async Task DownloadFolderAsync_DeeplyNestedFolders_ShouldCreateAllDirectories()
    {
        // Arrange
        var handler = new MockHttpHandler();
        var l1 = "level1";
        var l2 = "level2";
        var l3 = "level3";
        var fileId = "deep-file";

        handler.QueueFileListResponse(l1, [
            new DriveFileStub { Id = l2, Name = "L2", MimeType = "application/vnd.google-apps.folder" }
        ]);
        handler.QueueFileListResponse(l2, [
            new DriveFileStub { Id = l3, Name = "L3", MimeType = "application/vnd.google-apps.folder" }
        ]);
        handler.QueueFileListResponse(l3, [
            new DriveFileStub { Id = fileId, Name = "deep.txt", MimeType = "text/plain" }
        ]);
        handler.QueueDownloadResponse("deep content"u8.ToArray());

        using var downloader = new GoogleDriveDownloader(ApiKey, handler.CreateClient());

        // Act
        await downloader.DownloadFolderAsync(l1, _tempRoot);

        // Assert
        var deepPath = Path.Combine(_tempRoot, "L2", "L3", "deep.txt");
        File.Exists(deepPath).Should().BeTrue();
    }

    /// <summary>
    /// Files list retorna null (resposta inesperada da API).
    /// Não deve lançar exceção, apenas parar.
    /// </summary>
    [Fact]
    public async Task DownloadFolderAsync_NullFilesInResponse_ShouldNotThrow()
    {
        // Arrange
        var handler = new MockHttpHandler();
        // Resposta JSON com Files = null (ou omitido)
        var json = """{"nextPageToken": null}""";
        handler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        using var downloader = new GoogleDriveDownloader(ApiKey, handler.CreateClient());

        // Act
        var act = () => downloader.DownloadFolderAsync("folder-id", _tempRoot);

        // Assert
        await act.Should().NotThrowAsync();
        Directory.Exists(_tempRoot).Should().BeTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, true); }
            catch { /* cleanup on best-effort basis */ }
        }
    }

    // ============================================================
    //  Mock helper: substituto para HttpMessageHandler
    // ============================================================

    private class MockHttpHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();
        public List<HttpRequestMessage> Requests { get; } = new();

        public void QueueResponse(HttpResponseMessage response)
        {
            _responses.Enqueue(response);
        }

        public void QueueFileListResponse(string folderId, DriveFileStub[] files, string? nextPageToken = null)
        {
            var items = files.Select(f => new
            {
                id = f.Id,
                name = f.Name,
                mimeType = f.MimeType
            }).ToArray();

            var body = new
            {
                nextPageToken = nextPageToken,
                files = items
            };

            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }

        public void QueueDownloadResponse(byte[] content)
        {
            QueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content)
            });
        }

        public HttpClient CreateClient()
        {
            return new HttpClient(this);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var response = _responses.Count > 0
                ? _responses.Dequeue()
                : new HttpResponseMessage(HttpStatusCode.NotFound);
            return Task.FromResult(response);
        }
    }

    // Helper record para evitar depender de internal classes do downloader
    private record DriveFileStub
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string MimeType { get; init; } = "";
    }
}
