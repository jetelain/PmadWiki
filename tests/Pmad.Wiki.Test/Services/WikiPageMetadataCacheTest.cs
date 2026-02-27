using System.Text;
using Microsoft.Extensions.Options;
using Moq;
using Pmad.Git.HttpServer;
using Pmad.Git.LocalRepositories;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Services;

public class WikiPageMetadataCacheTest
{
    private readonly Mock<IGitRepositoryService> _mockGitRepositoryService;
    private readonly Mock<IGitRepository> _mockRepository;
    private readonly WikiOptions _options;
    private readonly WikiPageMetadataCache _service;

    public WikiPageMetadataCacheTest()
    {
        _mockGitRepositoryService = new Mock<IGitRepositoryService>();
        _mockRepository = new Mock<IGitRepository>();

        _options = new WikiOptions
        {
            RepositoryRoot = "/test/repos",
            WikiRepositoryName = "wiki",
            BranchName = "main",
            NeutralMarkdownPageCulture = "en"
        };

        var optionsWrapper = Options.Create(_options);

        _mockGitRepositoryService
            .Setup(x => x.GetRepositoryByPath(It.IsAny<string>()))
            .Returns(_mockRepository.Object);

        _service = new WikiPageMetadataCache(
            _mockGitRepositoryService.Object,
            optionsWrapper);
    }

    [Fact]
    public async Task GetPageMetadataAsync_WhenNotCached_ReadsFromRepository()
    {
        // Arrange
        var content = "# My Page Title\n\nSome content.";
        var contentBytes = Encoding.UTF8.GetBytes(content);

        _mockRepository
            .Setup(x => x.ReadFileAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentBytes);

        // Act
        var metadata = await _service.GetPageMetadataAsync("test", null, CancellationToken.None);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("My Page Title", metadata.Title);
        _mockRepository.Verify(x => x.ReadFileAsync("test.md", "main", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPageMetadataAsync_WhenCached_DoesNotReadFromRepository()
    {
        // Arrange
        var content = "# Cached Title\n\nContent.";
        var contentBytes = Encoding.UTF8.GetBytes(content);

        _mockRepository
            .Setup(x => x.ReadFileAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentBytes);

        // First call to populate cache
        await _service.GetPageMetadataAsync("test", null, CancellationToken.None);

        // Act - Second call should use cache
        var metadata = await _service.GetPageMetadataAsync("test", null, CancellationToken.None);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Cached Title", metadata.Title);
        _mockRepository.Verify(x => x.ReadFileAsync("test.md", "main", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPageMetadataAsync_WithNoH1_ReturnsFallback()
    {
        // Arrange
        var content = "Just some text without a title.";
        var contentBytes = Encoding.UTF8.GetBytes(content);

        _mockRepository
            .Setup(x => x.ReadFileAsync("fallback.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentBytes);

        // Act
        var metadata = await _service.GetPageMetadataAsync("fallback", null, CancellationToken.None);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("fallback", metadata.Title);
    }

    [Fact]
    public async Task GetPageMetadataAsync_WithCulture_UsesCultureSpecificFile()
    {
        // Arrange
        var content = "# Titre en Français\n\nContenu.";
        var contentBytes = Encoding.UTF8.GetBytes(content);

        _mockRepository
            .Setup(x => x.ReadFileAsync("test.fr.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentBytes);

        // Act
        var metadata = await _service.GetPageMetadataAsync("test", "fr", CancellationToken.None);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Titre en Français", metadata.Title);
        _mockRepository.Verify(x => x.ReadFileAsync("test.fr.md", "main", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPageMetadataAsync_WithNestedPage_ExtractsTitle()
    {
        // Arrange
        var content = "# Admin Settings\n\nConfiguration page.";
        var contentBytes = Encoding.UTF8.GetBytes(content);

        _mockRepository
            .Setup(x => x.ReadFileAsync("admin/settings.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentBytes);

        // Act
        var metadata = await _service.GetPageMetadataAsync("admin/settings", null, CancellationToken.None);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Admin Settings", metadata.Title);
    }

    [Fact]
    public async Task GetPageMetadataAsync_WithNestedPageNoH1_ReturnsLastPart()
    {
        // Arrange
        var content = "Just content without title.";
        var contentBytes = Encoding.UTF8.GetBytes(content);

        _mockRepository
            .Setup(x => x.ReadFileAsync("admin/settings.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentBytes);

        // Act
        var metadata = await _service.GetPageMetadataAsync("admin/settings", null, CancellationToken.None);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("settings", metadata.Title);
    }

    [Fact]
    public async Task GetPageMetadataAsync_WhenPageDoesNotExist_ReturnsNull()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.ReadFileAsync("nonexistent.md", "main", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException());

        // Act
        var metadata = await _service.GetPageMetadataAsync("nonexistent", null, CancellationToken.None);

        // Assert
        Assert.Null(metadata);
    }

    [Fact]
    public async Task ExtractAndCacheMetadata_OverwritesExistingCache()
    {
        // Arrange
        var oldContent = "# Old Title\n\nContent.";
        var newContent = "# New Title\n\nContent.";
        var contentBytes = Encoding.UTF8.GetBytes(oldContent);

        _mockRepository
            .Setup(x => x.ReadFileAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentBytes);

        // First populate cache with old title
        await _service.GetPageMetadataAsync("test", null, CancellationToken.None);

        // Act - Set new metadata
        _service.ExtractAndCacheMetadata("test", null, newContent);

        // Assert - Should return new title without calling repository
        var metadata = await _service.GetPageMetadataAsync("test", null, CancellationToken.None);
        Assert.NotNull(metadata);
        Assert.Equal("New Title", metadata.Title);
        _mockRepository.Verify(x => x.ReadFileAsync("test.md", "main", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CacheKeys_DifferentForDifferentCultures()
    {
        // Arrange
        var enContent = "# English Title\n\nContent.";
        var frContent = "# Titre Français\n\nContent.";

        _mockRepository
            .Setup(x => x.ReadFileAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(enContent));

        _mockRepository
            .Setup(x => x.ReadFileAsync("test.fr.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(frContent));

        // Act
        var enMetadata = await _service.GetPageMetadataAsync("test", null, CancellationToken.None);
        var frMetadata = await _service.GetPageMetadataAsync("test", "fr", CancellationToken.None);

        // Assert
        Assert.NotNull(enMetadata);
        Assert.NotNull(frMetadata);
        Assert.Equal("English Title", enMetadata.Title);
        Assert.Equal("Titre Français", frMetadata.Title);
    }

    [Fact]
    public async Task ExtractAndCacheMetadata_PreventsUnnecessaryRepositoryAccess()
    {
        // Arrange
        var content = "# Pre-cached Title\n\nContent.";

        // Act - Set metadata before any get
        _service.ExtractAndCacheMetadata("test", null, content);
        var metadata = await _service.GetPageMetadataAsync("test", null, CancellationToken.None);

        // Assert - Should not call repository at all
        Assert.NotNull(metadata);
        Assert.Equal("Pre-cached Title", metadata.Title);
        _mockRepository.Verify(x => x.ReadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ClearCache_RemovesAllCachedMetadata()
    {
        // Arrange
        var content1 = "# Title 1\n\nContent.";
        var content2 = "# Title 2\n\nContent.";
        var contentBytes1 = Encoding.UTF8.GetBytes(content1);
        var contentBytes2 = Encoding.UTF8.GetBytes(content2);

        _mockRepository
            .Setup(x => x.ReadFileAsync("page1.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentBytes1);

        _mockRepository
            .Setup(x => x.ReadFileAsync("page2.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentBytes2);

        // Populate cache with multiple entries
        await _service.GetPageMetadataAsync("page1", null, CancellationToken.None);
        await _service.GetPageMetadataAsync("page2", null, CancellationToken.None);

        // Verify they're cached (no additional repository calls)
        await _service.GetPageMetadataAsync("page1", null, CancellationToken.None);
        await _service.GetPageMetadataAsync("page2", null, CancellationToken.None);
        _mockRepository.Verify(x => x.ReadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        // Act
        _service.ClearCache();

        // Assert - Should read from repository again after clear
        await _service.GetPageMetadataAsync("page1", null, CancellationToken.None);
        await _service.GetPageMetadataAsync("page2", null, CancellationToken.None);
        _mockRepository.Verify(x => x.ReadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Fact]
    public async Task ExtractAndCacheMetadata_WithDifferentCultures_CachesSeparately()
    {
        // Arrange
        var enContent = "# English Title\n\nContent.";
        var frContent = "# Titre Français\n\nContent.";

        // Act
        _service.ExtractAndCacheMetadata("test", null, enContent);
        _service.ExtractAndCacheMetadata("test", "fr", frContent);

        // Assert - Should retrieve different titles without repository access
        var enMetadata = await _service.GetPageMetadataAsync("test", null, CancellationToken.None);
        var frMetadata = await _service.GetPageMetadataAsync("test", "fr", CancellationToken.None);

        Assert.NotNull(enMetadata);
        Assert.NotNull(frMetadata);
        Assert.Equal("English Title", enMetadata.Title);
        Assert.Equal("Titre Français", frMetadata.Title);
        _mockRepository.Verify(x => x.ReadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
