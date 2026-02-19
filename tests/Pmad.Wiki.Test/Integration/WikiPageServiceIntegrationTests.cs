using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Pmad.Git.HttpServer;
using Pmad.Git.LocalRepositories;
using Pmad.Wiki.Helpers;
using Pmad.Wiki.Services;
using Pmad.Wiki.Test.Infrastructure;

namespace Pmad.Wiki.Test.Integration;

/// <summary>
/// Integration tests for WikiPageService that use real Git repositories.
/// These tests verify the full stack including PmadGit and interaction with git CLI.
/// </summary>
public class WikiPageServiceIntegrationTests : IDisposable
{
    private readonly string _testRepoRoot;
    private readonly string _testRepoPath;
    private readonly Mock<IWikiUserService> _mockWikiUserService;
    private readonly WikiOptions _options;
    private readonly WikiPageService _service;
    private readonly string _branchName = "main";
    private readonly ServiceProvider _serviceProvider;

    public WikiPageServiceIntegrationTests()
    {
        // Create a unique temporary directory for this test run
        _testRepoRoot = Path.Combine(Path.GetTempPath(), "PmadWikiIntegrationTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRepoRoot);

        _testRepoPath = Path.Combine(_testRepoRoot, "wiki");

        _options = new WikiOptions
        {
            RepositoryRoot = _testRepoRoot,
            WikiRepositoryName = "wiki",
            BranchName = _branchName,
            NeutralMarkdownPageCulture = "en",
            HomePageName = "Home"
        };

        _mockWikiUserService = new Mock<IWikiUserService>();

        var services = new ServiceCollection();
        services.AddGitRepositoryService();
        services.AddMemoryCache();
        _serviceProvider = services.BuildServiceProvider();

        var optionsWrapper = Options.Create(_options);
        var linkGenerator = new TestLinkGenerator();
        var gitRepositoryService = _serviceProvider.GetRequiredService<IGitRepositoryService>();

        _service = new WikiPageService(
            gitRepositoryService,
            _mockWikiUserService.Object,
            new WikiPageTitleCache(gitRepositoryService, optionsWrapper),
            new MarkdownRenderService(optionsWrapper, linkGenerator),
            optionsWrapper);
    }

    public void Dispose()
    {
        // Cleanup service provider
        _serviceProvider?.Dispose();

        // Cleanup test repositories
        if (Directory.Exists(_testRepoRoot))
        {
            try
            {
                Directory.Delete(_testRepoRoot, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    #region Helper Methods

    private void InitializeGitRepository()
    {
        Directory.CreateDirectory(_testRepoPath);

        // Use git CLI to initialize repository
        RunGitCommand(_testRepoPath, "init", $"--initial-branch={_branchName}");
        RunGitCommand(_testRepoPath, "config", "user.name", "Test User");
        RunGitCommand(_testRepoPath, "config", "user.email", "test@example.com");
    }

    private void CommitFile(string relativePath, string content, string commitMessage)
    {
        var filePath = Path.Combine(_testRepoPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Use UTF-8 without BOM to match what WikiPageService writes
        File.WriteAllText(filePath, content, new UTF8Encoding(false));
        RunGitCommand(_testRepoPath, "add", relativePath.Replace('/', Path.DirectorySeparatorChar));
        RunGitCommand(_testRepoPath, "commit", "-m", commitMessage);
    }

    private void CommitBinaryFile(string relativePath, byte[] content, string commitMessage)
    {
        var filePath = Path.Combine(_testRepoPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(filePath, content);
        RunGitCommand(_testRepoPath, "add", relativePath.Replace('/', Path.DirectorySeparatorChar));
        RunGitCommand(_testRepoPath, "commit", "-m", commitMessage);
    }

    private string GetGitLog(string filePath)
    {
        return RunGitCommand(_testRepoPath, "log", "--oneline", "--", filePath);
    }

    private string GetGitFileContent(string filePath, string? revision = null)
    {
        var refSpec = revision != null ? $"{revision}:{filePath}" : $"HEAD:{filePath}";
        return RunGitCommand(_testRepoPath, "show", refSpec);
    }

    private byte[] GetGitBinaryFileContent(string filePath, string? revision = null)
    {
        var refSpec = revision != null ? $"{revision}:{filePath}" : $"HEAD:{filePath}";
        // Use cat-file to get raw binary content
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = _testRepoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("cat-file");
        startInfo.ArgumentList.Add("blob");
        startInfo.ArgumentList.Add(refSpec);

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start git process");
        }

        using var ms = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(ms);
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Git command failed: {error}");
        }

        return ms.ToArray();
    }

    private string GetLatestCommitHash()
    {
        return RunGitCommand(_testRepoPath, "rev-parse", "HEAD").Trim();
    }

    private string GetCommitMessage(string commitHash)
    {
        return RunGitCommand(_testRepoPath, "log", "-1", "--format=%B", commitHash).Trim();
    }

    private static string RunGitCommand(string workingDirectory, params string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start git process");
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Git command failed: {error}");
        }

        return output;
    }

    private static IWikiUser CreateTestUser(string name = "Test User", string email = "test@example.com")
    {
        var mockUser = new Mock<IWikiUser>();
        mockUser.Setup(x => x.GitName).Returns(name);
        mockUser.Setup(x => x.GitEmail).Returns(email);
        mockUser.Setup(x => x.DisplayName).Returns(name);
        return mockUser.Object;
    }

    #endregion

    #region Read Tests with Git CLI Initialization

    [Fact]
    public async Task GetPageAsync_WithGitInitializedPage_ReturnsCorrectPage()
    {
        // Arrange
        InitializeGitRepository();
        var content = "# Test Page\n\nThis is a test page created with git CLI.";
        CommitFile("test.md", content, "Add test page");

        // Act
        var result = await _service.GetPageAsync("test", null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.PageName);
        Assert.Equal(content, result.Content);
        Assert.Contains("<h1", result.HtmlContent);
        Assert.Contains("Test Page</h1>", result.HtmlContent);
        Assert.NotNull(result.ContentHash);
        Assert.Equal("Test User", result.LastModifiedBy);
        Assert.NotNull(result.LastModified);
    }

    [Fact]
    public async Task GetPageAsync_WithMultipleCommits_ReturnsLatestVersion()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile("test.md", "# Version 1\n\nFirst version.", "Add v1");
        CommitFile("test.md", "# Version 2\n\nSecond version.", "Update to v2");
        CommitFile("test.md", "# Version 3\n\nThird version.", "Update to v3");

        // Act
        var result = await _service.GetPageAsync("test", null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Version 3", result.Content);
        Assert.Contains("Third version", result.Content);
    }

    [Fact]
    public async Task GetPageHistoryAsync_WithGitHistory_ReturnsAllCommits()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile("page.md", "# Page\n\nVersion 1.", "First commit");
        CommitFile("page.md", "# Page\n\nVersion 2.", "Second commit");
        CommitFile("page.md", "# Page\n\nVersion 3.", "Third commit");

        // Act
        var history = await _service.GetPageHistoryAsync("page", null, CancellationToken.None);

        // Assert
        Assert.Equal(3, history.Count);
        // Commit messages from git log format include trailing newlines
        Assert.StartsWith("Third commit", history[0].Message);
        Assert.StartsWith("Second commit", history[1].Message);
        Assert.StartsWith("First commit", history[2].Message);
    }

    [Fact]
    public async Task GetPageAtRevisionAsync_WithGitCommit_ReturnsCorrectRevision()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile("test.md", "# Old Version\n\nOld content.", "Add old version");
        var oldCommit = GetLatestCommitHash();
        CommitFile("test.md", "# New Version\n\nNew content.", "Update to new version");

        // Act
        var result = await _service.GetPageAtRevisionAsync("test", null, oldCommit, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Old Version", result.Content);
        Assert.Contains("Old content", result.Content);
    }

    [Fact]
    public async Task GetPageAsync_WithNestedPages_RetrievesCorrectly()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile("docs/guide.md", "# User Guide\n\nDocumentation.", "Add guide");
        CommitFile("docs/api/reference.md", "# API Reference\n\nAPI docs.", "Add API reference");

        // Act
        var guide = await _service.GetPageAsync("docs/guide", null, CancellationToken.None);
        var apiRef = await _service.GetPageAsync("docs/api/reference", null, CancellationToken.None);

        // Assert
        Assert.NotNull(guide);
        Assert.Equal("docs/guide", guide.PageName);
        Assert.Contains("User Guide", guide.Content);

        Assert.NotNull(apiRef);
        Assert.Equal("docs/api/reference", apiRef.PageName);
        Assert.Contains("API Reference", apiRef.Content);
    }

    [Fact]
    public async Task GetPageAsync_WithLocalizedPages_RetrievesCorrectCulture()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile("home.md", "# Home\n\nEnglish version.", "Add English home");
        CommitFile("home.fr.md", "# Accueil\n\nVersion française.", "Add French home");
        CommitFile("home.de.md", "# Startseite\n\nDeutsche Version.", "Add German home");

        // Act
        var english = await _service.GetPageAsync("home", null, CancellationToken.None);
        var french = await _service.GetPageAsync("home", "fr", CancellationToken.None);
        var german = await _service.GetPageAsync("home", "de", CancellationToken.None);

        // Assert
        Assert.NotNull(english);
        Assert.Contains("English version", english.Content);

        Assert.NotNull(french);
        Assert.Contains("Version française", french.Content);

        Assert.NotNull(german);
        Assert.Contains("Deutsche Version", german.Content);
    }

    [Fact]
    public async Task GetAvailableCulturesForPageAsync_WithMultipleCultures_ReturnsAll()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile("test.md", "# Test", "Add English");
        CommitFile("test.fr.md", "# Test FR", "Add French");
        CommitFile("test.de.md", "# Test DE", "Add German");
        CommitFile("test.es.md", "# Test ES", "Add Spanish");

        // Act
        var cultures = await _service.GetAvailableCulturesForPageAsync("test", CancellationToken.None);

        // Assert
        Assert.Equal(4, cultures.Count);
        Assert.Contains("en", cultures);
        Assert.Contains("fr", cultures);
        Assert.Contains("de", cultures);
        Assert.Contains("es", cultures);
    }

    [Fact]
    public async Task GetAllPagesAsync_WithMultiplePages_ReturnsAllPages()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile("Home.md", "# Home", "Add home");
        CommitFile("About.md", "# About", "Add about");
        CommitFile("docs/Guide.md", "# Guide", "Add guide");
        CommitFile("docs/api/Reference.md", "# Reference", "Add reference");

        // Act
        var pages = await _service.GetAllPagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(4, pages.Count);
        Assert.Contains(pages, p => p.PageName == "Home");
        Assert.Contains(pages, p => p.PageName == "About");
        Assert.Contains(pages, p => p.PageName == "docs/Guide");
        Assert.Contains(pages, p => p.PageName == "docs/api/Reference");
    }

    [Fact]
    public async Task GetMediaFileAsync_WithGitCommittedMedia_ReturnsCorrectBytes()
    {
        // Arrange
        InitializeGitRepository();
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG header
        CommitBinaryFile("images/logo.png", imageBytes, "Add logo");

        // Act
        var result = await _service.GetMediaFileAsync("images/logo.png", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(imageBytes, result);
    }

    #endregion

    #region Write Tests with Git CLI Verification

    [Fact]
    public async Task SavePageWithMediaAsync_CreatesNewPage_VerifiableWithGit()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");
        var author = CreateTestUser("Alice", "alice@example.com");
        var content = "# New Page\n\nCreated via service.";

        // Act
        await _service.SavePageWithMediaAsync("newpage", null, content, "Create new page", author, new(), CancellationToken.None);

        // Assert - Verify with git CLI
        var gitContent = GetGitFileContent("newpage.md");
        Assert.Equal(content, gitContent);

        var commitMessage = GetCommitMessage(GetLatestCommitHash());
        Assert.Equal("Create new page", commitMessage);

        var log = GetGitLog("newpage.md");
        Assert.Contains("Create new page", log);
    }

    [Fact]
    public async Task SavePageWithMediaAsync_UpdatesExistingPage_VerifiableWithGit()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile("page.md", "# Original\n\nOriginal content.", "Initial commit");
        var author = CreateTestUser("Bob", "bob@example.com");
        var updatedContent = "# Updated\n\nUpdated content.";

        // Act
        await _service.SavePageWithMediaAsync("page", null, updatedContent, "Update page", author, new(), CancellationToken.None);

        // Assert - Verify with git CLI
        var gitContent = GetGitFileContent("page.md");
        Assert.Equal(updatedContent, gitContent);

        var history = GetGitLog("page.md");
        Assert.Contains("Update page", history);
        Assert.Contains("Initial commit", history);
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithMediaFiles_CommitsAllFiles()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");
        var author = CreateTestUser();
        var content = "# Page with Media\n\n![Image](images/pic.png)";
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var mediaFiles = new Dictionary<string, byte[]>
        {
            ["images/pic.png"] = imageBytes
        };

        // Act
        await _service.SavePageWithMediaAsync("media", null, content, "Add page with media", author, mediaFiles, CancellationToken.None);

        // Assert - Verify page
        var pageContent = GetGitFileContent("media.md");
        Assert.Equal(content, pageContent);

        // Verify media file using binary read
        var gitImageBytes = GetGitBinaryFileContent("images/pic.png");
        Assert.Equal(imageBytes, gitImageBytes);
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithMultipleEdits_CreatesCorrectHistory()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");
        var author = CreateTestUser();

        // Act - Make multiple edits
        await _service.SavePageWithMediaAsync("test", null, "# Version 1", "Create v1", author, new(), CancellationToken.None);
        await _service.SavePageWithMediaAsync("test", null, "# Version 2", "Update to v2", author, new(), CancellationToken.None);
        await _service.SavePageWithMediaAsync("test", null, "# Version 3", "Update to v3", author, new(), CancellationToken.None);

        // Assert - Verify with git CLI
        var log = GetGitLog("test.md");
        Assert.Contains("Update to v3", log);
        Assert.Contains("Update to v2", log);
        Assert.Contains("Create v1", log);

        // Verify we can read the history through the service
        var history = await _service.GetPageHistoryAsync("test", null, CancellationToken.None);
        Assert.Equal(3, history.Count);
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithNestedPath_CreatesDirectoryStructure()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");
        var author = CreateTestUser();
        var content = "# Deep Page\n\nNested content.";

        // Act
        await _service.SavePageWithMediaAsync("level1/level2/level3/page", null, content, "Create nested page", author, new(), CancellationToken.None);

        // Assert
        var gitContent = GetGitFileContent("level1/level2/level3/page.md");
        Assert.Equal(content, gitContent);

        // Verify page can be read back
        var page = await _service.GetPageAsync("level1/level2/level3/page", null, CancellationToken.None);
        Assert.NotNull(page);
        Assert.Equal(content, page.Content);
    }

    [Fact]
    public async Task SavePageWithMediaAsync_WithCulture_CreatesLocalizedFile()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");
        var author = CreateTestUser();
        var frenchContent = "# Page Française\n\nContenu français.";

        // Act
        await _service.SavePageWithMediaAsync("test", "fr", frenchContent, "Add French version", author, new(), CancellationToken.None);

        // Assert
        var gitContent = GetGitFileContent("test.fr.md");
        Assert.Equal(frenchContent, gitContent);
    }

    #endregion

    #region Large/Complex Repository Tests

    [Fact]
    public async Task LargeRepository_With100Pages_CanReadAll()
    {
        // Arrange
        InitializeGitRepository();

        // Create 100 pages with git CLI
        for (int i = 0; i < 100; i++)
        {
            var pageName = $"page{i:D3}";
            var content = $"# Page {i}\n\nContent for page {i}.";
            CommitFile($"{pageName}.md", content, $"Add {pageName}");
        }

        // Act
        var allPages = await _service.GetAllPagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(100, allPages.Count);

        // Verify we can read a few random pages
        var page0 = await _service.GetPageAsync("page000", null, CancellationToken.None);
        Assert.NotNull(page0);
        Assert.Contains("Content for page 0", page0.Content);

        var page50 = await _service.GetPageAsync("page050", null, CancellationToken.None);
        Assert.NotNull(page50);
        Assert.Contains("Content for page 50", page50.Content);

        var page99 = await _service.GetPageAsync("page099", null, CancellationToken.None);
        Assert.NotNull(page99);
        Assert.Contains("Content for page 99", page99.Content);
    }

    [Fact]
    public async Task ComplexRepository_WithNestedStructure_CanNavigate()
    {
        // Arrange
        InitializeGitRepository();

        // Create a complex nested structure
        var structure = new[]
        {
            "Home.md",
            "About.md",
            "docs/Overview.md",
            "docs/Installation.md",
            "docs/guides/QuickStart.md",
            "docs/guides/Advanced.md",
            "docs/api/Authentication.md",
            "docs/api/Users.md",
            "docs/api/v1/Endpoints.md",
            "docs/api/v2/Endpoints.md",
            "admin/Dashboard.md",
            "admin/Settings.md",
            "admin/users/Management.md"
        };

        foreach (var path in structure)
        {
            var pageName = Path.GetFileNameWithoutExtension(path);
            var content = $"# {pageName}\n\nContent for {path}.";
            CommitFile(path, content, $"Add {path}");
        }

        // Act
        var allPages = await _service.GetAllPagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(structure.Length, allPages.Count);

        // Verify we can read pages at different depths
        var home = await _service.GetPageAsync("Home", null, CancellationToken.None);
        Assert.NotNull(home);

        var quickStart = await _service.GetPageAsync("docs/guides/QuickStart", null, CancellationToken.None);
        Assert.NotNull(quickStart);
        Assert.Contains("Content for docs/guides/QuickStart.md", quickStart.Content);

        var v2Endpoints = await _service.GetPageAsync("docs/api/v2/Endpoints", null, CancellationToken.None);
        Assert.NotNull(v2Endpoints);

        var userMgmt = await _service.GetPageAsync("admin/users/Management", null, CancellationToken.None);
        Assert.NotNull(userMgmt);
    }

    [Fact]
    public async Task PageWithLongHistory_CanRetrieveAllCommits()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile("page.md", "# Page\n\nVersion 1.", "Initial version");

        // Create 50 commits
        for (int i = 2; i <= 50; i++)
        {
            CommitFile("page.md", $"# Page\n\nVersion {i}.", $"Update to v{i}");
        }

        // Act
        var history = await _service.GetPageHistoryAsync("page", null, CancellationToken.None);

        // Assert
        Assert.Equal(50, history.Count);
        // Commit messages from git log format include trailing newlines
        Assert.StartsWith("Update to v50", history[0].Message);
        Assert.StartsWith("Initial version", history[49].Message);

        // Verify we can read old revisions
        var firstCommit = history[49].CommitId;
        var oldVersion = await _service.GetPageAtRevisionAsync("page", null, firstCommit, CancellationToken.None);
        Assert.NotNull(oldVersion);
        Assert.Contains("Version 1", oldVersion.Content);
    }

    [Fact]
    public async Task Repository_WithMultipleCulturesPerPage_RetrievesCorrectly()
    {
        // Arrange
        InitializeGitRepository();

        var pages = new[] { "Home", "About", "Contact", "Services", "Products" };
        var cultures = new[] { "en", "fr", "de", "es", "it" };

        foreach (var page in pages)
        {
            foreach (var culture in cultures)
            {
                var fileName = culture == "en" ? $"{page}.md" : $"{page}.{culture}.md";
                var content = $"# {page} ({culture})\n\nContent in {culture}.";
                CommitFile(fileName, content, $"Add {page} in {culture}");
            }
        }

        // Act
        var allPages = await _service.GetAllPagesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(pages.Length * cultures.Length, allPages.Count);

        // Verify each page has all cultures
        foreach (var page in pages)
        {
            var availableCultures = await _service.GetAvailableCulturesForPageAsync(page, CancellationToken.None);
            Assert.Equal(cultures.Length, availableCultures.Count);
            foreach (var culture in cultures)
            {
                Assert.Contains(culture, availableCultures);
            }
        }
    }

    [Fact]
    public async Task Repository_WithLargeMediaFiles_HandlesCorrectly()
    {
        // Arrange
        InitializeGitRepository();

        // Create page with reference to media
        var content = "# Media Gallery\n\n![Image](images/large.jpg)\n\n[PDF](docs/manual.pdf)";
        CommitFile("gallery.md", content, "Add gallery");

        // Add large-ish media files
        var imageBytes = new byte[1024 * 100]; // 100 KB
        new Random(42).NextBytes(imageBytes);
        CommitBinaryFile("images/large.jpg", imageBytes, "Add large image");

        var pdfBytes = new byte[1024 * 500]; // 500 KB
        new Random(43).NextBytes(pdfBytes);
        CommitBinaryFile("docs/manual.pdf", pdfBytes, "Add PDF");

        // Act
        var retrievedImage = await _service.GetMediaFileAsync("images/large.jpg", CancellationToken.None);
        var retrievedPdf = await _service.GetMediaFileAsync("docs/manual.pdf", CancellationToken.None);

        // Assert
        Assert.NotNull(retrievedImage);
        Assert.Equal(imageBytes.Length, retrievedImage.Length);

        Assert.NotNull(retrievedPdf);
        Assert.Equal(pdfBytes.Length, retrievedPdf.Length);
    }

    [Fact]
    public async Task ConcurrentPageReads_FromSharedRepository_Succeeds()
    {
        // Arrange
        InitializeGitRepository();

        // Create multiple pages
        for (int i = 0; i < 20; i++)
        {
            CommitFile($"page{i}.md", $"# Page {i}\n\nContent {i}.", $"Add page {i}");
        }

        // Act - Read pages concurrently
        var tasks = new List<Task<WikiPage?>>();
        for (int i = 0; i < 20; i++)
        {
            var pageName = $"page{i}";
            tasks.Add(_service.GetPageAsync(pageName, null, CancellationToken.None));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(20, results.Length);
        Assert.All(results, page => Assert.NotNull(page));
        for (int i = 0; i < 20; i++)
        {
            Assert.Equal($"page{i}", results[i]!.PageName);
        }
    }

    [Fact]
    public async Task SequentialWrites_BuildCorrectHistory()
    {
        // Arrange
        InitializeGitRepository();
        // Create an initial commit so the branch exists
        CommitFile(".gitkeep", "", "Initial commit");
        var author = CreateTestUser();

        // Act - Sequential writes
        for (int i = 1; i <= 20; i++)
        {
            var content = $"# Version {i}\n\nContent for version {i}.";
            await _service.SavePageWithMediaAsync("evolving", null, content, $"Update to v{i}", author, new(), CancellationToken.None);
        }

        // Assert
        var history = await _service.GetPageHistoryAsync("evolving", null, CancellationToken.None);
        Assert.Equal(20, history.Count);

        // Verify with git CLI
        var gitLog = GetGitLog("evolving.md");
        for (int i = 1; i <= 20; i++)
        {
            Assert.Contains($"Update to v{i}", gitLog);
        }

        // Verify current version
        var currentPage = await _service.GetPageAsync("evolving", null, CancellationToken.None);
        Assert.NotNull(currentPage);
        Assert.Contains("Version 20", currentPage.Content);
    }

    #endregion

    #region Edge Cases and Error Scenarios

    [Fact]
    public async Task GetPageAsync_EmptyRepository_ReturnsNull()
    {
        // Arrange
        InitializeGitRepository();
        // Create an initial commit so the branch exists
        CommitFile(".gitkeep", "", "Initial commit");

        // Act
        var result = await _service.GetPageAsync("nonexistent", null, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SavePageWithMediaAsync_FirstCommit_InitializesRepository()
    {
        // Arrange
        InitializeGitRepository();
        // Create an initial commit so the branch exists
        CommitFile(".gitkeep", "", "Initialize repository");
        var author = CreateTestUser();
        var content = "# First Page\n\nFirst content.";

        // Act
        await _service.SavePageWithMediaAsync("first", null, content, "Add first page", author, new(), CancellationToken.None);

        // Assert
        var page = await _service.GetPageAsync("first", null, CancellationToken.None);
        Assert.NotNull(page);
        Assert.Equal(content, page.Content);

        // Verify with git
        var gitContent = GetGitFileContent("first.md");
        Assert.Equal(content, gitContent);
    }

    [Fact]
    public async Task PageWithSpecialCharacters_InFilename_HandlesCorrectly()
    {
        // Arrange
        InitializeGitRepository();
        // Use only valid characters: letters, numbers, dash, underscore, forward slash
        var pageName = "page-with_special123";
        var content = "# Special Page\n\nContent.";
        CommitFile($"{pageName}.md", content, "Add special page");

        // Act
        var result = await _service.GetPageAsync(pageName, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(content, result.Content);
    }

    [Fact]
    public async Task PageWithUnicodeContent_PreservesEncoding()
    {
        // Arrange
        InitializeGitRepository();
        var content = "# Unicode Test\n\nÄÖÜäöüß 中文 日本語 한글 😀🎉";
        CommitFile("unicode.md", content, "Add unicode content");

        // Act
        var result = await _service.GetPageAsync("unicode", null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(content, result.Content);
    }

    [Fact]
    public async Task SaveAndRead_WithMediaContainingBinaryData_PreservesData()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");
        var author = CreateTestUser();

        // Create binary data with all byte values
        var binaryData = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            binaryData[i] = (byte)i;
        }

        var content = "# Binary Test\n\nPage with binary media.";
        var mediaFiles = new Dictionary<string, byte[]>
        {
            ["data/binary.bin"] = binaryData
        };

        // Act
        await _service.SavePageWithMediaAsync("bintest", null, content, "Add binary media", author, mediaFiles, CancellationToken.None);

        // Assert
        var retrievedMedia = await _service.GetMediaFileAsync("data/binary.bin", CancellationToken.None);
        Assert.NotNull(retrievedMedia);
        Assert.Equal(binaryData, retrievedMedia);
    }

    [Fact]
    public async Task GetAllMediaFilesAsync_ReturnsAllMediaFiles()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");
        var author = CreateTestUser();

        var pngData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var jpgData = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var pdfData = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        var content = "# Test Page\n\nPage with multiple media files.";
        var mediaFiles = new Dictionary<string, byte[]>
        {
            ["images/logo.png"] = pngData,
            ["photos/picture.jpg"] = jpgData,
            ["documents/guide.pdf"] = pdfData
        };

        await _service.SavePageWithMediaAsync("mediatest", null, content, "Add media files", author, mediaFiles, CancellationToken.None);

        // Act
        var result = await _service.GetAllMediaFilesAsync(CancellationToken.None);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, m => m.Path == "images/logo.png" && m.MediaType == Models.MediaType.Image);
        Assert.Contains(result, m => m.Path == "photos/picture.jpg" && m.MediaType == Models.MediaType.Image);
        Assert.Contains(result, m => m.Path == "documents/guide.pdf" && m.MediaType == Models.MediaType.Document);
    }

    [Fact]
    public async Task GetAllMediaFilesAsync_WithNoMediaFiles_ReturnsEmptyList()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");

        // Act
        var result = await _service.GetAllMediaFilesAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllMediaFilesAsync_FiltersOnlyAllowedExtensions()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");
        var author = CreateTestUser();

        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var content = "# Test Page";

        var mediaFiles = new Dictionary<string, byte[]>
        {
            ["images/valid.png"] = imageData
        };

        await _service.SavePageWithMediaAsync("test", null, content, "Add media", author, mediaFiles, CancellationToken.None);

        // Verify media was committed using GetMediaFileAsync
        var retrievedMedia = await _service.GetMediaFileAsync("images/valid.png", CancellationToken.None);
        Assert.NotNull(retrievedMedia);

        CommitFile("data/text.txt", "This is text", "Add text file");

        // Act
        var result = await _service.GetAllMediaFilesAsync(CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("images/valid.png", result[0].Path);
        Assert.Equal(Models.MediaType.Image, result[0].MediaType);
    }

    #endregion
}

