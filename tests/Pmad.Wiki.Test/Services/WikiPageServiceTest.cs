using System.Text;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Moq;
using Pmad.Git.HttpServer;
using Pmad.Git.LocalRepositories;
using Pmad.Wiki.Services;
using Pmad.Wiki.Test.Infrastructure;

namespace Pmad.Wiki.Test.Services;

public class WikiPageServiceTest
{
    private readonly Mock<IGitRepositoryService> _mockGitRepositoryService;
    private readonly Mock<IGitRepository> _mockRepository;
    private readonly Mock<IWikiUserService> _mockWikiUserService;
    private readonly Mock<IWikiPageTitleCache> _mockTitleCache;
    private readonly WikiOptions _options;
    private readonly WikiPageService _service;

    public WikiPageServiceTest()
    {
        _mockGitRepositoryService = new Mock<IGitRepositoryService>();
        _mockRepository = new Mock<IGitRepository>();
        _mockWikiUserService = new Mock<IWikiUserService>();
        _mockTitleCache = new Mock<IWikiPageTitleCache>();

        _options = new WikiOptions
        {
            RepositoryRoot = "/test/repos",
            WikiRepositoryName = "wiki",
            BranchName = "main",
            NeutralMarkdownPageCulture = "en",
            HomePageName = "Home"
        };

        var optionsWrapper = Options.Create(_options);
        var linkGenerator = CreateMockLinkGenerator();

        _mockGitRepositoryService
            .Setup(x => x.GetRepositoryByPath(It.IsAny<string>()))
            .Returns(_mockRepository.Object);

        _service = new WikiPageService(
            _mockGitRepositoryService.Object,
            _mockWikiUserService.Object,
            _mockTitleCache.Object,
            new MarkdownRenderService(optionsWrapper, linkGenerator),
            optionsWrapper);
    }

    private static LinkGenerator CreateMockLinkGenerator()
    {
        return new TestLinkGenerator();
    }

    #region GetPageAsync Tests

    [Fact]
    public async Task GetPageAsync_WhenPageExists_ReturnsWikiPage()
    {
        // Arrange
        var content = "# Test Page\n\nThis is test content.";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var hash = GitHash.FromBytes(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 });
        var gitFile = new GitFileContentAndHash(contentBytes, hash);
        var commit = CreateMockCommit("abc123", "Test User", "test@example.com", "Initial commit");

        _mockRepository
            .Setup(x => x.ReadFileAndHashAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitFile);

        _mockRepository
            .Setup(x => x.GetFileHistoryAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(commit));

        _mockTitleCache
            .Setup(x => x.ExtractAndCacheTitle("test", null, content))
            .Returns("Test Page");

        // Act
        var result = await _service.GetPageAsync("test", null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.PageName);
        Assert.Equal(content, result.Content);
        Assert.Equal("Test Page", result.Title);
        Assert.Contains("<h1", result.HtmlContent);
        Assert.Contains("Test Page</h1>", result.HtmlContent);
        Assert.Equal(hash.Value, result.ContentHash);
        Assert.Equal("Test User", result.LastModifiedBy);
        Assert.NotNull(result.LastModified);
    }

    [Fact]
    public async Task GetPageAsync_WhenPageDoesNotExist_ReturnsNull()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.ReadFileAndHashAsync("nonexistent.md", "main", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException());

        // Act
        var result = await _service.GetPageAsync("nonexistent", null, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPageAsync_WithCulture_UsesCorrectFilePath()
    {
        // Arrange
        var content = "# Page Française\n\nContenu.";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var hash = GitHash.FromBytes(new byte[20]);
        var gitFile = new GitFileContentAndHash(contentBytes, hash);
        var commit = CreateMockCommit("abc123", "Test User", "test@example.com", "Add French version");

        _mockRepository
            .Setup(x => x.ReadFileAndHashAsync("test.fr.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitFile);

        _mockRepository
            .Setup(x => x.GetFileHistoryAsync("test.fr.md", "main", It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(commit));

        _mockTitleCache
            .Setup(x => x.ExtractAndCacheTitle("test", "fr", content))
            .Returns("Page Française");

        // Act
        var result = await _service.GetPageAsync("test", "fr", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.PageName);
        Assert.Equal("fr", result.Culture);
        Assert.Equal("Page Française", result.Title);
    }

    [Fact]
    public async Task GetPageAsync_WithNestedPath_ReturnsCorrectPage()
    {
        // Arrange
        var content = "# Admin Settings\n\nConfiguration page.";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var hash = GitHash.FromBytes(new byte[20]);
        var gitFile = new GitFileContentAndHash(contentBytes, hash);
        var commit = CreateMockCommit("abc123", "Admin", "admin@example.com", "Add settings");

        _mockRepository
            .Setup(x => x.ReadFileAndHashAsync("admin/settings.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitFile);

        _mockRepository
            .Setup(x => x.GetFileHistoryAsync("admin/settings.md", "main", It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(commit));

        _mockTitleCache
            .Setup(x => x.ExtractAndCacheTitle("admin/settings", null, content))
            .Returns("Admin Settings");

        // Act
        var result = await _service.GetPageAsync("admin/settings", null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("admin/settings", result.PageName);
        Assert.Equal("Admin Settings", result.Title);
    }

    [Fact]
    public async Task GetPageAsync_CachesTitle()
    {
        // Arrange
        var content = "# Cached Title\n\nContent.";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var hash = GitHash.FromBytes(new byte[20]);
        var gitFile = new GitFileContentAndHash(contentBytes, hash);
        var commit = CreateMockCommit("abc123", "User", "user@example.com", "Commit");

        _mockRepository
            .Setup(x => x.ReadFileAndHashAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitFile);

        _mockRepository
            .Setup(x => x.GetFileHistoryAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(commit));

        _mockTitleCache
            .Setup(x => x.ExtractAndCacheTitle("test", null, content))
            .Returns("Cached Title");

        // Act
        await _service.GetPageAsync("test", null, CancellationToken.None);

        // Assert
        _mockTitleCache.Verify(x => x.ExtractAndCacheTitle("test", null, content), Times.Once);
    }

    [Fact]
    public async Task GetPageAsync_WithNoCommitHistory_HasNullLastModified()
    {
        // Arrange
        var content = "# Test\n\nContent.";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var hash = GitHash.FromBytes(new byte[20]);
        var gitFile = new GitFileContentAndHash(contentBytes, hash);

        _mockRepository
            .Setup(x => x.ReadFileAndHashAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitFile);

        _mockRepository
            .Setup(x => x.GetFileHistoryAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable<GitCommit>());

        _mockTitleCache
            .Setup(x => x.ExtractAndCacheTitle("test", null, content))
            .Returns("Test");

        // Act
        var result = await _service.GetPageAsync("test", null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.LastModifiedBy);
        Assert.Null(result.LastModified);
    }

    #endregion

    #region GetPageHistoryAsync Tests

    [Fact]
    public async Task GetPageHistoryAsync_WhenPageHasHistory_ReturnsHistoryItems()
    {
        // Arrange
        var commit1 = CreateMockCommit("commit1", "User 1", "user1@example.com", "First commit");
        var commit2 = CreateMockCommit("commit2", "User 2", "user2@example.com", "Second commit");
        var commit3 = CreateMockCommit("commit3", "User 3", "user3@example.com", "Third commit");

        var wikiUser1 = CreateMockWikiUser("user1@example.com", "User One");
        var wikiUser2 = CreateMockWikiUser("user2@example.com", "User Two");

        _mockRepository
            .Setup(x => x.GetFileHistoryAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(commit1, commit2, commit3));

        _mockWikiUserService
            .Setup(x => x.GetWikiUserFromGitEmail("user1@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(wikiUser1);

        _mockWikiUserService
            .Setup(x => x.GetWikiUserFromGitEmail("user2@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(wikiUser2);

        _mockWikiUserService
            .Setup(x => x.GetWikiUserFromGitEmail("user3@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUser?)null);

        // Act
        var result = await _service.GetPageHistoryAsync("test", null, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        
        Assert.Equal(commit1.Id.Value, result[0].CommitId);
        Assert.Equal("User One", result[0].AuthorName);
        Assert.Equal("First commit", result[0].Message);

        Assert.Equal(commit2.Id.Value, result[1].CommitId);
        Assert.Equal("User Two", result[1].AuthorName);
        Assert.Equal("Second commit", result[1].Message);

        Assert.Equal(commit3.Id.Value, result[2].CommitId);
        Assert.Equal("User 3", result[2].AuthorName);
        Assert.Equal("Third commit", result[2].Message);
    }

    [Fact]
    public async Task GetPageHistoryAsync_WhenPageDoesNotExist_ReturnsEmptyList()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.GetFileHistoryAsync("nonexistent.md", "main", It.IsAny<CancellationToken>()))
            .Throws(new FileNotFoundException());

        // Act
        var result = await _service.GetPageHistoryAsync("nonexistent", null, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPageHistoryAsync_CachesUserLookups()
    {
        // Arrange
        var commit1 = CreateMockCommit("commit1", "User 1", "same@example.com", "First");
        var commit2 = CreateMockCommit("commit2", "User 1", "same@example.com", "Second");
        var commit3 = CreateMockCommit("commit3", "User 1", "same@example.com", "Third");

        var wikiUser = CreateMockWikiUser("same@example.com", "Cached User");

        _mockRepository
            .Setup(x => x.GetFileHistoryAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(commit1, commit2, commit3));

        _mockWikiUserService
            .Setup(x => x.GetWikiUserFromGitEmail("same@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(wikiUser);

        // Act
        var result = await _service.GetPageHistoryAsync("test", null, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, item => Assert.Equal("Cached User", item.AuthorName));
        
        // Should only call GetWikiUserFromGitEmail once due to caching
        _mockWikiUserService.Verify(
            x => x.GetWikiUserFromGitEmail("same@example.com", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPageHistoryAsync_WithCulture_UsesCorrectFilePath()
    {
        // Arrange
        var commit = CreateMockCommit("commit1", "User", "user@example.com", "Commit");

        _mockRepository
            .Setup(x => x.GetFileHistoryAsync("test.fr.md", "main", It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(commit));

        // Act
        var result = await _service.GetPageHistoryAsync("test", "fr", CancellationToken.None);

        // Assert
        Assert.Single(result);
        _mockRepository.Verify(
            x => x.GetFileHistoryAsync("test.fr.md", "main", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetPageAtRevisionAsync Tests

    [Fact]
    public async Task GetPageAtRevisionAsync_WhenRevisionExists_ReturnsWikiPage()
    {
        // Arrange
        var content = "# Old Version\n\nOld content.";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var hash = GitHash.FromBytes(new byte[20]);
        var gitFile = new GitFileContentAndHash(contentBytes, hash);
        var commit = CreateMockCommit("oldcommit", "User", "user@example.com", "Old commit");

        _mockRepository
            .Setup(x => x.ReadFileAndHashAsync("test.md", "oldcommit", It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitFile);

        _mockRepository
            .Setup(x => x.GetCommitAsync("oldcommit", It.IsAny<CancellationToken>()))
            .ReturnsAsync(commit);

        // Act
        var result = await _service.GetPageAtRevisionAsync("test", null, "oldcommit", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.PageName);
        Assert.Equal(content, result.Content);
        Assert.Contains("<h1", result.HtmlContent);
        Assert.Contains("Old Version</h1>", result.HtmlContent);
        Assert.Equal("User", result.LastModifiedBy);
    }

    [Fact]
    public async Task GetPageAtRevisionAsync_WhenRevisionDoesNotExist_ReturnsNull()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.ReadFileAndHashAsync("test.md", "badcommit", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException());

        // Act
        var result = await _service.GetPageAtRevisionAsync("test", null, "badcommit", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPageAtRevisionAsync_WithCulture_UsesCorrectFilePath()
    {
        // Arrange
        var content = "# Version Française\n\nContenu ancien.";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var hash = GitHash.FromBytes(new byte[20]);
        var gitFile = new GitFileContentAndHash(contentBytes, hash);
        var commit = CreateMockCommit("commit1", "User", "user@example.com", "French version");

        _mockRepository
            .Setup(x => x.ReadFileAndHashAsync("test.fr.md", "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitFile);

        _mockRepository
            .Setup(x => x.GetCommitAsync("commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(commit);

        // Act
        var result = await _service.GetPageAtRevisionAsync("test", "fr", "commit1", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.PageName);
        Assert.Equal("fr", result.Culture);
    }

    [Fact]
    public async Task GetPageAtRevisionAsync_DoesNotCacheTitle()
    {
        // Arrange
        var content = "# Old Title\n\nContent.";
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var hash = GitHash.FromBytes(new byte[20]);
        var gitFile = new GitFileContentAndHash(contentBytes, hash);
        var commit = CreateMockCommit("commit1", "User", "user@example.com", "Commit");

        _mockRepository
            .Setup(x => x.ReadFileAndHashAsync("test.md", "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(gitFile);

        _mockRepository
            .Setup(x => x.GetCommitAsync("commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(commit);

        // Act
        await _service.GetPageAtRevisionAsync("test", null, "commit1", CancellationToken.None);

        // Assert - Should NOT call ExtractAndCacheTitle for historical revisions
        _mockTitleCache.Verify(
            x => x.ExtractAndCacheTitle(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    #region PageExistsAsync Tests

    [Fact]
    public async Task PageExistsAsync_WhenPageExists_ReturnsTrue()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("# Test");

        _mockRepository
            .Setup(x => x.ReadFileAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        // Act
        var result = await _service.PageExistsAsync("test", null, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task PageExistsAsync_WhenPageDoesNotExist_ReturnsFalse()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.ReadFileAsync("nonexistent.md", "main", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException());

        // Act
        var result = await _service.PageExistsAsync("nonexistent", null, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task PageExistsAsync_WithCulture_ChecksCorrectFile()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("# Test");

        _mockRepository
            .Setup(x => x.ReadFileAsync("test.de.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        // Act
        var result = await _service.PageExistsAsync("test", "de", CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(
            x => x.ReadFileAsync("test.de.md", "main", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetAvailableCulturesForPageAsync Tests

    [Fact]
    public async Task GetAvailableCulturesForPageAsync_ReturnsAllCultures()
    {
        // Arrange
        var commit = CreateMockCommit("commit1", "User", "user@example.com", "Commit");
        
        var files = new[]
        {
            CreateTreeItem("test.md", GitTreeEntryKind.Blob),
            CreateTreeItem("test.fr.md", GitTreeEntryKind.Blob),
            CreateTreeItem("test.de.md", GitTreeEntryKind.Blob),
            CreateTreeItem("other.md", GitTreeEntryKind.Blob)
        };

        _mockRepository
            .Setup(x => x.GetCommitAsync("main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(commit);

        _mockRepository
            .Setup(x => x.EnumerateCommitTreeAsync("main", null, SearchOption.TopDirectoryOnly, It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(files));

        // Act
        var result = await _service.GetAvailableCulturesForPageAsync("test", CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("en", result);
        Assert.Contains("fr", result);
        Assert.Contains("de", result);
    }

    [Fact]
    public async Task GetAvailableCulturesForPageAsync_WithNestedPath_ReturnsCorrectCultures()
    {
        // Arrange
        var commit = CreateMockCommit("commit1", "User", "user@example.com", "Commit");
        
        var files = new[]
        {
            CreateTreeItem("admin/settings.md", GitTreeEntryKind.Blob),
            CreateTreeItem("admin/settings.fr.md", GitTreeEntryKind.Blob)
        };

        _mockRepository
            .Setup(x => x.GetCommitAsync("main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(commit);

        _mockRepository
            .Setup(x => x.EnumerateCommitTreeAsync("main", "admin", SearchOption.TopDirectoryOnly, It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(files));

        // Act
        var result = await _service.GetAvailableCulturesForPageAsync("admin/settings", CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("en", result);
        Assert.Contains("fr", result);
    }

    [Fact]
    public async Task GetAvailableCulturesForPageAsync_WhenDirectoryDoesNotExist_ReturnsEmptyList()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.GetCommitAsync("main", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DirectoryNotFoundException());

        // Act
        var result = await _service.GetAvailableCulturesForPageAsync("nonexistent", CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetAllPagesAsync Tests

    [Fact]
    public async Task GetAllPagesAsync_ReturnsAllPages()
    {
        // Arrange
        var commit1 = CreateMockCommit("commit1", "User1", "user1@example.com", "Add Home");
        var commit2 = CreateMockCommit("commit2", "User2", "user2@example.com", "Add About");
        var commit3 = CreateMockCommit("commit3", "User3", "user3@example.com", "Add Guide");

        var fileLastChanges = new List<GitFileLastChange>
        {
            new("Home.md", commit1),
            new("About.md", commit2),
            new("docs/Guide.md", commit3)
        };

        _mockRepository
            .Setup(x => x.ListFilesWithLastChangeAsync("main", null, It.IsAny<Func<string, bool>>(), SearchOption.AllDirectories, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileLastChanges);

        _mockTitleCache
            .Setup(x => x.GetPageTitleAsync("Home", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Home Page");

        _mockTitleCache
            .Setup(x => x.GetPageTitleAsync("About", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("About Us");

        _mockTitleCache
            .Setup(x => x.GetPageTitleAsync("docs/Guide", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("User Guide");

        // Act
        var result = await _service.GetAllPagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        
        var home = result.First(p => p.PageName == "Home");
        Assert.Equal("Home Page", home.Title);
        Assert.Equal("User1", home.LastModifiedBy);

        var about = result.First(p => p.PageName == "About");
        Assert.Equal("About Us", about.Title);
        Assert.Equal("User2", about.LastModifiedBy);

        var guide = result.First(p => p.PageName == "docs/Guide");
        Assert.Equal("User Guide", guide.Title);
        Assert.Equal("User3", guide.LastModifiedBy);
    }

    [Fact]
    public async Task GetAllPagesAsync_FiltersNonMarkdownFiles()
    {
        // Arrange
        var commit = CreateMockCommit("commit1", "User", "user@example.com", "Add page");

        // ListFilesWithLastChangeAsync already applies the fileFilter, so it returns only .md files
        var fileLastChanges = new List<GitFileLastChange>
        {
            new("page.md", commit)
        };

        _mockRepository
            .Setup(x => x.ListFilesWithLastChangeAsync("main", null, It.IsAny<Func<string, bool>>(), SearchOption.AllDirectories, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileLastChanges);

        _mockTitleCache
            .Setup(x => x.GetPageTitleAsync("page", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Page");

        // Act
        var result = await _service.GetAllPagesAsync(CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("page", result[0].PageName);
    }

    [Fact]
    public async Task GetAllPagesAsync_HandlesMultipleCultures()
    {
        // Arrange
        var commit1 = CreateMockCommit("commit1", "User", "user@example.com", "Add English");
        var commit2 = CreateMockCommit("commit2", "User", "user@example.com", "Add French");
        var commit3 = CreateMockCommit("commit3", "User", "user@example.com", "Add German");

        var fileLastChanges = new List<GitFileLastChange>
        {
            new("test.md", commit1),
            new("test.fr.md", commit2),
            new("test.de.md", commit3)
        };

        _mockRepository
            .Setup(x => x.ListFilesWithLastChangeAsync("main", null, It.IsAny<Func<string, bool>>(), SearchOption.AllDirectories, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileLastChanges);

        _mockTitleCache
            .Setup(x => x.GetPageTitleAsync("test", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test Page");

        _mockTitleCache
            .Setup(x => x.GetPageTitleAsync("test", "fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Page de Test");

        _mockTitleCache
            .Setup(x => x.GetPageTitleAsync("test", "de", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Testseite");

        // Act
        var result = await _service.GetAllPagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, p => Assert.Equal("test", p.PageName));
        Assert.Contains(result, p => p.Culture == null && p.Title == "Test Page");
        Assert.Contains(result, p => p.Culture == "fr" && p.Title == "Page de Test");
        Assert.Contains(result, p => p.Culture == "de" && p.Title == "Testseite");
    }

    [Fact]
    public async Task GetAllPagesAsync_WhenRepositoryEmpty_ReturnsEmptyList()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.ListFilesWithLastChangeAsync("main", null, It.IsAny<Func<string, bool>>(), SearchOption.AllDirectories, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Repository is empty"));

        // Act
        var result = await _service.GetAllPagesAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllPagesAsync_OrdersByPageName()
    {
        // Arrange
        var commit = CreateMockCommit("commit1", "User", "user@example.com", "Commit");

        var fileLastChanges = new List<GitFileLastChange>
        {
            new("Zebra.md", commit),
            new("Apple.md", commit),
            new("Mango.md", commit)
        };

        _mockRepository
            .Setup(x => x.ListFilesWithLastChangeAsync("main", null, It.IsAny<Func<string, bool>>(), SearchOption.AllDirectories, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileLastChanges);

        foreach (var item in fileLastChanges)
        {
            var pageName = Path.GetFileNameWithoutExtension(item.Path);
            _mockTitleCache
                .Setup(x => x.GetPageTitleAsync(pageName, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(pageName);
        }

        // Act
        var result = await _service.GetAllPagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Apple", result[0].PageName);
        Assert.Equal("Mango", result[1].PageName);
        Assert.Equal("Zebra", result[2].PageName);
    }

    #endregion

    #region SavePageWithMediaAsync Tests

    [Fact]
    public async Task SavePageWithMediaAsync_WhenPageIsNew_AddsFile()
    {
        // Arrange
        var author = CreateMockWikiUser("user@example.com", "Test User");
        var content = "# New Page\n\nNew content.";

        _mockRepository
            .Setup(x => x.GetPathTypeAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitTreeEntryKind?)null);

        _mockRepository
            .Setup(x => x.CreateCommitAsync(
                "main",
                It.IsAny<IEnumerable<GitCommitOperation>>(),
                It.IsAny<GitCommitMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GitHash.FromBytes(new byte[20]));

        _mockTitleCache
            .Setup(x => x.ExtractAndCacheTitle("test", null, content))
            .Returns("New Page");

        // Act
        await _service.SavePageWithMediaAsync("test", null, content, "Create new page", author, new(), CancellationToken.None);

        // Assert
        _mockRepository.Verify(x => x.CreateCommitAsync(
            "main",
            It.Is<IEnumerable<GitCommitOperation>>(ops => ops.Any(op => op is AddFileOperation)),
            It.Is<GitCommitMetadata>(m => 
                m.Message == "Create new page" && 
                m.AuthorName == "Test User" &&
                m.AuthorEmail == "user@example.com"),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockTitleCache.Verify(x => x.ExtractAndCacheTitle("test", null, content), Times.Once);
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WhenPageExists_UpdatesFile()
    {
        // Arrange
        var author = CreateMockWikiUser("user@example.com", "Test User");
        var content = "# Updated Page\n\nUpdated content.";

        _mockRepository
            .Setup(x => x.GetPathTypeAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GitTreeEntryKind.Blob);

        _mockRepository
            .Setup(x => x.CreateCommitAsync(
                "main",
                It.IsAny<IEnumerable<GitCommitOperation>>(),
                It.IsAny<GitCommitMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GitHash.FromBytes(new byte[20]));

        _mockTitleCache
            .Setup(x => x.ExtractAndCacheTitle("test", null, content))
            .Returns("Updated Page");

        // Act
        await _service.SavePageWithMediaAsync("test", null, content, "Update page", author, new(), CancellationToken.None);

        // Assert
        _mockRepository.Verify(x => x.CreateCommitAsync(
            "main",
            It.Is<IEnumerable<GitCommitOperation>>(ops => ops.Any(op => op is UpdateFileOperation)),
            It.Is<GitCommitMetadata>(m => m.Message == "Update page"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WhenPathIsDirectory_ThrowsInvalidOperationException()
    {
        // Arrange
        var author = CreateMockWikiUser("user@example.com", "Test User");
        var content = "# Page";

        _mockRepository
            .Setup(x => x.GetPathTypeAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GitTreeEntryKind.Tree);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SavePageWithMediaAsync("test", null, content, "Create page", author, new(), CancellationToken.None));
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithCulture_UsesCorrectFilePath()
    {
        // Arrange
        var author = CreateMockWikiUser("user@example.com", "Test User");
        var content = "# Page Française";

        _mockRepository
            .Setup(x => x.GetPathTypeAsync("test.fr.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitTreeEntryKind?)null);

        _mockRepository
            .Setup(x => x.CreateCommitAsync(
                "main",
                It.IsAny<IEnumerable<GitCommitOperation>>(),
                It.IsAny<GitCommitMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GitHash.FromBytes(new byte[20]));

        _mockTitleCache
            .Setup(x => x.ExtractAndCacheTitle("test", "fr", content))
            .Returns("Page Française");

        // Act
        await _service.SavePageWithMediaAsync("test", "fr", content, "Add French page", author, new(), CancellationToken.None);

        // Assert
        _mockRepository.Verify(x => x.GetPathTypeAsync("test.fr.md", "main", It.IsAny<CancellationToken>()), Times.Once);
        _mockTitleCache.Verify(x => x.ExtractAndCacheTitle("test", "fr", content), Times.Once);
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithNestedPath_SavesCorrectly()
    {
        // Arrange
        var author = CreateMockWikiUser("admin@example.com", "Admin");
        var content = "# Admin Settings";

        _mockRepository
            .Setup(x => x.GetPathTypeAsync("admin/settings.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitTreeEntryKind?)null);

        _mockRepository
            .Setup(x => x.CreateCommitAsync(
                "main",
                It.IsAny<IEnumerable<GitCommitOperation>>(),
                It.IsAny<GitCommitMetadata>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(GitHash.FromBytes(new byte[20]));

        _mockTitleCache
            .Setup(x => x.ExtractAndCacheTitle("admin/settings", null, content))
            .Returns("Admin Settings");

        // Act
        await _service.SavePageWithMediaAsync("admin/settings", null, content, "Add settings page", author, new (), CancellationToken.None);

        // Assert
        _mockRepository.Verify(x => x.GetPathTypeAsync("admin/settings.md", "main", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithSingleMediaFile_AddsMediaFile()
    {
        // Arrange
        var author = CreateMockWikiUser("user@example.com", "Test User");
        var content = "# Page with Image\n\n![Logo](images/logo.png)";
        var mediaFiles = new Dictionary<string, byte[]>
        {
            ["images/logo.png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 } // PNG header
        };

        _mockRepository
            .Setup(x => x.GetPathTypeAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitTreeEntryKind?)null);

        GitCommitOperation[]? capturedOps = null;
        _mockRepository
            .Setup(x => x.CreateCommitAsync(
                "main",
                It.IsAny<IEnumerable<GitCommitOperation>>(),
                It.IsAny<GitCommitMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, IEnumerable<GitCommitOperation>, GitCommitMetadata, CancellationToken>(
                (_, ops, _, _) => capturedOps = ops.ToArray())
            .ReturnsAsync(GitHash.FromBytes(new byte[20]));

        _mockTitleCache
            .Setup(x => x.ExtractAndCacheTitle("test", null, content))
            .Returns("Page with Image");

        // Act
        await _service.SavePageWithMediaAsync("test", null, content, "Add page with image", author, mediaFiles, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedOps);
        Assert.Equal(2, capturedOps.Length);
        
        var pageOp = capturedOps.OfType<AddFileOperation>().FirstOrDefault(op => op.Path == "test.md");
        Assert.NotNull(pageOp);
        
        var mediaOp = capturedOps.OfType<AddFileOperation>().FirstOrDefault(op => op.Path == "images/logo.png");
        Assert.NotNull(mediaOp);

        _mockTitleCache.Verify(x => x.ExtractAndCacheTitle("test", null, content), Times.Once);
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithMultipleMediaFiles_AddsAllMediaFiles()
    {
        // Arrange
        var author = CreateMockWikiUser("user@example.com", "Test User");
        var content = "# Gallery\n\n![Image 1](images/img1.png)\n![Image 2](images/img2.jpg)\n[Document](docs/manual.pdf)";
        var mediaFiles = new Dictionary<string, byte[]>
        {
            ["images/img1.png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            ["images/img2.jpg"] = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
            ["docs/manual.pdf"] = new byte[] { 0x25, 0x50, 0x44, 0x46 }
        };

        _mockRepository
            .Setup(x => x.GetPathTypeAsync("gallery.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitTreeEntryKind?)null);

        GitCommitOperation[]? capturedOps = null;
        _mockRepository
            .Setup(x => x.CreateCommitAsync(
                "main",
                It.IsAny<IEnumerable<GitCommitOperation>>(),
                It.IsAny<GitCommitMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, IEnumerable<GitCommitOperation>, GitCommitMetadata, CancellationToken>(
                (_, ops, _, _) => capturedOps = ops.ToArray())
            .ReturnsAsync(GitHash.FromBytes(new byte[20]));

        _mockTitleCache
            .Setup(x => x.ExtractAndCacheTitle("gallery", null, content))
            .Returns("Gallery");

        // Act
        await _service.SavePageWithMediaAsync("gallery", null, content, "Add gallery with media", author, mediaFiles, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedOps);
        Assert.Equal(4, capturedOps.Length);
        
        Assert.Contains(capturedOps.OfType<AddFileOperation>(), op => op.Path == "gallery.md");
        Assert.Contains(capturedOps.OfType<AddFileOperation>(), op => op.Path == "images/img1.png");
        Assert.Contains(capturedOps.OfType<AddFileOperation>(), op => op.Path == "images/img2.jpg");
        Assert.Contains(capturedOps.OfType<AddFileOperation>(), op => op.Path == "docs/manual.pdf");
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithExistingPageAndNewMedia_UpdatesPageAndAddsMedia()
    {
        // Arrange
        var author = CreateMockWikiUser("user@example.com", "Test User");
        var content = "# Updated Page\n\n![New Image](images/new.png)";
        var mediaFiles = new Dictionary<string, byte[]>
        {
            ["images/new.png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 }
        };

        _mockRepository
            .Setup(x => x.GetPathTypeAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GitTreeEntryKind.Blob);

        GitCommitOperation[]? capturedOps = null;
        _mockRepository
            .Setup(x => x.CreateCommitAsync(
                "main",
                It.IsAny<IEnumerable<GitCommitOperation>>(),
                It.IsAny<GitCommitMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, IEnumerable<GitCommitOperation>, GitCommitMetadata, CancellationToken>(
                (_, ops, _, _) => capturedOps = ops.ToArray())
            .ReturnsAsync(GitHash.FromBytes(new byte[20]));

        _mockTitleCache
            .Setup(x => x.ExtractAndCacheTitle("test", null, content))
            .Returns("Updated Page");

        // Act
        await _service.SavePageWithMediaAsync("test", null, content, "Update page and add media", author, mediaFiles, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedOps);
        Assert.Equal(2, capturedOps.Length);
        
        Assert.Contains(capturedOps.OfType<UpdateFileOperation>(), op => op.Path == "test.md");
        Assert.Contains(capturedOps.OfType<AddFileOperation>(), op => op.Path == "images/new.png");
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithInvalidMediaPath_ThrowsArgumentException()
    {
        // Arrange
        var author = CreateMockWikiUser("user@example.com", "Test User");
        var content = "# Test Page";
        var mediaFiles = new Dictionary<string, byte[]>
        {
            ["../../../etc/passwd"] = new byte[] { 0x01, 0x02, 0x03 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SavePageWithMediaAsync("test", null, content, "Attempt bad path", author, mediaFiles, CancellationToken.None));
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithMediaPathContainingDoubleSlash_ThrowsArgumentException()
    {
        // Arrange
        var author = CreateMockWikiUser("user@example.com", "Test User");
        var content = "# Test Page";
        var mediaFiles = new Dictionary<string, byte[]>
        {
            ["images//logo.png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SavePageWithMediaAsync("test", null, content, "Attempt double slash", author, mediaFiles, CancellationToken.None));
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithAbsoluteMediaPath_ThrowsArgumentException()
    {
        // Arrange
        var author = CreateMockWikiUser("user@example.com", "Test User");
        var content = "# Test Page";
        var mediaFiles = new Dictionary<string, byte[]>
        {
            ["/images/logo.png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SavePageWithMediaAsync("test", null, content, "Attempt absolute path", author, mediaFiles, CancellationToken.None));
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithMediaPathEndingWithSlash_ThrowsArgumentException()
    {
        // Arrange
        var author = CreateMockWikiUser("user@example.com", "Test User");
        var content = "# Test Page";
        var mediaFiles = new Dictionary<string, byte[]>
        {
            ["images/"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SavePageWithMediaAsync("test", null, content, "Attempt trailing slash", author, mediaFiles, CancellationToken.None));
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithDotFile_ThrowsArgumentException()
    {
        // Arrange
        var author = CreateMockWikiUser("user@example.com", "Test User");
        var content = "# Test Page";
        var mediaFiles = new Dictionary<string, byte[]>
        {
            ["images/.hidden.gif"] = new byte[] { 0x47, 0x49, 0x46 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SavePageWithMediaAsync("test", null, content, "Attempt hidden file", author, mediaFiles, CancellationToken.None));
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithNoExtensionMediaPath_ThrowsArgumentException()
    {
        // Arrange
        var author = CreateMockWikiUser("user@example.com", "Test User");
        var content = "# Test Page";
        var mediaFiles = new Dictionary<string, byte[]>
        {
            ["images/logo"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SavePageWithMediaAsync("test", null, content, "Attempt no extension", author, mediaFiles, CancellationToken.None));
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithEmptyMediaFiles_SavesOnlyPage()
    {
        // Arrange
        var author = CreateMockWikiUser("user@example.com", "Test User");
        var content = "# Simple Page";
        var mediaFiles = new Dictionary<string, byte[]>();

        _mockRepository
            .Setup(x => x.GetPathTypeAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitTreeEntryKind?)null);

        GitCommitOperation[]? capturedOps = null;
        _mockRepository
            .Setup(x => x.CreateCommitAsync(
                "main",
                It.IsAny<IEnumerable<GitCommitOperation>>(),
                It.IsAny<GitCommitMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, IEnumerable<GitCommitOperation>, GitCommitMetadata, CancellationToken>(
                (_, ops, _, _) => capturedOps = ops.ToArray())
            .ReturnsAsync(GitHash.FromBytes(new byte[20]));

        _mockTitleCache
            .Setup(x => x.ExtractAndCacheTitle("test", null, content))
            .Returns("Simple Page");

        // Act
        await _service.SavePageWithMediaAsync("test", null, content, "Add simple page", author, mediaFiles, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedOps);
        Assert.Single(capturedOps);
        Assert.Contains(capturedOps.OfType<AddFileOperation>(), op => op.Path == "test.md");
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithCultureAndMediaFiles_SavesBothCorrectly()
    {
        // Arrange
        var author = CreateMockWikiUser("user@example.com", "Test User");
        var content = "# Page Française\n\n![Image](images/banner.jpg)";
        var mediaFiles = new Dictionary<string, byte[]>
        {
            ["images/banner.jpg"] = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }
        };

        _mockRepository
            .Setup(x => x.GetPathTypeAsync("test.fr.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitTreeEntryKind?)null);

        GitCommitOperation[]? capturedOps = null;
        _mockRepository
            .Setup(x => x.CreateCommitAsync(
                "main",
                It.IsAny<IEnumerable<GitCommitOperation>>(),
                It.IsAny<GitCommitMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, IEnumerable<GitCommitOperation>, GitCommitMetadata, CancellationToken>(
                (_, ops, _, _) => capturedOps = ops.ToArray())
            .ReturnsAsync(GitHash.FromBytes(new byte[20]));

        _mockTitleCache
            .Setup(x => x.ExtractAndCacheTitle("test", "fr", content))
            .Returns("Page Française");

        // Act
        await _service.SavePageWithMediaAsync("test", "fr", content, "Add French page with media", author, mediaFiles, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedOps);
        Assert.Equal(2, capturedOps.Length);
        Assert.Contains(capturedOps.OfType<AddFileOperation>(), op => op.Path == "test.fr.md");
        Assert.Contains(capturedOps.OfType<AddFileOperation>(), op => op.Path == "images/banner.jpg");

        _mockTitleCache.Verify(x => x.ExtractAndCacheTitle("test", "fr", content), Times.Once);
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithNestedPathAndMediaFiles_SavesCorrectly()
    {
        // Arrange
        var author = CreateMockWikiUser("admin@example.com", "Admin");
        var content = "# Admin Guide\n\n![Screenshot](docs/admin/screenshot.png)";
        var mediaFiles = new Dictionary<string, byte[]>
        {
            ["docs/admin/screenshot.png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 }
        };

        _mockRepository
            .Setup(x => x.GetPathTypeAsync("admin/guide.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitTreeEntryKind?)null);

        GitCommitOperation[]? capturedOps = null;
        _mockRepository
            .Setup(x => x.CreateCommitAsync(
                "main",
                It.IsAny<IEnumerable<GitCommitOperation>>(),
                It.IsAny<GitCommitMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, IEnumerable<GitCommitOperation>, GitCommitMetadata, CancellationToken>(
                (_, ops, _, _) => capturedOps = ops.ToArray())
            .ReturnsAsync(GitHash.FromBytes(new byte[20]));

        _mockTitleCache
            .Setup(x => x.ExtractAndCacheTitle("admin/guide", null, content))
            .Returns("Admin Guide");

        // Act
        await _service.SavePageWithMediaAsync("admin/guide", null, content, "Add admin guide with screenshot", author, mediaFiles, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedOps);
        Assert.Equal(2, capturedOps.Length);
        Assert.Contains(capturedOps.OfType<AddFileOperation>(), op => op.Path == "admin/guide.md");
        Assert.Contains(capturedOps.OfType<AddFileOperation>(), op => op.Path == "docs/admin/screenshot.png");
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithMediaFilesOfVariousTypes_SavesAll()
    {
        // Arrange
        var author = CreateMockWikiUser("user@example.com", "Test User");
        var content = "# Rich Media Page\n\nMultiple media types.";
        var mediaFiles = new Dictionary<string, byte[]>
        {
            ["images/photo.jpg"] = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
            ["images/diagram.png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            ["videos/tutorial.mp4"] = new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70 },
            ["documents/spec.pdf"] = new byte[] { 0x25, 0x50, 0x44, 0x46 },
            ["data/info.json"] = new byte[] { 0x7B, 0x7D }
        };

        _mockRepository
            .Setup(x => x.GetPathTypeAsync("media.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitTreeEntryKind?)null);

        GitCommitOperation[]? capturedOps = null;
        _mockRepository
            .Setup(x => x.CreateCommitAsync(
                "main",
                It.IsAny<IEnumerable<GitCommitOperation>>(),
                It.IsAny<GitCommitMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, IEnumerable<GitCommitOperation>, GitCommitMetadata, CancellationToken>(
                (_, ops, _, _) => capturedOps = ops.ToArray())
            .ReturnsAsync(GitHash.FromBytes(new byte[20]));

        _mockTitleCache
            .Setup(x => x.ExtractAndCacheTitle("media", null, content))
            .Returns("Rich Media Page");

        // Act
        await _service.SavePageWithMediaAsync("media", null, content, "Add page with various media", author, mediaFiles, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedOps);
        Assert.Equal(6, capturedOps.Length);
        Assert.Contains(capturedOps.OfType<AddFileOperation>(), op => op.Path == "media.md");
        Assert.Contains(capturedOps.OfType<AddFileOperation>(), op => op.Path == "images/photo.jpg");
        Assert.Contains(capturedOps.OfType<AddFileOperation>(), op => op.Path == "images/diagram.png");
        Assert.Contains(capturedOps.OfType<AddFileOperation>(), op => op.Path == "videos/tutorial.mp4");
        Assert.Contains(capturedOps.OfType<AddFileOperation>(), op => op.Path == "documents/spec.pdf");
        Assert.Contains(capturedOps.OfType<AddFileOperation>(), op => op.Path == "data/info.json");
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithMediaContentVerification_SavesCorrectContent()
    {
        // Arrange
        var author = CreateMockWikiUser("user@example.com", "Test User");
        var content = "# Test Page";
        var imageContent = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var mediaFiles = new Dictionary<string, byte[]>
        {
            ["images/test.png"] = imageContent
        };

        _mockRepository
            .Setup(x => x.GetPathTypeAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitTreeEntryKind?)null);

        GitCommitOperation[]? capturedOps = null;
        _mockRepository
            .Setup(x => x.CreateCommitAsync(
                "main",
                It.IsAny<IEnumerable<GitCommitOperation>>(),
                It.IsAny<GitCommitMetadata>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, IEnumerable<GitCommitOperation>, GitCommitMetadata, CancellationToken>(
                (_, ops, _, _) => capturedOps = ops.ToArray())
            .ReturnsAsync(GitHash.FromBytes(new byte[20]));

        _mockTitleCache
            .Setup(x => x.ExtractAndCacheTitle("test", null, content))
            .Returns("Test Page");

        // Act
        await _service.SavePageWithMediaAsync("test", null, content, "Add page with image", author, mediaFiles, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedOps);
        var mediaOp = capturedOps.OfType<AddFileOperation>().FirstOrDefault(op => op.Path == "images/test.png");
        Assert.NotNull(mediaOp);
        Assert.True(mediaOp.Content.SequenceEqual(imageContent));
    }

    [Fact]
    public async Task SavePageWithMediaAsync_ValidatesAllMediaPathsBeforeCommit()
    {
        // Arrange
        var author = CreateMockWikiUser("user@example.com", "Test User");
        var content = "# Test Page";
        var mediaFiles = new Dictionary<string, byte[]>
        {
            ["images/valid.png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            ["../invalid.png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 }
        };

        _mockRepository
            .Setup(x => x.GetPathTypeAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitTreeEntryKind?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SavePageWithMediaAsync("test", null, content, "Attempt mixed validity", author, mediaFiles, CancellationToken.None));

        // Verify CreateCommitAsync was never called since validation failed
        _mockRepository.Verify(x => x.CreateCommitAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<GitCommitOperation>>(),
            It.IsAny<GitCommitMetadata>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetPageTitleAsync Tests

    [Fact]
    public async Task GetPageTitleAsync_DelegatesToTitleCache()
    {
        // Arrange
        _mockTitleCache
            .Setup(x => x.GetPageTitleAsync("test", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test Title");

        // Act
        var result = await _service.GetPageTitleAsync("test", null, CancellationToken.None);

        // Assert
        Assert.Equal("Test Title", result);
        _mockTitleCache.Verify(
            x => x.GetPageTitleAsync("test", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPageTitleAsync_WithCulture_DelegatesToTitleCache()
    {
        // Arrange
        _mockTitleCache
            .Setup(x => x.GetPageTitleAsync("test", "fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Titre du Test");

        // Act
        var result = await _service.GetPageTitleAsync("test", "fr", CancellationToken.None);

        // Assert
        Assert.Equal("Titre du Test", result);
        _mockTitleCache.Verify(
            x => x.GetPageTitleAsync("test", "fr", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetMediaFileAsync Tests

    [Fact]
    public async Task GetMediaFileAsync_WhenMediaExists_ReturnsFileBytes()
    {
        // Arrange
        var mediaContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header bytes
        
        _mockRepository
            .Setup(x => x.ReadFileAsync("images/logo.png", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        var result = await _service.GetMediaFileAsync("images/logo.png", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mediaContent, result);
    }

    [Fact]
    public async Task GetMediaFileAsync_WhenMediaDoesNotExist_ReturnsNull()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.ReadFileAsync("images/nonexistent.png", "main", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException());

        // Act
        var result = await _service.GetMediaFileAsync("images/nonexistent.png", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMediaFileAsync_WithNestedPath_ReturnsCorrectFile()
    {
        // Arrange
        var mediaContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG header bytes
        
        _mockRepository
            .Setup(x => x.ReadFileAsync("docs/images/screenshot.jpg", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        var result = await _service.GetMediaFileAsync("docs/images/screenshot.jpg", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mediaContent, result);
    }

    [Fact]
    public async Task GetMediaFileAsync_WithVideoFile_ReturnsFileBytes()
    {
        // Arrange
        var mediaContent = new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70 }; // MP4 header
        
        _mockRepository
            .Setup(x => x.ReadFileAsync("videos/demo.mp4", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        var result = await _service.GetMediaFileAsync("videos/demo.mp4", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mediaContent, result);
    }

    [Fact]
    public async Task GetMediaFileAsync_WithPdfFile_ReturnsFileBytes()
    {
        // Arrange
        var mediaContent = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF header "%PDF"
        
        _mockRepository
            .Setup(x => x.ReadFileAsync("documents/manual.pdf", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        var result = await _service.GetMediaFileAsync("documents/manual.pdf", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mediaContent, result);
    }

    [Fact]
    public async Task GetMediaFileAsync_WithInvalidPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetMediaFileAsync("../../../etc/passwd", CancellationToken.None));
    }

    [Fact]
    public async Task GetMediaFileAsync_WithPathContainingDoubleSlash_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetMediaFileAsync("images//logo.png", CancellationToken.None));
    }

    [Fact]
    public async Task GetMediaFileAsync_WithAbsolutePath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetMediaFileAsync("/images/logo.png", CancellationToken.None));
    }

    [Fact]
    public async Task GetMediaFileAsync_WithTrailingSlash_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetMediaFileAsync("images/", CancellationToken.None));
    }

    [Fact]
    public async Task GetMediaFileAsync_WithDotFile_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetMediaFileAsync("images/.hidden.gif", CancellationToken.None));
    }

    [Fact]
    public async Task GetMediaFileAsync_UsesConfiguredBranchName()
    {
        // Arrange
        var mediaContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        
        _mockRepository
            .Setup(x => x.ReadFileAsync("images/logo.png", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        await _service.GetMediaFileAsync("images/logo.png", CancellationToken.None);

        // Assert
        _mockRepository.Verify(
            x => x.ReadFileAsync("images/logo.png", "main", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetMediaFileAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetMediaFileAsync("", CancellationToken.None));
    }

    [Fact]
    public async Task GetMediaFileAsync_WithWhitespacePath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetMediaFileAsync("   ", CancellationToken.None));
    }

    #endregion

    #region Helper Methods

    private static GitCommit CreateMockCommit(string id, string authorName, string authorEmail, string message)
    {
        var commitId = new GitHash(new string('0', 40));
        var treeId = new GitHash(new string('1', 40));
        var headers = new Dictionary<string, string>
        {
            ["author"] = $"{authorName} <{authorEmail}> {DateTimeOffset.UtcNow.ToUnixTimeSeconds()} +0000",
            ["committer"] = $"{authorName} <{authorEmail}> {DateTimeOffset.UtcNow.ToUnixTimeSeconds()} +0000"
        };
        return new GitCommit(commitId, treeId, new List<GitHash>(), headers, message);
    }

    private static IWikiUser CreateMockWikiUser(string email, string name)
    {
        var mockUser = new Mock<IWikiUser>();
        mockUser.Setup(x => x.GitEmail).Returns(email);
        mockUser.Setup(x => x.GitName).Returns(name);
        mockUser.Setup(x => x.DisplayName).Returns(name);
        return mockUser.Object;
    }

    private static GitTreeItem CreateTreeItem(string path, GitTreeEntryKind kind)
    {
        var mode = kind == GitTreeEntryKind.Tree ? 040000 : 0100644;
        var entry = new GitTreeEntry(Path.GetFileName(path), kind, GitHash.FromBytes(new byte[20]), mode);
        return new GitTreeItem(path, entry);
    }

    private static async IAsyncEnumerable<T> AsyncEnumerable<T>(params T[] items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }

    #endregion
}
