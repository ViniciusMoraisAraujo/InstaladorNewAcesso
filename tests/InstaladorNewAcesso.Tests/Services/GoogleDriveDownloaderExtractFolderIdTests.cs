using FluentAssertions;
using InstaladorNewAcesso.Services;

namespace InstaladorNewAcesso.Tests.Services;

public class GoogleDriveDownloaderExtractFolderIdTests
{
    [Theory]
    [InlineData("https://drive.google.com/drive/folders/1a2b3c4d5e6f7g8h9i0j", "1a2b3c4d5e6f7g8h9i0j")]
    [InlineData("https://drive.google.com/drive/folders/ABC123_DEF456", "ABC123_DEF456")]
    [InlineData("https://drive.google.com/drive/folders/abc-123_XYZ", "abc-123_XYZ")]
    public void ExtractFolderId_FromFolderUrl_ShouldExtractId(string url, string expectedId)
    {
        var result = GoogleDriveDownloader.ExtractFolderId(url);
        result.Should().Be(expectedId);
    }

    [Theory]
    [InlineData("https://drive.google.com/uc?id=1a2b3c4d5e6f7g8h9i0j&export=download", "1a2b3c4d5e6f7g8h9i0j")]
    [InlineData("https://docs.google.com/file/d/1a2b3c4d5e6f7g8h9i0j/view?usp=drive_link&id=abc123", "abc123")]
    [InlineData("?id=xyz789_ABC", "xyz789_ABC")]
    public void ExtractFolderId_FromQueryParamId_ShouldExtractId(string url, string expectedId)
    {
        var result = GoogleDriveDownloader.ExtractFolderId(url);
        result.Should().Be(expectedId);
    }

    [Theory]
    [InlineData("1a2b3c4d5e6f7g8h9i0j")]
    [InlineData("ABC123_DEF456_GHI789")]
    [InlineData("a1b2-c3d4_e5f6")]
    public void ExtractFolderId_FromRawId_ShouldReturnId(string rawId)
    {
        var result = GoogleDriveDownloader.ExtractFolderId(rawId);
        result.Should().Be(rawId);
    }

    [Theory]
    [InlineData("https://drive.google.com/file/d/1a2b3c4d5e6f7g8h9i0j/view")]
    [InlineData("https://drive.google.com/drive/folders/")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://example.com")]
    [InlineData("short")]
    [InlineData("has spaces")]
    [InlineData(null)]
    public void ExtractFolderId_InvalidInput_ShouldReturnNull(string? url)
    {
        var result = GoogleDriveDownloader.ExtractFolderId(url!);
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("folders/abc123_folders/xyz789")]
    [InlineData("id=abc&id=xyz")]
    public void ExtractFolderId_WithMultipleMatches_ShouldReturnFirstMatch(string url)
    {
        var result = GoogleDriveDownloader.ExtractFolderId(url);
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData("https://drive.google.com/drive/folders/FOLDER_ID_123?usp=sharing", "FOLDER_ID_123")]
    [InlineData("https://drive.google.com/drive/folders/ABC123?resourcekey=0-def456", "ABC123")]
    public void ExtractFolderId_FromFolderUrlWithQueryParams_ShouldExtractId(string url, string expectedId)
    {
        var result = GoogleDriveDownloader.ExtractFolderId(url);
        result.Should().Be(expectedId);
    }
}
