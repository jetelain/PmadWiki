using System.Text;
using Microsoft.AspNetCore.Routing;
using Moq;
using Pmad.Wiki.Services;
using Pmad.Wiki.Test.Infrastructure;

namespace Pmad.Wiki.Test.Services;

public class WikiPageEditServiceTest
{
    private readonly Mock<IWikiPageService> _mockPageService;
    private readonly Mock<ITemporaryMediaStorageService> _mockTempMediaStorage;
    private readonly LinkGenerator _linkGenerator;
    private readonly WikiPageEditService _service;

    public WikiPageEditServiceTest()
    {
        _mockPageService = new Mock<IWikiPageService>();
        _mockTempMediaStorage = new Mock<ITemporaryMediaStorageService>();
        _linkGenerator = new TestLinkGenerator();

        _service = new WikiPageEditService(
            _mockPageService.Object,
            _mockTempMediaStorage.Object,
            _linkGenerator);
    }

    #region SavePageAsync Tests - Basic Scenarios

    [Fact]
    public async Task SavePageAsync_WithNoTempMedia_SavesPageWithoutMedia()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");
        var content = "# Test Page\n\nThis is a test.";
        var commitMessage = "Create test page";

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TemporaryMediaInfo>());

        // Act
        await _service.SavePageAsync("test", null, content, commitMessage, author, CancellationToken.None);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "test",
            null,
            content,
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(d => d.Count == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SavePageAsync_WithContentNotReferencingTempMedia_DoesNotProcessMedia()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");
        var content = "# Test Page\n\nNo media references here.";
        var commitMessage = "Create page";

        var tempMedia = new Dictionary<string, TemporaryMediaInfo>();

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempMedia);

        // Act
        await _service.SavePageAsync("test", null, content, commitMessage, author, CancellationToken.None);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "test",
            null,
            content,
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(d => d.Count == 0),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockTempMediaStorage.Verify(
            x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SavePageAsync_WithCulture_PassesCultureToPageService()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");
        var content = "# Page Française";
        var commitMessage = "Create French page";

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TemporaryMediaInfo>());

        // Act
        await _service.SavePageAsync("test", "fr", content, commitMessage, author, CancellationToken.None);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "test",
            "fr",
            content,
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(d => d.Count == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SavePageAsync_WithNestedPagePath_SavesCorrectly()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");
        var content = "# Admin Settings";
        var commitMessage = "Update settings";

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TemporaryMediaInfo>());

        // Act
        await _service.SavePageAsync("admin/settings", null, content, commitMessage, author, CancellationToken.None);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "admin/settings",
            null,
            content,
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(d => d.Count == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SavePageAsync Tests - Single Media File

    [Fact]
    public async Task SavePageAsync_WithSingleTempMedia_SavesPageWithMedia()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");
        var tempId = "abc123def456";
        var originalFileName = "screenshot.png";
        var mediaContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        var content = $"# Test Page\n\n![Screenshot](/wiki/tempmedia/{tempId})";
        var commitMessage = "Add page with image";

        var tempMedia = new Dictionary<string, TemporaryMediaInfo>
        {
            [tempId] = new TemporaryMediaInfo
            {
                TemporaryId = tempId,
                OriginalFileName = originalFileName,
                FilePath = "/temp/path.png",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempMedia);

        _mockTempMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(author, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        await _service.SavePageAsync("test", null, content, commitMessage, author, CancellationToken.None);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "test",
            null,
            It.Is<string>(c => c.Contains("![Screenshot](medias/screenshot_abc123def456.png)")),
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(m =>
                m.Count == 1 &&
                m.ContainsKey("medias/screenshot_abc123def456.png") &&
                m["medias/screenshot_abc123def456.png"].SequenceEqual(mediaContent)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SavePageAsync_WithTempMediaInNestedPage_UsesCorrectRelativePath()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");
        var tempId = "abc789def012";
        var originalFileName = "diagram.svg";
        var mediaContent = new byte[] { 0x3C, 0x73, 0x76, 0x67 }; // <svg

        var content = $"# Guide\n\n![Diagram](/wiki/tempmedia/{tempId})";
        var commitMessage = "Add guide with diagram";

        var tempMedia = new Dictionary<string, TemporaryMediaInfo>
        {
            [tempId] = new TemporaryMediaInfo
            {
                TemporaryId = tempId,
                OriginalFileName = originalFileName,
                FilePath = "/temp/path.svg",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempMedia);

        _mockTempMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(author, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        await _service.SavePageAsync("docs/guide", null, content, commitMessage, author, CancellationToken.None);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "docs/guide",
            null,
            It.Is<string>(c => c.Contains("![Diagram](medias/diagram_abc789def012.svg)")),
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(m =>
                m.Count == 1 &&
                m.ContainsKey("docs/medias/diagram_abc789def012.svg") &&
                m["docs/medias/diagram_abc789def012.svg"].SequenceEqual(mediaContent)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SavePageAsync_WithTempMediaNoExtension_UsesEmptyExtension()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");
        var tempId = "abcdef123456";
        var originalFileName = "datafile";
        var mediaContent = new byte[] { 0x01, 0x02, 0x03 };

        var content = $"# Page\n\n[Download](/wiki/tempmedia/{tempId})";
        var commitMessage = "Add page";

        var tempMedia = new Dictionary<string, TemporaryMediaInfo>
        {
            [tempId] = new TemporaryMediaInfo
            {
                TemporaryId = tempId,
                OriginalFileName = originalFileName,
                FilePath = "/temp/path",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempMedia);

        _mockTempMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(author, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        await _service.SavePageAsync("test", null, content, commitMessage, author, CancellationToken.None);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "test",
            null,
            It.Is<string>(c => c.Contains("[Download](medias/datafile_abcdef123456)")),
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(m =>
                m.Count == 1 &&
                m.ContainsKey("medias/datafile_abcdef123456") &&
                m["medias/datafile_abcdef123456"].SequenceEqual(mediaContent)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SavePageAsync_WithTempMediaMultipleExtensions_UsesFullExtension()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");
        var tempId = "fedcba987654";
        var originalFileName = "backup.tar.gz";
        var mediaContent = new byte[] { 0x1F, 0x8B };

        var content = $"# Backup\n\n[Download](/wiki/tempmedia/{tempId})";
        var commitMessage = "Add backup page";

        var tempMedia = new Dictionary<string, TemporaryMediaInfo>
        {
            [tempId] = new TemporaryMediaInfo
            {
                TemporaryId = tempId,
                OriginalFileName = originalFileName,
                FilePath = "/temp/backup.tar.gz",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempMedia);

        _mockTempMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(author, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        await _service.SavePageAsync("test", null, content, commitMessage, author, CancellationToken.None);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "test",
            null,
            It.Is<string>(c => c.Contains("[Download](medias/backup-tar_fedcba987654.gz)")),
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(m =>
                m.Count == 1 &&
                m.ContainsKey("medias/backup-tar_fedcba987654.gz") &&
                m["medias/backup-tar_fedcba987654.gz"].SequenceEqual(mediaContent)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SavePageAsync Tests - Multiple Media Files

    [Fact]
    public async Task SavePageAsync_WithMultipleTempMedia_SavesAllMedia()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");
        var tempId1 = "aabbccdd0001";
        var tempId2 = "aabbccdd0002";
        var tempId3 = "aabbccdd0003";

        var mediaContent1 = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var mediaContent2 = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var mediaContent3 = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        var content = $@"# Multi-Media Page

![Image 1](/wiki/tempmedia/{tempId1})

Some text here.

![Image 2](/wiki/tempmedia/{tempId2})

More text.

[Download PDF](/wiki/tempmedia/{tempId3})";

        var commitMessage = "Add multi-media page";

        var tempMedia = new Dictionary<string, TemporaryMediaInfo>
        {
            [tempId1] = new TemporaryMediaInfo
            {
                TemporaryId = tempId1,
                OriginalFileName = "photo1.png",
                FilePath = "/temp/photo1.png",
                CreatedAt = DateTimeOffset.UtcNow
            },
            [tempId2] = new TemporaryMediaInfo
            {
                TemporaryId = tempId2,
                OriginalFileName = "photo2.jpg",
                FilePath = "/temp/photo2.jpg",
                CreatedAt = DateTimeOffset.UtcNow
            },
            [tempId3] = new TemporaryMediaInfo
            {
                TemporaryId = tempId3,
                OriginalFileName = "document.pdf",
                FilePath = "/temp/document.pdf",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempMedia);

        _mockTempMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(author, tempId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent1);

        _mockTempMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(author, tempId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent2);

        _mockTempMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(author, tempId3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent3);

        // Act
        await _service.SavePageAsync("test", null, content, commitMessage, author, CancellationToken.None);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "test",
            null,
            It.Is<string>(c =>
                c.Contains("![Image 1](medias/photo1_aabbccdd0001.png)") &&
                c.Contains("![Image 2](medias/photo2_aabbccdd0002.jpg)") &&
                c.Contains("[Download PDF](medias/document_aabbccdd0003.pdf)")),
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(m =>
                m.Count == 3 &&
                m.ContainsKey("medias/photo1_aabbccdd0001.png") &&
                m.ContainsKey("medias/photo2_aabbccdd0002.jpg") &&
                m.ContainsKey("medias/document_aabbccdd0003.pdf") &&
                m["medias/photo1_aabbccdd0001.png"].SequenceEqual(mediaContent1) &&
                m["medias/photo2_aabbccdd0002.jpg"].SequenceEqual(mediaContent2) &&
                m["medias/document_aabbccdd0003.pdf"].SequenceEqual(mediaContent3)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SavePageAsync_WithDuplicateTempMediaReferences_ProcessesOnce()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");
        var tempId = "abcdef123789";
        var mediaContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        var content = $@"# Duplicate References

![First reference](/wiki/tempmedia/{tempId})

Some text.

![Second reference](/wiki/tempmedia/{tempId})

The same image appears twice.";

        var commitMessage = "Add page with duplicate refs";

        var tempMedia = new Dictionary<string, TemporaryMediaInfo>
        {
            [tempId] = new TemporaryMediaInfo
            {
                TemporaryId = tempId,
                OriginalFileName = "logo.png",
                FilePath = "/temp/logo.png",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempMedia);

        _mockTempMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(author, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        await _service.SavePageAsync("test", null, content, commitMessage, author, CancellationToken.None);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "test",
            null,
            It.Is<string>(c =>
                c.Contains("![First reference](medias/logo_abcdef123789.png)") &&
                c.Contains("![Second reference](medias/logo_abcdef123789.png)")),
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(m =>
                m.Count == 1 &&
                m.ContainsKey("medias/logo_abcdef123789.png") &&
                m["medias/logo_abcdef123789.png"].SequenceEqual(mediaContent)),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockTempMediaStorage.Verify(
            x => x.GetTemporaryMediaAsync(author, tempId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region SavePageAsync Tests - Edge Cases

    [Fact]
    public async Task SavePageAsync_WithTempMediaNotFound_SkipsMedia()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");
        var tempId = "ffffffffffff";

        var content = $"# Page\n\n![Image](/wiki/tempmedia/{tempId})";
        var commitMessage = "Add page";

        var tempMedia = new Dictionary<string, TemporaryMediaInfo>();

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempMedia);

        // Act
        await _service.SavePageAsync("test", null, content, commitMessage, author, CancellationToken.None);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "test",
            null,
            It.Is<string>(c => c.Contains("/wiki/tempmedia/ffffffffffff")),
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(m => m.Count == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SavePageAsync_WithTempMediaContentNull_SkipsMedia()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");
        var tempId = "deadbeefcafe";

        var content = $"# Page\n\n![Image](/wiki/tempmedia/{tempId})";
        var commitMessage = "Add page";

        var tempMedia = new Dictionary<string, TemporaryMediaInfo>
        {
            [tempId] = new TemporaryMediaInfo
            {
                TemporaryId = tempId,
                OriginalFileName = "missing.png",
                FilePath = "/temp/missing.png",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempMedia);

        _mockTempMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(author, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        await _service.SavePageAsync("test", null, content, commitMessage, author, CancellationToken.None);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "test",
            null,
            It.Is<string>(c => c.Contains("/wiki/tempmedia/deadbeefcafe")),
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(m => m.Count == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SavePageAsync_WithMixedValidAndInvalidTempMedia_ProcessesOnlyValid()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");
        var validId = "abcd1234ef56";
        var invalidId = "999999999999";
        var mediaContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        var content = $@"# Page

![Valid](/wiki/tempmedia/{validId})

![Invalid](/wiki/tempmedia/{invalidId})";

        var commitMessage = "Add page";

        var tempMedia = new Dictionary<string, TemporaryMediaInfo>
        {
            [validId] = new TemporaryMediaInfo
            {
                TemporaryId = validId,
                OriginalFileName = "valid.png",
                FilePath = "/temp/valid.png",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempMedia);

        _mockTempMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(author, validId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        await _service.SavePageAsync("test", null, content, commitMessage, author, CancellationToken.None);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "test",
            null,
            It.Is<string>(c =>
                c.Contains("![Valid](medias/valid_abcd1234ef56.png)") &&
                c.Contains("![Invalid](/wiki/tempmedia/999999999999)")),
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(m =>
                m.Count == 1 &&
                m.ContainsKey("medias/valid_abcd1234ef56.png") &&
                m["medias/valid_abcd1234ef56.png"].SequenceEqual(mediaContent)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SavePageAsync_WithEmptyContent_DoesNotThrow()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");
        var content = "";
        var commitMessage = "Create empty page";

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TemporaryMediaInfo>());

        // Act
        await _service.SavePageAsync("test", null, content, commitMessage, author, CancellationToken.None);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "test",
            null,
            "",
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(d => d.Count == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SavePageAsync_WithHexTempId_HandlesCorrectly()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");
        var tempId = "a1b2c3d4e5f6";
        var mediaContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        var content = $"# Page\n\n![Image](/wiki/tempmedia/{tempId})";
        var commitMessage = "Add page";

        var tempMedia = new Dictionary<string, TemporaryMediaInfo>
        {
            [tempId] = new TemporaryMediaInfo
            {
                TemporaryId = tempId,
                OriginalFileName = "image.png",
                FilePath = "/temp/image.png",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempMedia);

        _mockTempMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(author, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        await _service.SavePageAsync("test", null, content, commitMessage, author, CancellationToken.None);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "test",
            null,
            It.Is<string>(c => c.Contains("![Image](medias/image_a1b2c3d4e5f6.png)")),
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(m =>
                m.Count == 1 &&
                m.ContainsKey("medias/image_a1b2c3d4e5f6.png") &&
                m["medias/image_a1b2c3d4e5f6.png"].SequenceEqual(mediaContent)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SavePageAsync Tests - Content Replacement

    [Fact]
    public async Task SavePageAsync_ReplacesOnlyTempMediaUrls_PreservesOtherUrls()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");
        var tempId = "cafe12345678";
        var mediaContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        var content = $@"# Page

![Temp Image](/wiki/tempmedia/{tempId})

![External Image](https://example.com/image.png)

![Relative Image](../images/other.png)

[Link](/wiki/view/OtherPage)";

        var commitMessage = "Add page";

        var tempMedia = new Dictionary<string, TemporaryMediaInfo>
        {
            [tempId] = new TemporaryMediaInfo
            {
                TemporaryId = tempId,
                OriginalFileName = "temp.png",
                FilePath = "/temp/temp.png",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempMedia);

        _mockTempMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(author, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        await _service.SavePageAsync("test", null, content, commitMessage, author, CancellationToken.None);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "test",
            null,
            It.Is<string>(c =>
                c.Contains("![Temp Image](medias/temp_cafe12345678.png)") &&
                c.Contains("![External Image](https://example.com/image.png)") &&
                c.Contains("![Relative Image](../images/other.png)") &&
                c.Contains("[Link](/wiki/view/OtherPage)") &&
                !c.Contains("/wiki/tempmedia/")),
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(m =>
                m.Count == 1 &&
                m.ContainsKey("medias/temp_cafe12345678.png") &&
                m["medias/temp_cafe12345678.png"].SequenceEqual(mediaContent)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SavePageAsync_WithTempMediaInHtmlTags_ReplacesCorrectly()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");
        var tempId = "dada123beeee";
        var mediaContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        var content = $@"# Page

<img src=""/wiki/tempmedia/{tempId}"" alt=""Image"" />

<a href=""/wiki/tempmedia/{tempId}"">Download</a>";

        var commitMessage = "Add page";

        var tempMedia = new Dictionary<string, TemporaryMediaInfo>
        {
            [tempId] = new TemporaryMediaInfo
            {
                TemporaryId = tempId,
                OriginalFileName = "file.png",
                FilePath = "/temp/file.png",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempMedia);

        _mockTempMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(author, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        await _service.SavePageAsync("test", null, content, commitMessage, author, CancellationToken.None);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "test",
            null,
            It.Is<string>(c =>
                c.Contains($@"<img src=""medias/file_dada123beeee.png"" alt=""Image"" />") &&
                c.Contains($@"<a href=""medias/file_dada123beeee.png"">Download</a>")),
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(m =>
                m.Count == 1 &&
                m.ContainsKey("medias/file_dada123beeee.png") &&
                m["medias/file_dada123beeee.png"].SequenceEqual(mediaContent)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SavePageAsync Tests - CancellationToken

    [Fact]
    public async Task SavePageAsync_PassesCancellationTokenToServices()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");
        var content = "# Test";
        var commitMessage = "Create page";
        var cancellationToken = new CancellationToken();

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, cancellationToken))
            .ReturnsAsync(new Dictionary<string, TemporaryMediaInfo>());

        // Act
        await _service.SavePageAsync("test", null, content, commitMessage, author, cancellationToken);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "test",
            null,
            content,
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(d => d.Count == 0),
            cancellationToken), Times.Once);
    }

    [Fact]
    public async Task SavePageAsync_WithTempMedia_PassesCancellationTokenToGetMedia()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");
        var tempId = "fade00000000";
        var mediaContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var content = $"# Page\n\n![Image](/wiki/tempmedia/{tempId})";
        var commitMessage = "Add page";
        var cancellationToken = new CancellationToken();

        var tempMedia = new Dictionary<string, TemporaryMediaInfo>
        {
            [tempId] = new TemporaryMediaInfo
            {
                TemporaryId = tempId,
                OriginalFileName = "image.png",
                FilePath = "/temp/image.png",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, cancellationToken))
            .ReturnsAsync(tempMedia);

        _mockTempMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(author, tempId, cancellationToken))
            .ReturnsAsync(mediaContent);

        // Act
        await _service.SavePageAsync("test", null, content, commitMessage, author, cancellationToken);

        // Assert
        _mockTempMediaStorage.Verify(
            x => x.GetUserTemporaryMediaAsync(author, cancellationToken),
            Times.Once);

        _mockTempMediaStorage.Verify(
            x => x.GetTemporaryMediaAsync(author, tempId, cancellationToken),
            Times.Once);

        _mockPageService.Verify(
            x => x.SavePageWithMediaAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IWikiUser>(),
                It.Is<Dictionary<string, byte[]>>(m =>
                    m.Count == 1 &&
                    m.ContainsKey("medias/image_fade00000000.png") &&
                    m["medias/image_fade00000000.png"].SequenceEqual(mediaContent)),
                cancellationToken),
            Times.Once);
    }

    #endregion

    #region SavePageAsync Tests - Integration Scenarios

    [Fact]
    public async Task SavePageAsync_CompleteWorkflow_ProcessesCorrectly()
    {
        // Arrange
        var author = CreateMockUser("author@example.com", "Author Name");
        var tempId1 = "abcdef000001";
        var tempId2 = "abcdef000002";

        var mediaContent1 = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG
        var mediaContent2 = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 }; // JPEG

        var content = $@"# Tutorial

## Introduction

This tutorial includes two images.

### First Section

![Screenshot 1](/wiki/tempmedia/{tempId1})

Here is the first screenshot showing the main interface.

### Second Section

![Screenshot 2](/wiki/tempmedia/{tempId2})

This is the second screenshot.

## External Resources

You can also check [this external image](https://example.com/banner.png).

## Conclusion

That's it!";

        var commitMessage = "Add complete tutorial with images";

        var tempMedia = new Dictionary<string, TemporaryMediaInfo>
        {
            [tempId1] = new TemporaryMediaInfo
            {
                TemporaryId = tempId1,
                OriginalFileName = "screenshot-1.png",
                FilePath = "/temp/screenshot-1.png",
                CreatedAt = DateTimeOffset.UtcNow
            },
            [tempId2] = new TemporaryMediaInfo
            {
                TemporaryId = tempId2,
                OriginalFileName = "screenshot-2.jpg",
                FilePath = "/temp/screenshot-2.jpg",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempMedia);

        _mockTempMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(author, tempId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent1);

        _mockTempMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(author, tempId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent2);

        // Act
        await _service.SavePageAsync("docs/tutorial", null, content, commitMessage, author, CancellationToken.None);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "docs/tutorial",
            null,
            It.Is<string>(c =>
                c.Contains("![Screenshot 1](medias/screenshot-1_abcdef000001.png)") &&
                c.Contains("![Screenshot 2](medias/screenshot-2_abcdef000002.jpg)")),
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(m =>
                m.Count == 2 &&
                m.ContainsKey("docs/medias/screenshot-1_abcdef000001.png") &&
                m.ContainsKey("docs/medias/screenshot-2_abcdef000002.jpg") &&
                m["docs/medias/screenshot-1_abcdef000001.png"].SequenceEqual(mediaContent1) &&
                m["docs/medias/screenshot-2_abcdef000002.jpg"].SequenceEqual(mediaContent2)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SavePageAsync_WithVariousMediaTypes_HandlesAllCorrectly()
    {
        // Arrange
        var author = CreateMockUser("user@example.com", "Test User");

        var pngId = "aabbccddee01";
        var jpgId = "aabbccddee02";
        var gifId = "aabbccddee03";
        var svgId = "aabbccddee04";
        var pdfId = "aabbccddee05";
        var mp4Id = "aabbccddee06";

        var content = $@"# Media Gallery

![PNG Image](/wiki/tempmedia/{pngId})
![JPEG Image](/wiki/tempmedia/{jpgId})
![GIF Animation](/wiki/tempmedia/{gifId})
![SVG Graphic](/wiki/tempmedia/{svgId})
[PDF Document](/wiki/tempmedia/{pdfId})
<video src=""/wiki/tempmedia/{mp4Id}"" controls></video>";

        var commitMessage = "Add media gallery";

        var tempMedia = new Dictionary<string, TemporaryMediaInfo>
        {
            [pngId] = new TemporaryMediaInfo
            {
                TemporaryId = pngId,
                OriginalFileName = "image.png",
                FilePath = "/temp/image.png",
                CreatedAt = DateTimeOffset.UtcNow
            },
            [jpgId] = new TemporaryMediaInfo
            {
                TemporaryId = jpgId,
                OriginalFileName = "photo.jpg",
                FilePath = "/temp/photo.jpg",
                CreatedAt = DateTimeOffset.UtcNow
            },
            [gifId] = new TemporaryMediaInfo
            {
                TemporaryId = gifId,
                OriginalFileName = "anim.gif",
                FilePath = "/temp/anim.gif",
                CreatedAt = DateTimeOffset.UtcNow
            },
            [svgId] = new TemporaryMediaInfo
            {
                TemporaryId = svgId,
                OriginalFileName = "vector.svg",
                FilePath = "/temp/vector.svg",
                CreatedAt = DateTimeOffset.UtcNow
            },
            [pdfId] = new TemporaryMediaInfo
            {
                TemporaryId = pdfId,
                OriginalFileName = "doc.pdf",
                FilePath = "/temp/doc.pdf",
                CreatedAt = DateTimeOffset.UtcNow
            },
            [mp4Id] = new TemporaryMediaInfo
            {
                TemporaryId = mp4Id,
                OriginalFileName = "video.mp4",
                FilePath = "/temp/video.mp4",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockTempMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(author, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempMedia);

        foreach (var kvp in tempMedia)
        {
            _mockTempMediaStorage
                .Setup(x => x.GetTemporaryMediaAsync(author, kvp.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new byte[] { 0x00, 0x01, 0x02 });
        }

        // Act
        await _service.SavePageAsync("gallery", null, content, commitMessage, author, CancellationToken.None);

        // Assert
        _mockPageService.Verify(x => x.SavePageWithMediaAsync(
            "gallery",
            null,
            It.Is<string>(c =>
                c.Contains("![PNG Image](medias/image_aabbccddee01.png)") &&
                c.Contains("![JPEG Image](medias/photo_aabbccddee02.jpg)") &&
                c.Contains("![GIF Animation](medias/anim_aabbccddee03.gif)") &&
                c.Contains("![SVG Graphic](medias/vector_aabbccddee04.svg)") &&
                c.Contains("[PDF Document](medias/doc_aabbccddee05.pdf)") &&
                c.Contains($@"<video src=""medias/video_aabbccddee06.mp4"" controls></video>")),
            commitMessage,
            author,
            It.Is<Dictionary<string, byte[]>>(m =>
                m.Count == 6 &&
                m.ContainsKey("medias/image_aabbccddee01.png") &&
                m.ContainsKey("medias/photo_aabbccddee02.jpg") &&
                m.ContainsKey("medias/anim_aabbccddee03.gif") &&
                m.ContainsKey("medias/vector_aabbccddee04.svg") &&
                m.ContainsKey("medias/doc_aabbccddee05.pdf") &&
                m.ContainsKey("medias/video_aabbccddee06.mp4")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static IWikiUser CreateMockUser(string email, string name)
    {
        var mockUser = new Mock<IWikiUser>();
        mockUser.Setup(x => x.GitEmail).Returns(email);
        mockUser.Setup(x => x.GitName).Returns(name);
        mockUser.Setup(x => x.DisplayName).Returns(name);
        return mockUser.Object;
    }

    #endregion
}
