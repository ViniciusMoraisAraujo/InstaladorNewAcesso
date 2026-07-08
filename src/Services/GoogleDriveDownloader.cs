namespace InstaladorNewAcesso.Services;

public class GoogleDriveDownloader
{
    private const string ApiBase = "https://www.googleapis.com/drive/v3";

    private readonly string _apiKey;
    private readonly HttpClient _http;

    public GoogleDriveDownloader(string apiKey)
    {
        _apiKey = apiKey;
        _http = new HttpClient();
        _http.Timeout = TimeSpan.FromMinutes(10);
    }
    
    public static string? ExtractFolderId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var match = System.Text.RegularExpressions.Regex.Match(
            url, @"folders/([a-zA-Z0-9_-]+)");
        if (match.Success) return match.Groups[1].Value;

        match = System.Text.RegularExpressions.Regex.Match(
            url, @"[?&]id=([a-zA-Z0-9_-]+)");
        if (match.Success) return match.Groups[1].Value;

        if (System.Text.RegularExpressions.Regex.IsMatch(url, @"^[a-zA-Z0-9_-]{10,}$"))
            return url;

        return null;
    }

    public async Task DownloadFolderAsync(string folderId, string localRoot, IProgress<string>? progress = null)
    {
        Directory.CreateDirectory(localRoot);
        await DownloadFolderRecursiveAsync(folderId, localRoot, progress);
    }

    private async Task DownloadFolderRecursiveAsync(string folderId, string localPath, IProgress<string>? progress)
    {
        string? pageToken = null;

        do
        {
            var url = $"{ApiBase}/files" +
                      $"?q={Uri.EscapeDataString($"'{folderId}' in parents and trashed=false")}" +
                      $"&fields=nextPageToken,files(id,name,mimeType)" +
                      $"&pageSize=1000" +
                      $"&key={_apiKey}" +
                      (pageToken != null ? $"&pageToken={pageToken}" : "");

            var json = await _http.GetStringAsync(url);
            var response = System.Text.Json.JsonSerializer.Deserialize<DriveListResponse>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (response?.Files == null) break;

            foreach (var item in response.Files)
            {
                if (item.MimeType == "application/vnd.google-apps.folder")
                {
                    var subFolder = Path.Combine(localPath, item.Name);
                    Directory.CreateDirectory(subFolder);
                    progress?.Report($"Pasta: {item.Name}");
                    await DownloadFolderRecursiveAsync(item.Id, subFolder, progress);
                }
                else
                {
                    var filePath = Path.Combine(localPath, item.Name);
                    progress?.Report($"Baixando: {item.Name}");
                    await DownloadFileAsync(item.Id, filePath);
                }
            }

            pageToken = response.NextPageToken;

        } while (pageToken != null);
    }

    private async Task DownloadFileAsync(string fileId, string destPath)
    {
        var url = $"{ApiBase}/files/{fileId}?alt=media&key={_apiKey}";

        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fs);
    }
    private class DriveListResponse
    {
        public List<DriveFile>? Files { get; set; }
        public string? NextPageToken { get; set; }
    }

    private class DriveFile
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string MimeType { get; set; } = "";
    }
}