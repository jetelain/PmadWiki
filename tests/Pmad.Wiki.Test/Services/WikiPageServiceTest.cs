using System.Text;
using Microsoft.Extensions.Options;
using Moq;
using Pmad.Git.HttpServer;
using Pmad.Git.LocalRepositories;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Services;

public class WikiPageServiceTest
{
    private readonly Mock<IGitRepositoryService> _mockGitRepositoryService;
    private readonly Mock<IGitRepository> _mockRepository;
    private readonly Mock<IWikiUserService> _mockWikiUserService;
    private readonly Mock<IPageAccessControlService> _mockPageAccessControlService;
    private readonly Mock<IWikiPageTitleCache> _mockTitleCache;
    private readonly WikiOptions _options;
    private readonly WikiPageService _service;

    public WikiPageServiceTest()
    {
        _mockGitRepositoryService = new Mock<IGitRepositoryService>();
        _mockRepository = new Mock<IGitRepository>();
        _mockWikiUserService = new Mock<IWikiUserService>();
        _mockPageAccessControlService = new Mock<IPageAccessControlService>();
        _mockTitleCache = new Mock<IWikiPageTitleCache>();

        _options = new WikiOptions
        {
            RepositoryRoot = "/test/repos",
            WikiRepositoryName = "wiki",
            BranchName = "main",
            NeutralMarkdownPageCulture = "en",
            HomePageName = "Home",
            BasePath = "wiki"
        };

        var optionsWrapper = Options.Create(_options);

        _mockGitRepositoryService
            .Setup(x => x.GetRepository(It.IsAny<string>()))
            .Returns(_mockRepository.Object);

        _service = new WikiPageService(
            _mockGitRepositoryService.Object,
            _mockWikiUserService.Object,
            _mockPageAccessControlService.Object,
            _mockTitleCache.Object,
            optionsWrapper);
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
            .Setup(x => x.EnumerateCommitTreeAsync("main", null, It.IsAny<CancellationToken>()))
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
            .Setup(x => x.EnumerateCommitTreeAsync("main", "admin", It.IsAny<CancellationToken>()))
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
        var files = new[]
        {
            CreateTreeItem("Home.md", GitTreeEntryKind.Blob),
            CreateTreeItem("About.md", GitTreeEntryKind.Blob),
            CreateTreeItem("docs/Guide.md", GitTreeEntryKind.Blob)
        };

        var commit1 = CreateMockCommit("commit1", "User1", "user1@example.com", "Add Home");
        var commit2 = CreateMockCommit("commit2", "User2", "user2@example.com", "Add About");
        var commit3 = CreateMockCommit("commit3", "User3", "user3@example.com", "Add Guide");

        _mockRepository
            .Setup(x => x.EnumerateCommitTreeAsync("main", null, It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(files));

        _mockRepository
            .Setup(x => x.GetFileHistoryAsync("Home.md", "main", It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(commit1));

        _mockRepository
            .Setup(x => x.GetFileHistoryAsync("About.md", "main", It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(commit2));

        _mockRepository
            .Setup(x => x.GetFileHistoryAsync("docs/Guide.md", "main", It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(commit3));

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
        var files = new[]
        {
            CreateTreeItem("page.md", GitTreeEntryKind.Blob),
            CreateTreeItem("image.png", GitTreeEntryKind.Blob),
            CreateTreeItem("data.json", GitTreeEntryKind.Blob),
            CreateTreeItem("directory", GitTreeEntryKind.Tree)
        };

        var commit = CreateMockCommit("commit1", "User", "user@example.com", "Add page");

        _mockRepository
            .Setup(x => x.EnumerateCommitTreeAsync("main", null, It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(files));

        _mockRepository
            .Setup(x => x.GetFileHistoryAsync("page.md", "main", It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(commit));

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
        var files = new[]
        {
            CreateTreeItem("test.md", GitTreeEntryKind.Blob),
            CreateTreeItem("test.fr.md", GitTreeEntryKind.Blob),
            CreateTreeItem("test.de.md", GitTreeEntryKind.Blob)
        };

        var commit1 = CreateMockCommit("commit1", "User", "user@example.com", "Add English");
        var commit2 = CreateMockCommit("commit2", "User", "user@example.com", "Add French");
        var commit3 = CreateMockCommit("commit3", "User", "user@example.com", "Add German");

        _mockRepository
            .Setup(x => x.EnumerateCommitTreeAsync("main", null, It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(files));

        _mockRepository
            .Setup(x => x.GetFileHistoryAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(commit1));

        _mockRepository
            .Setup(x => x.GetFileHistoryAsync("test.fr.md", "main", It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(commit2));

        _mockRepository
            .Setup(x => x.GetFileHistoryAsync("test.de.md", "main", It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(commit3));

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
            .Setup(x => x.EnumerateCommitTreeAsync("main", null, It.IsAny<CancellationToken>()))
            .Throws(new Exception("Repository is empty"));

        // Act
        var result = await _service.GetAllPagesAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllPagesAsync_OrdersByPageName()
    {
        // Arrange
        var files = new[]
        {
            CreateTreeItem("Zebra.md", GitTreeEntryKind.Blob),
            CreateTreeItem("Apple.md", GitTreeEntryKind.Blob),
            CreateTreeItem("Mango.md", GitTreeEntryKind.Blob)
        };

        var commit = CreateMockCommit("commit1", "User", "user@example.com", "Commit");

        _mockRepository
            .Setup(x => x.EnumerateCommitTreeAsync("main", null, It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(files));

        foreach (var file in files)
        {
            _mockRepository
                .Setup(x => x.GetFileHistoryAsync(file.Path, "main", It.IsAny<CancellationToken>()))
                .Returns(AsyncEnumerable(commit));

            var pageName = Path.GetFileNameWithoutExtension(file.Path);
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

    #region SavePageAsync Tests

    [Fact]
    public async Task SavePageAsync_WhenPageIsNew_AddsFile()
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
        await _service.SavePageAsync("test", null, content, "Create new page", author, CancellationToken.None);

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
    public async Task SavePageAsync_WhenPageExists_UpdatesFile()
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
        await _service.SavePageAsync("test", null, content, "Update page", author, CancellationToken.None);

        // Assert
        _mockRepository.Verify(x => x.CreateCommitAsync(
            "main",
            It.Is<IEnumerable<GitCommitOperation>>(ops => ops.Any(op => op is UpdateFileOperation)),
            It.Is<GitCommitMetadata>(m => m.Message == "Update page"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SavePageAsync_WhenPathIsDirectory_ThrowsInvalidOperationException()
    {
        // Arrange
        var author = CreateMockWikiUser("user@example.com", "Test User");
        var content = "# Page";

        _mockRepository
            .Setup(x => x.GetPathTypeAsync("test.md", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GitTreeEntryKind.Tree);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SavePageAsync("test", null, content, "Create page", author, CancellationToken.None));
    }

    [Fact]
    public async Task SavePageAsync_WithCulture_UsesCorrectFilePath()
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
        await _service.SavePageAsync("test", "fr", content, "Add French page", author, CancellationToken.None);

        // Assert
        _mockRepository.Verify(x => x.GetPathTypeAsync("test.fr.md", "main", It.IsAny<CancellationToken>()), Times.Once);
        _mockTitleCache.Verify(x => x.ExtractAndCacheTitle("test", "fr", content), Times.Once);
    }

    [Fact]
    public async Task SavePageAsync_WithNestedPath_SavesCorrectly()
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
        await _service.SavePageAsync("admin/settings", null, content, "Add settings page", author, CancellationToken.None);

        // Assert
        _mockRepository.Verify(x => x.GetPathTypeAsync("admin/settings.md", "main", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CheckPageAccessAsync Tests

    [Fact]
    public async Task CheckPageAccessAsync_DelegatesToAccessControlService()
    {
        // Arrange
        var userGroups = new[] { "admin", "users" };
        var expectedPermissions = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = true,
            MatchedPattern = "admin/**"
        };

        _mockPageAccessControlService
            .Setup(x => x.CheckPageAccessAsync("admin/page", userGroups, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPermissions);

        // Act
        var result = await _service.CheckPageAccessAsync("admin/page", userGroups, CancellationToken.None);

        // Assert
        Assert.Equal(expectedPermissions.CanRead, result.CanRead);
        Assert.Equal(expectedPermissions.CanEdit, result.CanEdit);
        Assert.Equal(expectedPermissions.MatchedPattern, result.MatchedPattern);

        _mockPageAccessControlService.Verify(
            x => x.CheckPageAccessAsync("admin/page", userGroups, It.IsAny<CancellationToken>()),
            Times.Once);
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
