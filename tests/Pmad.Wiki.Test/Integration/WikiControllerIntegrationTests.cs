using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Pmad.Wiki.Controllers;
using Pmad.Wiki.Models;
using Pmad.Wiki.Resources;
using Pmad.Wiki.Services;
using Pmad.Wiki.Test.Infrastructure;

namespace Pmad.Wiki.Test.Integration;

/// <summary>
/// Integration tests for WikiController that use real Git repositories.
/// These tests verify the full stack including Git operations.
/// </summary>
public class WikiControllerIntegrationTests : IDisposable
{
    private readonly string _testRepoRoot;
    private readonly string _testRepoPath;
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<IStringLocalizer<WikiResources>> _mockLocalizer;
    private readonly Mock<IWikiUserService> _mockUserService;
    private readonly string _branchName = "main";

    public WikiControllerIntegrationTests()
    {
        // Create a unique temporary directory for this test run
        _testRepoRoot = Path.Combine(Path.GetTempPath(), "WikiControllerIntegrationTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRepoRoot);

        _testRepoPath = Path.Combine(_testRepoRoot, "wiki");

        var services = new ServiceCollection();
        services.AddWiki(options =>
        {
            options.RepositoryRoot = _testRepoRoot;
            options.WikiRepositoryName = "wiki";
            options.BranchName = _branchName;
            options.NeutralMarkdownPageCulture = "en";
            options.HomePageName = "Home";
            options.AllowAnonymousViewing = true;
            options.UsePageLevelPermissions = false;
            options.AllowedMediaExtensions = new List<string> { ".png", ".jpg", ".jpeg", ".gif", ".pdf", ".mp4", ".svg" };
        });
        services.AddSingleton<LinkGenerator, TestLinkGenerator>();

        // Mock IWikiUserService
        _mockUserService = new Mock<IWikiUserService>();
        services.AddSingleton<IWikiUserService>(_mockUserService.Object);
        
        _serviceProvider = services.BuildServiceProvider();

        _mockLocalizer = new Mock<IStringLocalizer<WikiResources>>();
        _mockLocalizer
            .Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        _mockLocalizer
            .Setup(x => x[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));
    }

    private WikiController CreateController()
    {
        // Note: A controller MUST NOT be reused across tests due to its internal state (e.g. TempData, ControllerContext)

        var pageService = _serviceProvider.GetRequiredService<IWikiPageService>();
        var accessControlService = _serviceProvider.GetRequiredService<IPageAccessControlService>();
        var markdownRenderService = _serviceProvider.GetRequiredService<IMarkdownRenderService>();
        var wikiPageEditService = _serviceProvider.GetRequiredService<IWikiPageEditService>();
        var tempMediaStorage = _serviceProvider.GetRequiredService<ITemporaryMediaStorageService>();
        var pagePermissionHelper = _serviceProvider.GetRequiredService<IWikiPagePermissionHelper>();
        var options = _serviceProvider.GetRequiredService<IOptions<WikiOptions>>();

        var logger = new Mock<ILogger<WikiController>>().Object;
        var templateService = _serviceProvider.GetRequiredService<IWikiTemplateService>();

        var _controller = new WikiController(
            pageService,
            _mockUserService.Object,
            accessControlService,
            markdownRenderService,
            tempMediaStorage,
            wikiPageEditService,
            templateService,
            options,
            logger,
            _mockLocalizer.Object,
            pagePermissionHelper);

        SetupControllerContext(_controller);

        return _controller;
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

    private string GetGitFileContent(string filePath, string? revision = null)
    {
        var refSpec = revision != null ? $"{revision}:{filePath}" : $"HEAD:{filePath}";
        return RunGitCommand(_testRepoPath, "show", refSpec);
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

    private void SetupControllerContext(WikiController _controller, ClaimsPrincipal? user = null)
    {
        var httpContext = new DefaultHttpContext
        {
            User = user ?? new ClaimsPrincipal(new ClaimsIdentity())
        };

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor());

        _controller.ControllerContext = new ControllerContext(actionContext);
        _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        // For Edit POST actions, we don't actually need the URL helper as it returns a redirect result
        // For other actions that might need it, we set a simple implementation
        _controller.Url = new TestUrlHelper();
    }

    private void SetupAuthenticatedUser(WikiController _controller, string name, string email, bool canView = true, bool canEdit = true, bool canAdmin = false, string[]? groups = null)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Email, email)
        };

        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        SetupControllerContext(_controller, user);

        var mockUser = new Mock<IWikiUser>();
        mockUser.Setup(x => x.DisplayName).Returns(name);
        mockUser.Setup(x => x.GitName).Returns(name);
        mockUser.Setup(x => x.GitEmail).Returns(email);

        var mockWikiUser = new Mock<IWikiUserWithPermissions>();
        mockWikiUser.Setup(x => x.CanView).Returns(canView);
        mockWikiUser.Setup(x => x.CanEdit).Returns(canEdit);
        mockWikiUser.Setup(x => x.CanAdmin).Returns(canAdmin);
        mockWikiUser.Setup(x => x.Groups).Returns(groups ?? Array.Empty<string>());
        mockWikiUser.Setup(x => x.User).Returns(mockUser.Object);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockWikiUser.Object);
    }

    #endregion

    #region View Action Tests

    [Fact]
    public async Task View_WithExistingPage_ReturnsViewResult()
    {
        // Arrange
        InitializeGitRepository();
        var content = "# Test Page\n\nThis is a test page.";
        CommitFile("test.md", content, "Add test page");

        // Act
        var result = await CreateController().View("test", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.Equal("test", model.PageName);
        Assert.Contains("Test Page", model.Title);
        Assert.Contains("<h1", model.HtmlContent);
    }

    [Fact]
    public async Task View_WithNonExistentPage_ReturnsNotFound()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");

        // Act
        var result = await CreateController().View("nonexistent", null, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task View_WithNonExistentPageAndEditPermission_RedirectsToEdit()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");
        SetupAuthenticatedUser(controller, "Editor", "editor@example.com", canEdit: true);

        // Act
        var result = await controller.View("newpage", null, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WikiController.Edit), redirectResult.ActionName);
        Assert.Equal("newpage", redirectResult.RouteValues?["id"]);
    }

    [Fact]
    public async Task View_WithLocalizedPage_ReturnsCorrectCulture()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile("page.md", "# English Page\n\nEnglish content.", "Add English page");
        CommitFile("page.fr.md", "# Page Française\n\nContenu français.", "Add French page");

        // Act
        var englishResult = await CreateController().View("page", null, CancellationToken.None);
        var frenchResult = await CreateController().View("page", "fr", CancellationToken.None);

        // Assert
        var englishView = Assert.IsType<ViewResult>(englishResult);
        var englishModel = Assert.IsType<WikiPageViewModel>(englishView.Model);
        Assert.Equal("English Page", englishModel.Title);

        var frenchView = Assert.IsType<ViewResult>(frenchResult);
        var frenchModel = Assert.IsType<WikiPageViewModel>(frenchView.Model);
        Assert.Equal("Page Française", frenchModel.Title);
        
        // Verify both cultures are available
        Assert.Contains("fr", frenchModel.AvailableCultures);
        Assert.Contains("en", frenchModel.AvailableCultures);
    }

    [Fact]
    public async Task View_WithNestedPage_ReturnsBreadcrumb()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile("docs/guide/tutorial.md", "# Tutorial\n\nTutorial content.", "Add tutorial");
        CommitFile("docs/guide.md", "# Guide\n\nGuide content.", "Add guide");
        CommitFile("docs.md", "# Documentation\n\nDocs content.", "Add docs");

        // Act
        var result = await CreateController().View("docs/guide/tutorial", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.Equal(3, model.Breadcrumb.Count);
        Assert.Equal("docs", model.Breadcrumb[0].PageName);
        Assert.Equal("docs/guide", model.Breadcrumb[1].PageName);
        Assert.Equal("docs/guide/tutorial", model.Breadcrumb[2].PageName);
    }

    [Fact]
    public async Task View_WithMultipleCultures_ReturnsAvailableCultures()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile("page.md", "# Page", "Add English");
        CommitFile("page.fr.md", "# Page FR", "Add French");
        CommitFile("page.de.md", "# Page DE", "Add German");

        // Act
        var result = await CreateController().View("page", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.Equal(3, model.AvailableCultures.Count);
        Assert.Contains("en", model.AvailableCultures);
        Assert.Contains("fr", model.AvailableCultures);
        Assert.Contains("de", model.AvailableCultures);
    }

    [Fact]
    public async Task View_DefaultPageName_LoadsHomePage()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile("Home.md", "# Welcome\n\nHome page content.", "Add home page");

        // Act
        var result = await CreateController().View("", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.Equal("Home", model.PageName);
        Assert.Contains("Welcome", model.Title);
    }

    #endregion

    #region History Action Tests

    [Fact]
    public async Task History_WithMultipleCommits_ReturnsAllHistory()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile("page.md", "# Page v1", "First commit");
        CommitFile("page.md", "# Page v2", "Second commit");
        CommitFile("page.md", "# Page v3", "Third commit");

        // Act
        var result = await CreateController().History("page", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiHistoryViewModel>(viewResult.Model);
        Assert.Equal(3, model.Entries.Count);
        Assert.StartsWith("Third commit", model.Entries[0].Message);
        Assert.StartsWith("Second commit", model.Entries[1].Message);
        Assert.StartsWith("First commit", model.Entries[2].Message);
    }

    [Fact]
    public async Task History_WithNonExistentPage_ReturnsEmptyHistory()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");

        // Act
        var result = await CreateController().History("nonexistent", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiHistoryViewModel>(viewResult.Model);
        Assert.Empty(model.Entries);
    }

    #endregion

    #region Revision Action Tests

    [Fact]
    public async Task Revision_WithOldRevision_ReturnsCorrectContent()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile("page.md", "# Old Version", "Initial version");
        var oldCommit = GetLatestCommitHash();
        CommitFile("page.md", "# New Version", "Updated version");

        // Act
        var result = await CreateController().Revision("page", oldCommit, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        Assert.Contains("Old Version", model.Title);
        Assert.Equal(oldCommit, model.CommitId);
    }

    [Fact]
    public async Task Revision_WithInvalidCommitId_ReturnsNotFound()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile("page.md", "# Page", "Add page");

        // Act
        var result = await CreateController().Revision("page", "0000000000000000000000000000000000000000", null, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region Diff Action Tests

    [Fact]
    public async Task Diff_BetweenTwoRevisions_ReturnsCorrectContent()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile("page.md", "# Version 1\n\nContent v1.", "First version");
        var firstCommit = GetLatestCommitHash();
        CommitFile("page.md", "# Version 2\n\nContent v2.", "Second version");
        var secondCommit = GetLatestCommitHash();

        // Act
        var result = await CreateController().Diff("page", firstCommit, secondCommit, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        Assert.Contains("Version 1", model.FromContent);
        Assert.Contains("Version 2", model.ToContent);
        Assert.Equal(firstCommit, model.FromCommitId);
        Assert.Equal(secondCommit, model.ToCommitId);
    }

    #endregion

    #region SiteMap Action Tests

    [Fact]
    public async Task SiteMap_WithMultiplePages_ReturnsAllPages()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile("Home.md", "# Home", "Add home");
        CommitFile("About.md", "# About", "Add about");
        CommitFile("docs/Guide.md", "# Guide", "Add guide");
        CommitFile("docs/api/Reference.md", "# Reference", "Add reference");

        // Act
        var result = await CreateController().SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        Assert.NotEmpty(model.RootNodes);
    }

    #endregion

    #region Edit Action - GET Tests

    [Fact]
    public async Task Edit_Get_WithExistingPage_ReturnsEditView()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        var content = "# Test Page\n\nExisting content.";
        CommitFile("test.md", content, "Add test page");
        SetupAuthenticatedUser(controller, "Editor", "editor@example.com", canEdit: true);

        // Act
        var result = await controller.Edit("test", null, null, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageEditViewModel>(viewResult.Model);
        Assert.Equal("test", model.PageName);
        Assert.Equal(content, model.Content);
        Assert.False(model.IsNew);
    }

    [Fact]
    public async Task Edit_Get_WithNewPage_ReturnsEmptyEditView()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");
        SetupAuthenticatedUser(controller, "Editor", "editor@example.com", canEdit: true);

        // Act
        var result = await controller.Edit("newpage", null, null, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageEditViewModel>(viewResult.Model);
        Assert.Equal("newpage", model.PageName);
        Assert.Empty(model.Content);
        Assert.True(model.IsNew);
    }

    [Fact]
    public async Task Edit_Get_WithRestoreCommit_LoadsOldRevision()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        CommitFile("page.md", "# Old Content", "Initial version");
        var oldCommit = GetLatestCommitHash();
        CommitFile("page.md", "# New Content", "Updated version");
        SetupAuthenticatedUser(controller, "Editor", "editor@example.com", canEdit: true);

        // Act
        var result = await controller.Edit("page", null, oldCommit, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageEditViewModel>(viewResult.Model);
        Assert.Contains("Old Content", model.Content);
        Assert.Contains($"Restore page page to revision {oldCommit.Substring(0, 8)}", model.CommitMessage);
    }

    [Fact]
    public async Task Edit_Get_WithoutEditPermission_ReturnsForbid()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        CommitFile("test.md", "# Test", "Add test page");
        SetupAuthenticatedUser(controller, "Viewer", "viewer@example.com", canEdit: false);

        // Act
        var result = await controller.Edit("test", null, null, null, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    #endregion

    #region Edit Action - POST Tests

    [Fact]
    public async Task Edit_Post_CreateNewPage_CommitsToGit()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");
        SetupAuthenticatedUser(controller, "Editor", "editor@example.com", canEdit: true);

        var model = new WikiPageEditViewModel
        {
            PageName = "newpage",
            Content = "# New Page\n\nNew content.",
            CommitMessage = "Create new page",
            IsNew = true
        };

        // Act
        var result = await controller.Edit(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WikiController.View), redirectResult.ActionName);
        Assert.Equal("newpage", redirectResult.RouteValues?["id"]);

        // Verify with git CLI
        var gitContent = GetGitFileContent("newpage.md");
        Assert.Equal("# New Page\n\nNew content.", gitContent);

        var commitMessage = GetCommitMessage(GetLatestCommitHash());
        Assert.Equal("Create new page", commitMessage);
    }

    [Fact]
    public async Task Edit_Post_UpdateExistingPage_CommitsToGit()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        CommitFile("page.md", "# Original Content", "Initial commit");
        SetupAuthenticatedUser(controller, "Editor", "editor@example.com", canEdit: true);

        // Get the content hash of the original page
        var pageService = _serviceProvider.GetRequiredService<IWikiPageService>();
        var originalPage = await pageService.GetPageAsync("page", null, CancellationToken.None);

        var model = new WikiPageEditViewModel
        {
            PageName = "page",
            Content = "# Updated Content",
            CommitMessage = "Update page",
            IsNew = false,
            OriginalContentHash = originalPage?.ContentHash
        };

        // Act
        var result = await controller.Edit(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WikiController.View), redirectResult.ActionName);

        // Verify with git CLI
        var gitContent = GetGitFileContent("page.md");
        Assert.Equal("# Updated Content", gitContent);

        var commitMessage = GetCommitMessage(GetLatestCommitHash());
        Assert.Equal("Update page", commitMessage);
    }

    [Fact]
    public async Task Edit_Post_WithLocalizedPage_CommitsCorrectFile()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");
        SetupAuthenticatedUser(controller, "Editor", "editor@example.com", canEdit: true);

        var model = new WikiPageEditViewModel
        {
            PageName = "page",
            Culture = "fr",
            Content = "# Page Française",
            CommitMessage = "Add French page",
            IsNew = true
        };

        // Act
        await controller.Edit(model, CancellationToken.None);

        // Assert - Verify with git CLI
        var gitContent = GetGitFileContent("page.fr.md");
        Assert.Equal("# Page Française", gitContent);
    }

    [Fact]
    public async Task Edit_Post_WithNestedPage_CreatesDirectories()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");
        SetupAuthenticatedUser(controller, "Editor", "editor@example.com", canEdit: true);

        var model = new WikiPageEditViewModel
        {
            PageName = "docs/guides/tutorial",
            Content = "# Tutorial",
            CommitMessage = "Add tutorial",
            IsNew = true
        };

        // Act
        await controller.Edit(model, CancellationToken.None);

        // Assert - Verify with git CLI
        var gitContent = GetGitFileContent("docs/guides/tutorial.md");
        Assert.Equal("# Tutorial", gitContent);
    }

    [Fact]
    public async Task Edit_Post_WithoutEditPermission_ReturnsForbid()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        CommitFile("page.md", "# Page", "Add page");
        SetupAuthenticatedUser(controller, "Viewer", "viewer@example.com", canEdit: false);

        var model = new WikiPageEditViewModel
        {
            PageName = "page",
            Content = "# Modified",
            CommitMessage = "Update"
        };

        // Act
        var result = await controller.Edit(model, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    #endregion

    #region Media Action Tests

    [Fact]
    public async Task Media_WithExistingImage_ReturnsFileResult()
    {
        // Arrange
        InitializeGitRepository();
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG header
        CommitBinaryFile("images/logo.png", imageBytes, "Add logo");

        // Act
        var result = await CreateController().Media("images/logo.png", CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(imageBytes, fileResult.FileContents);
        Assert.Equal("image/png", fileResult.ContentType);
    }

    [Fact]
    public async Task Media_WithNonExistentFile_ReturnsNotFound()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");

        // Act
        var result = await CreateController().Media("images/nonexistent.png", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region Large/Complex Repository Tests

    [Fact]
    public async Task View_LargeRepository_With100Pages_HandlesCorrectly()
    {
        // Arrange
        InitializeGitRepository();

        // Create 100 pages
        for (int i = 0; i < 100; i++)
        {
            var pageName = $"page{i:D3}";
            var content = $"# Page {i}\n\nContent for page {i}.";
            CommitFile($"{pageName}.md", content, $"Add {pageName}");
        }

        // Act - Test viewing a few pages
        var result1 = await CreateController().View("page000", null, CancellationToken.None);
        var result50 = await CreateController().View("page050", null, CancellationToken.None);
        var result99 = await CreateController().View("page099", null, CancellationToken.None);

        // Assert
        Assert.Equal("Page 0", Assert.IsType<WikiPageViewModel>(Assert.IsType<ViewResult>(result1).Model).Title);
        Assert.Equal("Page 50", Assert.IsType<WikiPageViewModel>(Assert.IsType<ViewResult>(result50).Model).Title);
        Assert.Equal("Page 99", Assert.IsType<WikiPageViewModel>(Assert.IsType<ViewResult>(result99).Model).Title);
    }

    [Fact]
    public async Task SiteMap_ComplexNestedStructure_BuildsCorrectHierarchy()
    {
        // Arrange
        InitializeGitRepository();

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
        var result = await CreateController().SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        Assert.NotEmpty(model.RootNodes);
    }

    [Fact]
    public async Task History_PageWith50Commits_ReturnsAllHistory()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile("page.md", "# Page\n\nVersion 1.", "Initial version");

        // Create 49 more commits
        for (int i = 2; i <= 50; i++)
        {
            CommitFile("page.md", $"# Page\n\nVersion {i}.", $"Update to v{i}");
        }

        // Act
        var result = await CreateController().History("page", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiHistoryViewModel>(viewResult.Model);
        Assert.Equal(50, model.Entries.Count);
        Assert.StartsWith("Update to v50", model.Entries[0].Message);
        Assert.StartsWith("Initial version", model.Entries[49].Message);
    }

    [Fact]
    public async Task View_WithMultipleCulturesPerPage_RetrievesCorrectly()
    {
        // Arrange
        InitializeGitRepository();

        var pages = new[] { "Home", "About", "Contact" };
        var cultures = new[] { "en", "fr", "de", "es" };

        foreach (var page in pages)
        {
            foreach (var culture in cultures)
            {
                var fileName = culture == "en" ? $"{page}.md" : $"{page}.{culture}.md";
                var content = $"# {page} ({culture})\n\nContent in {culture}.";
                CommitFile(fileName, content, $"Add {page} in {culture}");
            }
        }

        // Act - Test viewing different cultures
        var homeEn = await CreateController().View("Home", null, CancellationToken.None);
        var homeFr = await CreateController().View("Home", "fr", CancellationToken.None);
        var aboutDe = await CreateController().View("About", "de", CancellationToken.None);

        // Assert
        var homeEnView = Assert.IsType<ViewResult>(homeEn);
        var homeEnModel = Assert.IsType<WikiPageViewModel>(homeEnView.Model);
        Assert.Equal(4, homeEnModel.AvailableCultures.Count);

        var homeFrView = Assert.IsType<ViewResult>(homeFr);
        var homeFrModel = Assert.IsType<WikiPageViewModel>(homeFrView.Model);
        Assert.Contains("Home (fr)", homeFrModel.Title);

        var aboutDeView = Assert.IsType<ViewResult>(aboutDe);
        var aboutDeModel = Assert.IsType<WikiPageViewModel>(aboutDeView.Model);
        Assert.Contains("About (de)", aboutDeModel.Title);
    }

    #endregion

    #region Sequential Operations Tests

    [Fact]
    public async Task SequentialEdits_BuildCorrectHistory()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initial commit");

        // Act - Sequential edits
        for (int i = 1; i <= 10; i++)
        {
            var model = new WikiPageEditViewModel
            {
                PageName = "evolving",
                Content = $"# Version {i}\n\nContent for version {i}.",
                CommitMessage = $"Update to v{i}",
                IsNew = i == 1
            };

            if (i > 1)
            {
                var pageService = _serviceProvider.GetRequiredService<IWikiPageService>();
                var currentPage = await pageService.GetPageAsync("evolving", null, CancellationToken.None);
                model.OriginalContentHash = currentPage?.ContentHash;
            }

            var controller = CreateController();
            SetupAuthenticatedUser(controller, "Editor", "editor@example.com", canEdit: true);
            await controller.Edit(model, CancellationToken.None);
        }

        // Assert - Verify history
        var historyResult = await CreateController().History("evolving", null, CancellationToken.None);
        var historyView = Assert.IsType<ViewResult>(historyResult);
        var historyModel = Assert.IsType<WikiHistoryViewModel>(historyView.Model);
        Assert.Equal(10, historyModel.Entries.Count);

        // Verify current version
        var currentResult = await CreateController().View("evolving", null, CancellationToken.None);
        var currentView = Assert.IsType<ViewResult>(currentResult);
        var currentModel = Assert.IsType<WikiPageViewModel>(currentView.Model);
        Assert.Contains("Version 10", currentModel.Title);
    }

    #endregion

    #region Concurrent Operations Tests

    [Fact]
    public async Task ConcurrentPageReads_FromSharedRepository_Succeeds()
    {
        // Arrange
        InitializeGitRepository();

        // Create multiple pages
        for (int i = 0; i < 10; i++)
        {
            CommitFile($"page{i}.md", $"# Page {i}\n\nContent {i}.", $"Add page {i}");
        }

        // Act - Read pages concurrently
        var tasks = new List<Task<IActionResult>>();
        for (int i = 0; i < 10; i++)
        {
            var pageName = $"page{i}";
            tasks.Add(CreateController().View(pageName, null, CancellationToken.None));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, results.Length);
        Assert.All(results, result => Assert.IsType<ViewResult>(result));
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task View_WithUnicodeContent_PreservesEncoding()
    {
        // Arrange
        InitializeGitRepository();
        var content = "# Unicode Test\n\nÄÖÜäöüß 中文 日本語 한글 😀🎉";
        CommitFile("unicode.md", content, "Add unicode content");

        // Act
        var result = await CreateController().View("unicode", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.Contains("Unicode Test", model.Title);
        // HTML content should contain the unicode characters (possibly HTML-encoded)
        Assert.NotNull(model.HtmlContent);
    }

    [Fact]
    public async Task View_WithSpecialCharactersInFilename_HandlesCorrectly()
    {
        // Arrange
        InitializeGitRepository();
        // Use only valid characters: letters, numbers, dash, underscore, forward slash
        var pageName = "page-with_special123";
        var content = "# Special Page\n\nContent.";
        CommitFile($"{pageName}.md", content, "Add special page");

        // Act
        var result = await CreateController().View(pageName, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.Equal(pageName, model.PageName);
    }

    #endregion

    #region Create/CreatePage/CreatePageConfirm Integration Tests

    [Fact]
    public async Task Create_WithAuthenticatedUser_ReturnsViewWithTemplates()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        
        // Create template pages
        CommitFile("_templates/MeetingNotes.md", "---\ntitle: Meeting Notes\npattern: Meeting-{date}\nlocation: meetings\n---\n# Meeting Notes\n\nDate: {date}", "Add meeting template");
        CommitFile("_templates/BlogPost.md", "---\ntitle: Blog Post\npattern: Post-{date}\nlocation: blog/posts\n---\n# Blog Post", "Add blog template");
        
        SetupAuthenticatedUser(controller, "Editor", "editor@example.com", canEdit: true);

        // Act
        var result = await controller.Create(null, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreateFromTemplateViewModel>(viewResult.Model);
        Assert.Equal(2, model.Templates.Count);
        Assert.Contains(model.Templates, t => t.DisplayName == "Meeting Notes");
        Assert.Contains(model.Templates, t => t.DisplayName == "Blog Post");
    }

    [Fact]
    public async Task Create_WithFromPageAndCulture_PassesParametersToView()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");
        SetupAuthenticatedUser(controller, "Editor", "editor@example.com", canEdit: true);

        // Act
        var result = await controller.Create("docs/guide", "fr", CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreateFromTemplateViewModel>(viewResult.Model);
        Assert.Equal("docs/guide", model.FromPage);
        Assert.Equal("fr", model.Culture);
    }

    [Fact]
    public async Task Create_WithoutEditPermission_ReturnsForbid()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");
        SetupAuthenticatedUser(controller, "Viewer", "viewer@example.com", canEdit: false);

        // Act
        var result = await controller.Create(null, null, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task CreatePage_WithValidTemplate_LoadsTemplateProperties()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        
        // Create a template with front matter
        var templateContent = "---\ntitle: Daily Report\npattern: Report-{date}\nlocation: reports/{year}\ndescription: Template for daily reports\n---\n# Daily Report\n\nDate: {date}\n\n## Summary\n\n...";
        CommitFile("_templates/DailyReport.md", templateContent, "Add daily report template");
        
        SetupAuthenticatedUser(controller, "Editor", "editor@example.com", canEdit: true);

        // Act
        var result = await controller.CreatePage("_templates/DailyReport", null, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreatePageViewModel>(viewResult.Model);
        Assert.Equal("_templates/DailyReport", model.TemplateId);
        Assert.Equal("Daily Report", model.TemplateName);
        
        // Verify pattern is resolved
        Assert.Contains("Report-", model.PageName);
        Assert.Matches(@"Report-\d{4}-\d{2}-\d{2}", model.PageName);
        
        // Verify location is resolved
        Assert.Contains("reports/", model.Location);
        Assert.Matches(@"reports/\d{4}", model.Location);
    }

    [Fact]
    public async Task CreatePage_WithoutTemplate_UsesDefaultValues()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");
        SetupAuthenticatedUser(controller, "Editor", "editor@example.com", canEdit: true);

        // Act
        var result = await controller.CreatePage(null, "docs/guide", "es", CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreatePageViewModel>(viewResult.Model);
        Assert.Null(model.TemplateId);
        Assert.Equal("NewPage", model.PageName);
        Assert.Equal("docs", model.Location); // Directory from fromPage
        Assert.Equal("docs/guide", model.FromPage);
        Assert.Equal("es", model.Culture);
    }

    [Fact]
    public async Task CreatePage_WithInvalidTemplateId_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");
        SetupAuthenticatedUser(controller, "Editor", "editor@example.com", canEdit: true);

        // Act
        var result = await controller.CreatePage("_templates/NonExistent", null, null, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreatePageConfirm_WithValidInput_RedirectsToEditWithParameters()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");
        SetupAuthenticatedUser(controller, "Editor", "editor@example.com", canEdit: true);

        var model = new WikiCreatePageViewModel
        {
            PageName = "MyNewPage",
            Location = "docs/guides",
            Culture = "fr",
            TemplateId = "_templates/Standard"
        };

        // Act
        var result = await controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WikiController.Edit), redirectResult.ActionName);
        Assert.Equal("docs/guides/MyNewPage", redirectResult.RouteValues?["id"]);
        Assert.Equal("fr", redirectResult.RouteValues?["culture"]);
        Assert.Equal("_templates/Standard", redirectResult.RouteValues?["templateId"]);
    }

    [Fact]
    public async Task CreatePageConfirm_WhenPageAlreadyExists_ReturnsViewWithError()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        CommitFile("docs/ExistingPage.md", "# Existing Page", "Add existing page");
        SetupAuthenticatedUser(controller, "Editor", "editor@example.com", canEdit: true);

        var model = new WikiCreatePageViewModel
        {
            PageName = "ExistingPage",
            Location = "docs"
        };

        // Act
        var result = await controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("CreatePage", viewResult.ViewName);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(string.Empty));
    }

    [Fact]
    public async Task FullCreateWorkflow_FromTemplateToEditedPage_CompletesSuccessfully()
    {
        // Arrange - Create template
        InitializeGitRepository();
        var templateContent = "---\ntitle: Project Documentation\npattern: Project-{date}\nlocation: projects\ndescription: Standard project documentation template\n---\n# Project Documentation\n\nCreated: {date}\n\n## Overview\n\n## Requirements\n\n## Implementation\n";
        CommitFile("_templates/ProjectDoc.md", templateContent, "Add project doc template");

        // Step 1: List templates with Create action
        var createController = CreateController();
        SetupAuthenticatedUser(createController, "Editor", "editor@example.com", canEdit: true);
        var createResult = await createController.Create(null, null, CancellationToken.None);
        var createView = Assert.IsType<ViewResult>(createResult);
        var createModel = Assert.IsType<WikiCreateFromTemplateViewModel>(createView.Model);
        Assert.Single(createModel.Templates);
        var template = createModel.Templates[0];

        // Step 2: Load template with CreatePage action
        var createPageController = CreateController();
        SetupAuthenticatedUser(createPageController, "Editor", "editor@example.com", canEdit: true);
        var createPageResult = await createPageController.CreatePage(template.TemplateName, null, null, CancellationToken.None);
        var createPageView = Assert.IsType<ViewResult>(createPageResult);
        var createPageModel = Assert.IsType<WikiCreatePageViewModel>(createPageView.Model);
        Assert.Equal(template.TemplateName, createPageModel.TemplateId);
        Assert.Equal("Project Documentation", createPageModel.TemplateName);
        Assert.Contains("Project-", createPageModel.PageName);
        Assert.Equal("projects", createPageModel.Location);

        // Step 3: Confirm page creation with CreatePageConfirm action
        var confirmController = CreateController();
        SetupAuthenticatedUser(confirmController, "Editor", "editor@example.com", canEdit: true);
        var confirmModel = new WikiCreatePageViewModel
        {
            PageName = "MyProject",
            Location = createPageModel.Location,
            TemplateId = createPageModel.TemplateId
        };
        var confirmResult = await confirmController.CreatePageConfirm(confirmModel, CancellationToken.None);
        var redirectToEdit = Assert.IsType<RedirectToActionResult>(confirmResult);
        Assert.Equal(nameof(WikiController.Edit), redirectToEdit.ActionName);
        Assert.Equal("projects/MyProject", redirectToEdit.RouteValues?["id"]);

        // Step 4: Edit the page (simulating what happens after redirect)
        var editController = CreateController();
        SetupAuthenticatedUser(editController, "Editor", "editor@example.com", canEdit: true);
        var editGetResult = await editController.Edit("projects/MyProject", null, null, template.TemplateName, CancellationToken.None);
        var editView = Assert.IsType<ViewResult>(editGetResult);
        var editModel = Assert.IsType<WikiPageEditViewModel>(editView.Model);
        Assert.Equal("projects/MyProject", editModel.PageName);
        Assert.True(editModel.IsNew);
        // Content should have placeholders resolved
        Assert.Contains("Created:", editModel.Content);
        Assert.Matches(@"Created: \d{4}-\d{2}-\d{2}", editModel.Content);

        // Step 5: Save the edited page
        editModel.Content += "\n\n## Status\n\nIn progress";
        editModel.CommitMessage = "Create MyProject documentation";
        var editPostResult = await editController.Edit(editModel, CancellationToken.None);
        var redirectToView = Assert.IsType<RedirectToActionResult>(editPostResult);
        Assert.Equal(nameof(WikiController.View), redirectToView.ActionName);

        // Step 6: Verify the page was created correctly in Git
        var gitContent = GetGitFileContent("projects/MyProject.md");
        Assert.Contains("# Project Documentation", gitContent);
        Assert.Contains("## Status", gitContent);
        Assert.Contains("In progress", gitContent);
        Assert.Matches(@"Created: \d{4}-\d{2}-\d{2}", gitContent);

        var commitMessage = GetCommitMessage(GetLatestCommitHash());
        Assert.Equal("Create MyProject documentation", commitMessage);
    }

    [Fact]
    public async Task CreatePageConfirm_WithNestedLocation_CreatesCorrectPath()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");
        SetupAuthenticatedUser(controller, "Editor", "editor@example.com", canEdit: true);

        var model = new WikiCreatePageViewModel
        {
            PageName = "Specification",
            Location = "projects/2024/Q1/ProjectAlpha"
        };

        // Act
        var result = await controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("projects/2024/Q1/ProjectAlpha/Specification", redirectResult.RouteValues?["id"]);
    }

    [Fact]
    public async Task CreatePage_WithTemplateHavingPlaceholders_ResolvesAllPlaceholders()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        
        var now = DateTimeOffset.UtcNow;
        var templateContent = $"---\ntitle: Timestamped Report\npattern: Report-{{year}}-{{month}}-{{day}}\nlocation: reports/{{year}}/{{month}}\n---\n# Report {{date}}\n\nGenerated at: {{datetime}}";
        CommitFile("_templates/TimestampedReport.md", templateContent, "Add timestamped template");
        
        SetupAuthenticatedUser(controller, "Editor", "editor@example.com", canEdit: true);

        // Act
        var result = await controller.CreatePage("_templates/TimestampedReport", null, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreatePageViewModel>(viewResult.Model);
        
        // Verify page name pattern resolved
        Assert.Equal($"Report-{now.Year}-{now.Month:D2}-{now.Day:D2}", model.PageName);
        
        // Verify location pattern resolved
        Assert.Equal($"reports/{now.Year}/{now.Month:D2}", model.Location);
    }

    [Fact]
    public async Task CreatePageConfirm_WithMultipleCultures_CreatesIndependentPages()
    {
        // Arrange
        InitializeGitRepository();
        CommitFile(".gitkeep", "", "Initialize repository");

        // Create English version
        var controllerEn = CreateController();
        SetupAuthenticatedUser(controllerEn, "Editor", "editor@example.com", canEdit: true);
        var modelEn = new WikiCreatePageViewModel
        {
            PageName = "Documentation",
            Location = "docs",
            Culture = null // English (default)
        };
        await controllerEn.CreatePageConfirm(modelEn, CancellationToken.None);

        // Now create it with Edit
        var editControllerEn = CreateController();
        SetupAuthenticatedUser(editControllerEn, "Editor", "editor@example.com", canEdit: true);
        var editModelEn = new WikiPageEditViewModel
        {
            PageName = "docs/Documentation",
            Content = "# English Documentation\n\nEnglish content.",
            CommitMessage = "Create English documentation",
            IsNew = true
        };
        await editControllerEn.Edit(editModelEn, CancellationToken.None);

        // Create French version
        var controllerFr = CreateController();
        SetupAuthenticatedUser(controllerFr, "Editor", "editor@example.com", canEdit: true);
        var modelFr = new WikiCreatePageViewModel
        {
            PageName = "Documentation",
            Location = "docs",
            Culture = "fr"
        };
        var resultFr = await controllerFr.CreatePageConfirm(modelFr, CancellationToken.None);

        // Assert - Both should succeed
        var redirectFr = Assert.IsType<RedirectToActionResult>(resultFr);
        Assert.Equal("docs/Documentation", redirectFr.RouteValues?["id"]);
        Assert.Equal("fr", redirectFr.RouteValues?["culture"]);

        // Verify both files exist in Git
        var englishContent = GetGitFileContent("docs/Documentation.md");
        Assert.Contains("English Documentation", englishContent);

        // French file should not exist yet (CreatePageConfirm only validates and redirects to Edit)
        // But the validation should pass because it's a different culture
    }

    [Fact]
    public async Task Create_WithNestedTemplates_ReturnsAllTemplates()
    {
        // Arrange
        var controller = CreateController();
        InitializeGitRepository();
        
        // Create templates in different locations
        CommitFile("_templates/Basic.md", "# Basic Template", "Add basic template");
        CommitFile("_templates/reports/Weekly.md", "---\ntitle: Weekly Report\n---\n# Weekly Report", "Add weekly report");
        CommitFile("_templates/reports/Monthly.md", "---\ntitle: Monthly Report\n---\n# Monthly Report", "Add monthly report");
        
        SetupAuthenticatedUser(controller, "Editor", "editor@example.com", canEdit: true);

        // Act
        var result = await controller.Create(null, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreateFromTemplateViewModel>(viewResult.Model);
        Assert.Equal(3, model.Templates.Count);
        
        // Templates should be sorted by display name
        var templateNames = model.Templates.Select(t => t.DisplayName ?? t.TemplateName).ToList();
        Assert.Equal(templateNames.OrderBy(n => n).ToList(), templateNames);
    }

    #endregion
}
