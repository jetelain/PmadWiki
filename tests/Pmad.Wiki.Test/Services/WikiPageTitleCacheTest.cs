using System.Text;
using Microsoft.Extensions.Options;
using Moq;
using Pmad.Git.HttpServer;
using Pmad.Git.LocalRepositories;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Services;

public class WikiPageTitleCacheTest
{
    private readonly Mock<IGitRepositoryService> _mockGitRepositoryService;
    private readonly Mock<IGitRepository> _mockRepository;
    private readonly WikiOptions _options;
    private readonly WikiPageTitleCache _service;

    public WikiPageTitleCacheTest()
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

        _service = new WikiPageTitleCache(
            _mockGitRepositoryService.Object,
            optionsWrapper);
    }

    [Fact]
    public async Task GetPageTitleAsync_WhenNotCached_ReadsFromRepository()
    {
        // Arrange
        var content = "# My Page Title\n\nSome content.";
        var contentBytes = Encoding.UTF8.GetBytes(content);

        _mockRepository
            .Setup(x => x.ReadFileAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentBytes);

        // Act
        var title = await _service.GetPageTitleAsync("test", null, CancellationToken.None);

        // Assert
        Assert.Equal("My Page Title", title);
        _mockRepository.Verify(x => x.ReadFileAsync("test.md", "main", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPageTitleAsync_WhenCached_DoesNotReadFromRepository()
    {
        // Arrange
        var content = "# Cached Title\n\nContent.";
        var contentBytes = Encoding.UTF8.GetBytes(content);

        _mockRepository
            .Setup(x => x.ReadFileAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentBytes);

        // First call to populate cache
        await _service.GetPageTitleAsync("test", null, CancellationToken.None);

        // Act - Second call should use cache
        var title = await _service.GetPageTitleAsync("test", null, CancellationToken.None);

        // Assert
        Assert.Equal("Cached Title", title);
        _mockRepository.Verify(x => x.ReadFileAsync("test.md", "main", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPageTitleAsync_WithNoH1_ReturnsFallback()
    {
        // Arrange
        var content = "Just some text without a title.";
        var contentBytes = Encoding.UTF8.GetBytes(content);

        _mockRepository
            .Setup(x => x.ReadFileAsync("fallback.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentBytes);

        // Act
        var title = await _service.GetPageTitleAsync("fallback", null, CancellationToken.None);

        // Assert
        Assert.Equal("fallback", title);
    }

    [Fact]
    public async Task GetPageTitleAsync_WithCulture_UsesCultureSpecificFile()
    {
        // Arrange
        var content = "# Titre en Français\n\nContenu.";
        var contentBytes = Encoding.UTF8.GetBytes(content);

        _mockRepository
            .Setup(x => x.ReadFileAsync("test.fr.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentBytes);

        // Act
        var title = await _service.GetPageTitleAsync("test", "fr", CancellationToken.None);

        // Assert
        Assert.Equal("Titre en Français", title);
        _mockRepository.Verify(x => x.ReadFileAsync("test.fr.md", "main", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPageTitleAsync_WithNestedPage_ExtractsTitle()
    {
        // Arrange
        var content = "# Admin Settings\n\nConfiguration page.";
        var contentBytes = Encoding.UTF8.GetBytes(content);

        _mockRepository
            .Setup(x => x.ReadFileAsync("admin/settings.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentBytes);

        // Act
        var title = await _service.GetPageTitleAsync("admin/settings", null, CancellationToken.None);

        // Assert
        Assert.Equal("Admin Settings", title);
    }

    [Fact]
    public async Task GetPageTitleAsync_WithNestedPageNoH1_ReturnsLastPart()
    {
        // Arrange
        var content = "Just content without title.";
        var contentBytes = Encoding.UTF8.GetBytes(content);

        _mockRepository
            .Setup(x => x.ReadFileAsync("admin/settings.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentBytes);

        // Act
        var title = await _service.GetPageTitleAsync("admin/settings", null, CancellationToken.None);

        // Assert
        Assert.Equal("settings", title);
    }

    [Fact]
    public async Task GetPageTitleAsync_WhenPageDoesNotExist_ReturnsNull()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.ReadFileAsync("nonexistent.md", "main", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException());

        // Act
        var title = await _service.GetPageTitleAsync("nonexistent", null, CancellationToken.None);

        // Assert
        Assert.Null(title);
    }

    [Fact]
    public async Task ExtractAndCacheTitle_OverwritesExistingCache()
    {
        // Arrange
        var oldContent = "# Old Title\n\nContent.";
        var newContent = "# New Title\n\nContent.";
        var contentBytes = Encoding.UTF8.GetBytes(oldContent);

        _mockRepository
            .Setup(x => x.ReadFileAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentBytes);

        // First populate cache with old title
        await _service.GetPageTitleAsync("test", null, CancellationToken.None);

        // Act - Set new title
        _service.ExtractAndCacheTitle("test", null, newContent);

        // Assert - Should return new title without calling repository
        var title = await _service.GetPageTitleAsync("test", null, CancellationToken.None);
        Assert.Equal("New Title", title);
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
        var enTitle = await _service.GetPageTitleAsync("test", null, CancellationToken.None);
        var frTitle = await _service.GetPageTitleAsync("test", "fr", CancellationToken.None);

        // Assert
        Assert.Equal("English Title", enTitle);
        Assert.Equal("Titre Français", frTitle);
    }

    [Fact]
    public async Task ExtractAndCacheTitle_PreventsUnnecessaryRepositoryAccess()
    {
        // Arrange
        var content = "# Pre-cached Title\n\nContent.";

        // Act - Set title before any get
        _service.ExtractAndCacheTitle("test", null, content);
        var title = await _service.GetPageTitleAsync("test", null, CancellationToken.None);

        // Assert - Should not call repository at all
        Assert.Equal("Pre-cached Title", title);
        _mockRepository.Verify(x => x.ReadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ClearCache_RemovesAllCachedTitles()
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

        // Populate cache with multiple titles
        await _service.GetPageTitleAsync("page1", null, CancellationToken.None);
        await _service.GetPageTitleAsync("page2", null, CancellationToken.None);

        // Verify they're cached (no additional repository calls)
        await _service.GetPageTitleAsync("page1", null, CancellationToken.None);
        await _service.GetPageTitleAsync("page2", null, CancellationToken.None);
        _mockRepository.Verify(x => x.ReadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        // Act
        _service.ClearCache();

        // Assert - Should read from repository again after clear
        await _service.GetPageTitleAsync("page1", null, CancellationToken.None);
        await _service.GetPageTitleAsync("page2", null, CancellationToken.None);
        _mockRepository.Verify(x => x.ReadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Fact]
    public async Task ExtractAndCacheTitle_WithDifferentCultures_CachesSeparately()
    {
        // Arrange
        var enContent = "# English Title\n\nContent.";
        var frContent = "# Titre Français\n\nContent.";

        // Act
        _service.ExtractAndCacheTitle("test", null, enContent);
        _service.ExtractAndCacheTitle("test", "fr", frContent);

        // Assert - Should retrieve different titles without repository access
        var enTitle = await _service.GetPageTitleAsync("test", null, CancellationToken.None);
        var frTitle = await _service.GetPageTitleAsync("test", "fr", CancellationToken.None);

        Assert.Equal("English Title", enTitle);
        Assert.Equal("Titre Français", frTitle);
        _mockRepository.Verify(x => x.ReadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
