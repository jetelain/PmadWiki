using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pmad.Wiki.Models;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Controllers;

public class WikiController_DiffTests : WikiControllerTestBase
{
    #region Diff Action Tests

    [Fact]
    public async Task Diff_WithValidParameters_ReturnsViewWithDiff()
    {
        // Arrange
        var fromPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Original Content",
            ContentHash = "hash1",
            HtmlContent = "<h1>Original Content</h1>",
            Title = "Test Page",
            LastModifiedBy = "author1",
            LastModified = DateTimeOffset.UtcNow.AddDays(-2)
        };

        var toPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Updated Content",
            ContentHash = "hash2",
            HtmlContent = "<h1>Updated Content</h1>",
            Title = "Test Page",
            LastModifiedBy = "author2",
            LastModified = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "Initial commit",
                AuthorName = "John Doe",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-2)
            },
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = "Update content",
                AuthorName = "Jane Smith",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Diff("TestPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        
        Assert.Equal("TestPage", model.PageName);
        Assert.Null(model.Culture);
        Assert.Equal("commit1", model.FromCommitId);
        Assert.Equal("commit2", model.ToCommitId);
        Assert.Equal("John Doe", model.FromAuthorName);
        Assert.Equal("Jane Smith", model.ToAuthorName);
        Assert.Equal("Initial commit", model.FromMessage);
        Assert.Equal("Update content", model.ToMessage);
        Assert.Equal("# Original Content", model.FromContent);
        Assert.Equal("# Updated Content", model.ToContent);
        Assert.False(model.CanEdit);
    }

    [Fact]
    public async Task Diff_WithEmptyPageName_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Diff("", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Page name is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task Diff_WithNullPageName_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Diff(null!, "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Page name is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task Diff_WithEmptyFromCommit_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Diff("TestPage", "", "commit2", null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("From commit ID is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task Diff_WithNullFromCommit_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Diff("TestPage", null!, "commit2", null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("From commit ID is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task Diff_WithEmptyToCommit_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Diff("TestPage", "commit1", "", null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("To commit ID is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task Diff_WithNullToCommit_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Diff("TestPage", "commit1", null!, null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("To commit ID is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task Diff_WithInvalidPageName_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Diff("../../../etc/passwd", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid page name.", badRequestResult.Value);
    }

    [Fact]
    public async Task Diff_WithInvalidCulture_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Diff("TestPage", "commit1", "commit2", "invalid-culture-code", CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid culture identifier.", badRequestResult.Value);
    }

    [Fact]
    public async Task Diff_WhenAnonymousViewingDisabledAndUserNotAuthenticated_ReturnsChallenge()
    {
        // Arrange
        _options.AllowAnonymousViewing = false;

        // Act
        var result = await _controller.Diff("TestPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task Diff_WhenUserAuthenticatedButCannotView_ReturnsForbid()
    {
        // Arrange
        _options.AllowAnonymousViewing = false;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(false);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Diff("TestPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Diff_WhenFromPageNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WikiPage?)null);

        var toPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Content",
            ContentHash = "hash2",
            HtmlContent = "<h1>Content</h1>",
            Title = "Test Page"
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        // Act
        var result = await _controller.Diff("TestPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Diff_WhenToPageNotFound_ReturnsNotFound()
    {
        // Arrange
        var fromPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Content",
            ContentHash = "hash1",
            HtmlContent = "<h1>Content</h1>",
            Title = "Test Page"
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WikiPage?)null);

        // Act
        var result = await _controller.Diff("TestPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Diff_WhenBothPagesNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WikiPage?)null);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WikiPage?)null);

        // Act
        var result = await _controller.Diff("TestPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Diff_WithCulture_ReturnsDiffInSpecificCulture()
    {
        // Arrange
        var fromPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Contenu Original",
            ContentHash = "hash1",
            HtmlContent = "<h1>Contenu Original</h1>",
            Title = "Page Test",
            Culture = "fr"
        };

        var toPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Contenu Mis à Jour",
            ContentHash = "hash2",
            HtmlContent = "<h1>Contenu Mis à Jour</h1>",
            Title = "Page Test",
            Culture = "fr"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "Création",
                AuthorName = "Jean Dupont",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-2)
            },
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = "Mise à jour",
                AuthorName = "Marie Martin",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", "fr", "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", "fr", "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", "fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Diff("TestPage", "commit1", "commit2", "fr", CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        
        Assert.Equal("TestPage", model.PageName);
        Assert.Equal("fr", model.Culture);
        Assert.Equal("Jean Dupont", model.FromAuthorName);
        Assert.Equal("Marie Martin", model.ToAuthorName);
        Assert.Equal("Création", model.FromMessage);
        Assert.Equal("Mise à jour", model.ToMessage);
    }

    [Fact]
    public async Task Diff_WithPageLevelPermissionsEnabledAndNoReadAccess_ReturnsForbid()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "users" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var pageAccess = new PageAccessPermissions
        {
            CanRead = false,
            CanEdit = false
        };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("AdminPage", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Diff("AdminPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Diff_WithPageLevelPermissionsEnabledAndUnauthenticatedUser_ReturnsChallenge()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var pageAccess = new PageAccessPermissions
        {
            CanRead = false,
            CanEdit = false
        };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("AdminPage", Array.Empty<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        // Act
        var result = await _controller.Diff("AdminPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task Diff_WithPageLevelPermissionsAndReadAccess_ReturnsDiff()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.CanEdit).Returns(false);
        mockUser.Setup(x => x.Groups).Returns(new[] { "users" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = false
        };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("TestPage", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var fromPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# From",
            ContentHash = "hash1",
            HtmlContent = "<h1>From</h1>",
            Title = "Test Page"
        };

        var toPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# To",
            ContentHash = "hash2",
            HtmlContent = "<h1>To</h1>",
            Title = "Test Page"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "First",
                AuthorName = "Author1",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-2)
            },
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = "Second",
                AuthorName = "Author2",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Diff("TestPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        Assert.Equal("TestPage", model.PageName);
        Assert.False(model.CanEdit);
    }

    [Fact]
    public async Task Diff_WithUserWhoCanEdit_SetsCanEditToTrue()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var fromPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# From",
            ContentHash = "hash1",
            HtmlContent = "<h1>From</h1>",
            Title = "Test Page"
        };

        var toPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# To",
            ContentHash = "hash2",
            HtmlContent = "<h1>To</h1>",
            Title = "Test Page"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "First",
                AuthorName = "Author1",
                Timestamp = DateTimeOffset.UtcNow
            },
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = "Second",
                AuthorName = "Author2",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Diff("TestPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        Assert.True(model.CanEdit);
    }

    [Fact]
    public async Task Diff_WhenFromHistoryEntryNotFound_UsesPageMetadata()
    {
        // Arrange
        var fromPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# From",
            ContentHash = "hash1",
            HtmlContent = "<h1>From</h1>",
            Title = "Test Page",
            LastModifiedBy = "pageauthor1",
            LastModified = DateTimeOffset.UtcNow.AddDays(-5)
        };

        var toPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# To",
            ContentHash = "hash2",
            HtmlContent = "<h1>To</h1>",
            Title = "Test Page",
            LastModifiedBy = "pageauthor2",
            LastModified = DateTimeOffset.UtcNow.AddDays(-3)
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = "Second commit",
                AuthorName = "Author2",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-3)
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Diff("TestPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        
        Assert.Equal("pageauthor1", model.FromAuthorName);
        Assert.Equal(fromPage.LastModified, model.FromTimestamp);
        Assert.Equal("", model.FromMessage);
        Assert.Equal("Author2", model.ToAuthorName);
        Assert.Equal("Second commit", model.ToMessage);
    }

    [Fact]
    public async Task Diff_WhenToHistoryEntryNotFound_UsesPageMetadata()
    {
        // Arrange
        var fromPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# From",
            ContentHash = "hash1",
            HtmlContent = "<h1>From</h1>",
            Title = "Test Page",
            LastModifiedBy = "pageauthor1",
            LastModified = DateTimeOffset.UtcNow.AddDays(-5)
        };

        var toPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# To",
            ContentHash = "hash2",
            HtmlContent = "<h1>To</h1>",
            Title = "Test Page",
            LastModifiedBy = "pageauthor2",
            LastModified = DateTimeOffset.UtcNow.AddDays(-3)
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "First commit",
                AuthorName = "Author1",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-5)
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Diff("TestPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        
        Assert.Equal("Author1", model.FromAuthorName);
        Assert.Equal("First commit", model.FromMessage);
        Assert.Equal("pageauthor2", model.ToAuthorName);
        Assert.Equal(toPage.LastModified, model.ToTimestamp);
        Assert.Equal("", model.ToMessage);
    }

    [Fact]
    public async Task Diff_WhenBothHistoryEntriesNotFound_UsesPageMetadata()
    {
        // Arrange
        var fromPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# From",
            ContentHash = "hash1",
            HtmlContent = "<h1>From</h1>",
            Title = "Test Page",
            LastModifiedBy = "pageauthor1",
            LastModified = DateTimeOffset.UtcNow.AddDays(-5)
        };

        var toPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# To",
            ContentHash = "hash2",
            HtmlContent = "<h1>To</h1>",
            Title = "Test Page",
            LastModifiedBy = "pageauthor2",
            LastModified = DateTimeOffset.UtcNow.AddDays(-3)
        };

        var history = new List<WikiHistoryItem>();

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Diff("TestPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        
        Assert.Equal("pageauthor1", model.FromAuthorName);
        Assert.Equal(fromPage.LastModified, model.FromTimestamp);
        Assert.Equal("", model.FromMessage);
        Assert.Equal("pageauthor2", model.ToAuthorName);
        Assert.Equal(toPage.LastModified, model.ToTimestamp);
        Assert.Equal("", model.ToMessage);
    }

    [Fact]
    public async Task Diff_WhenPageMetadataIsNull_UsesDefaultValues()
    {
        // Arrange
        var fromPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# From",
            ContentHash = "hash1",
            HtmlContent = "<h1>From</h1>",
            Title = "Test Page",
            LastModifiedBy = null,
            LastModified = null
        };

        var toPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# To",
            ContentHash = "hash2",
            HtmlContent = "<h1>To</h1>",
            Title = "Test Page",
            LastModifiedBy = null,
            LastModified = null
        };

        var history = new List<WikiHistoryItem>();

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Diff("TestPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        
        Assert.Equal("Unknown", model.FromAuthorName);
        Assert.Equal(DateTimeOffset.MinValue, model.FromTimestamp);
        Assert.Equal("", model.FromMessage);
        Assert.Equal("Unknown", model.ToAuthorName);
        Assert.Equal(DateTimeOffset.MinValue, model.ToTimestamp);
        Assert.Equal("", model.ToMessage);
    }

    [Fact]
    public async Task Diff_WithNestedPagePath_ReturnsDiff()
    {
        // Arrange
        var fromPage = new WikiPage
        {
            PageName = "docs/api/reference",
            Content = "# API v1",
            ContentHash = "hash1",
            HtmlContent = "<h1>API v1</h1>",
            Title = "API Reference"
        };

        var toPage = new WikiPage
        {
            PageName = "docs/api/reference",
            Content = "# API v2",
            ContentHash = "hash2",
            HtmlContent = "<h1>API v2</h1>",
            Title = "API Reference"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "Initial",
                AuthorName = "Author",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-2)
            },
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = "Update to v2",
                AuthorName = "Author",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("docs/api/reference", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("docs/api/reference", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("docs/api/reference", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Diff("docs/api/reference", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        Assert.Equal("docs/api/reference", model.PageName);
    }

    [Fact]
    public async Task Diff_GeneratesBreadcrumb()
    {
        // Arrange
        var fromPage = new WikiPage
        {
            PageName = "docs/guide",
            Content = "# Guide v1",
            ContentHash = "hash1",
            HtmlContent = "<h1>Guide v1</h1>",
            Title = "User Guide"
        };

        var toPage = new WikiPage
        {
            PageName = "docs/guide",
            Content = "# Guide v2",
            ContentHash = "hash2",
            HtmlContent = "<h1>Guide v2</h1>",
            Title = "User Guide"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "First",
                AuthorName = "Author",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = "Second",
                AuthorName = "Author",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("docs/guide", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("docs/guide", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("docs/guide", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("docs", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Documentation");

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("docs/guide", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("User Guide");

        // Act
        var result = await _controller.Diff("docs/guide", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        
        Assert.Equal(2, model.Breadcrumb.Count);
        Assert.Equal("docs", model.Breadcrumb[0].PageName);
        Assert.Equal("Documentation", model.Breadcrumb[0].PageTitle);
        Assert.Equal("docs/guide", model.Breadcrumb[1].PageName);
        Assert.Equal("User Guide", model.Breadcrumb[1].PageTitle);
    }

    [Fact]
    public async Task Diff_GeneratesBreadcrumbWithCulture()
    {
        // Arrange
        var fromPage = new WikiPage
        {
            PageName = "docs/guide",
            Content = "# Guide v1",
            ContentHash = "hash1",
            HtmlContent = "<h1>Guide v1</h1>",
            Title = "Guide Utilisateur",
            Culture = "fr"
        };

        var toPage = new WikiPage
        {
            PageName = "docs/guide",
            Content = "# Guide v2",
            ContentHash = "hash2",
            HtmlContent = "<h1>Guide v2</h1>",
            Title = "Guide Utilisateur",
            Culture = "fr"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "Premier",
                AuthorName = "Auteur",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = "Deuxième",
                AuthorName = "Auteur",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("docs/guide", "fr", "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("docs/guide", "fr", "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("docs/guide", "fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("docs", "fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Documentation");

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("docs/guide", "fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Guide Utilisateur");

        // Act
        var result = await _controller.Diff("docs/guide", "commit1", "commit2", "fr", CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        
        Assert.Equal("fr", model.Culture);
        Assert.Equal(2, model.Breadcrumb.Count);
    }

    [Fact]
    public async Task Diff_WithLongCommitIds_ReturnsFullCommitIds()
    {
        // Arrange
        var longCommitId1 = "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0";
        var longCommitId2 = "z9y8x7w6v5u4t3s2r1q0p9o8n7m6l5k4j3i2h1g0";

        var fromPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# From",
            ContentHash = "hash1",
            HtmlContent = "<h1>From</h1>",
            Title = "Test Page"
        };

        var toPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# To",
            ContentHash = "hash2",
            HtmlContent = "<h1>To</h1>",
            Title = "Test Page"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = longCommitId1,
                Message = "First",
                AuthorName = "Author1",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new WikiHistoryItem
            {
                CommitId = longCommitId2,
                Message = "Second",
                AuthorName = "Author2",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, longCommitId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, longCommitId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Diff("TestPage", longCommitId1, longCommitId2, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        
        Assert.Equal(longCommitId1, model.FromCommitId);
        Assert.Equal(longCommitId2, model.ToCommitId);
    }

    [Fact]
    public async Task Diff_WithSpecialCharactersInMessages_PreservesMessages()
    {
        // Arrange
        var specialMessage1 = "Fix: Added <strong>bold</strong> & 'quoted' text";
        var specialMessage2 = "Update: Changed \"value\" to 100%";

        var fromPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# From",
            ContentHash = "hash1",
            HtmlContent = "<h1>From</h1>",
            Title = "Test Page"
        };

        var toPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# To",
            ContentHash = "hash2",
            HtmlContent = "<h1>To</h1>",
            Title = "Test Page"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = specialMessage1,
                AuthorName = "Author1",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = specialMessage2,
                AuthorName = "Author2",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Diff("TestPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        
        Assert.Equal(specialMessage1, model.FromMessage);
        Assert.Equal(specialMessage2, model.ToMessage);
    }

    [Fact]
    public async Task Diff_WithAnonymousUserAndAllowAnonymousViewing_ReturnsDiff()
    {
        // Arrange
        _options.AllowAnonymousViewing = true;

        var fromPage = new WikiPage
        {
            PageName = "PublicPage",
            Content = "# From",
            ContentHash = "hash1",
            HtmlContent = "<h1>From</h1>",
            Title = "Public Page"
        };

        var toPage = new WikiPage
        {
            PageName = "PublicPage",
            Content = "# To",
            ContentHash = "hash2",
            HtmlContent = "<h1>To</h1>",
            Title = "Public Page"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "First",
                AuthorName = "Author1",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = "Second",
                AuthorName = "Author2",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("PublicPage", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("PublicPage", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("PublicPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Diff("PublicPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        Assert.Equal("PublicPage", model.PageName);
        Assert.False(model.CanEdit);
    }

    [Fact]
    public async Task Diff_WithPageLevelPermissionsAndAnonymousUser_UsesEmptyGroups()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = false
        };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("PublicPage", Array.Empty<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var fromPage = new WikiPage
        {
            PageName = "PublicPage",
            Content = "# From",
            ContentHash = "hash1",
            HtmlContent = "<h1>From</h1>",
            Title = "Public Page"
        };

        var toPage = new WikiPage
        {
            PageName = "PublicPage",
            Content = "# To",
            ContentHash = "hash2",
            HtmlContent = "<h1>To</h1>",
            Title = "Public Page"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "First",
                AuthorName = "Author1",
                Timestamp = DateTimeOffset.UtcNow
            },
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = "Second",
                AuthorName = "Author2",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("PublicPage", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("PublicPage", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("PublicPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Diff("PublicPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        Assert.Equal("PublicPage", model.PageName);

        _mockAccessControlService.Verify(
            x => x.CheckPageAccessAsync("PublicPage", Array.Empty<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Diff_WithAuthenticatedUserAndNullWikiUser_AllowsIfAnonymousViewingEnabled()
    {
        // Arrange
        _options.AllowAnonymousViewing = true;

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        var fromPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# From",
            ContentHash = "hash1",
            HtmlContent = "<h1>From</h1>",
            Title = "Test Page"
        };

        var toPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# To",
            ContentHash = "hash2",
            HtmlContent = "<h1>To</h1>",
            Title = "Test Page"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "First",
                AuthorName = "Author1",
                Timestamp = DateTimeOffset.UtcNow
            },
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = "Second",
                AuthorName = "Author2",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Diff("TestPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        Assert.Equal("TestPage", model.PageName);
    }

    [Fact]
    public async Task Diff_MapsAllPropertiesToViewModel()
    {
        // Arrange
        var timestamp1 = new DateTimeOffset(2024, 1, 10, 10, 30, 0, TimeSpan.Zero);
        var timestamp2 = new DateTimeOffset(2024, 1, 15, 14, 45, 0, TimeSpan.Zero);

        var fromPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Original Content\nFirst paragraph.",
            ContentHash = "hash1",
            HtmlContent = "<h1>Original Content</h1><p>First paragraph.</p>",
            Title = "Test Page"
        };

        var toPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Updated Content\nSecond paragraph.",
            ContentHash = "hash2",
            HtmlContent = "<h1>Updated Content</h1><p>Second paragraph.</p>",
            Title = "Test Page"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "abc123",
                Message = "Initial creation",
                AuthorName = "Alice Johnson",
                Timestamp = timestamp1
            },
            new WikiHistoryItem
            {
                CommitId = "def456",
                Message = "Major update",
                AuthorName = "Bob Smith",
                Timestamp = timestamp2
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "abc123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "def456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Diff("TestPage", "abc123", "def456", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        
        Assert.Equal("TestPage", model.PageName);
        Assert.Null(model.Culture);
        Assert.Equal("abc123", model.FromCommitId);
        Assert.Equal("def456", model.ToCommitId);
        Assert.Equal("Alice Johnson", model.FromAuthorName);
        Assert.Equal("Bob Smith", model.ToAuthorName);
        Assert.Equal(timestamp1, model.FromTimestamp);
        Assert.Equal(timestamp2, model.ToTimestamp);
        Assert.Equal("Initial creation", model.FromMessage);
        Assert.Equal("Major update", model.ToMessage);
        Assert.Equal("# Original Content\nFirst paragraph.", model.FromContent);
        Assert.Equal("# Updated Content\nSecond paragraph.", model.ToContent);
        Assert.False(model.CanEdit);
    }

    [Fact]
    public async Task Diff_WithDeeplyNestedPath_GeneratesCompleteBreadcrumb()
    {
        // Arrange
        var fromPage = new WikiPage
        {
            PageName = "docs/api/v2/reference",
            Content = "# API v2.0",
            ContentHash = "hash1",
            HtmlContent = "<h1>API v2.0</h1>",
            Title = "API v2 Reference"
        };

        var toPage = new WikiPage
        {
            PageName = "docs/api/v2/reference",
            Content = "# API v2.1",
            ContentHash = "hash2",
            HtmlContent = "<h1>API v2.1</h1>",
            Title = "API v2 Reference"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "v2.0",
                AuthorName = "Author",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = "v2.1",
                AuthorName = "Author",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("docs/api/v2/reference", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("docs/api/v2/reference", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("docs/api/v2/reference", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("docs", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Documentation");

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("docs/api", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("API");

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("docs/api/v2", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Version 2");

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("docs/api/v2/reference", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("API v2 Reference");

        // Act
        var result = await _controller.Diff("docs/api/v2/reference", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        
        Assert.Equal(4, model.Breadcrumb.Count);
        Assert.Equal("docs", model.Breadcrumb[0].PageName);
        Assert.Equal("docs/api", model.Breadcrumb[1].PageName);
        Assert.Equal("docs/api/v2", model.Breadcrumb[2].PageName);
        Assert.Equal("docs/api/v2/reference", model.Breadcrumb[3].PageName);
    }

    [Fact]
    public async Task Diff_BreadcrumbUsesPageNameAsTitleWhenTitleNotFound()
    {
        // Arrange
        var fromPage = new WikiPage
        {
            PageName = "docs/guide",
            Content = "# Guide v1",
            ContentHash = "hash1",
            HtmlContent = "<h1>Guide v1</h1>",
            Title = "User Guide"
        };

        var toPage = new WikiPage
        {
            PageName = "docs/guide",
            Content = "# Guide v2",
            ContentHash = "hash2",
            HtmlContent = "<h1>Guide v2</h1>",
            Title = "User Guide"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "v1",
                AuthorName = "Author",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = "v2",
                AuthorName = "Author",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("docs/guide", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("docs/guide", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("docs/guide", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("docs", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("docs/guide", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("User Guide");

        // Act
        var result = await _controller.Diff("docs/guide", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        
        Assert.Equal(2, model.Breadcrumb.Count);
        Assert.Equal("docs", model.Breadcrumb[0].PageName);
        Assert.Equal("docs", model.Breadcrumb[0].PageTitle);
    }

    [Fact]
    public async Task Diff_WithSingleSegmentPage_GeneratesSingleItemBreadcrumb()
    {
        // Arrange
        var fromPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# From",
            ContentHash = "hash1",
            HtmlContent = "<h1>From</h1>",
            Title = "Test Page"
        };

        var toPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# To",
            ContentHash = "hash2",
            HtmlContent = "<h1>To</h1>",
            Title = "Test Page"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "First",
                AuthorName = "Author",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = "Second",
                AuthorName = "Author",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test Page");

        // Act
        var result = await _controller.Diff("TestPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        
        Assert.Single(model.Breadcrumb);
        Assert.Equal("TestPage", model.Breadcrumb[0].PageName);
        Assert.Equal("Test Page", model.Breadcrumb[0].PageTitle);
    }

    [Fact]
    public async Task Diff_WithAuthenticatedUserAndCanView_ReturnsDiff()
    {
        // Arrange
        _options.AllowAnonymousViewing = false;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.CanEdit).Returns(false);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var fromPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# From",
            ContentHash = "hash1",
            HtmlContent = "<h1>From</h1>",
            Title = "Test Page"
        };

        var toPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# To",
            ContentHash = "hash2",
            HtmlContent = "<h1>To</h1>",
            Title = "Test Page"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "First",
                AuthorName = "Author1",
                Timestamp = DateTimeOffset.UtcNow
            },
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = "Second",
                AuthorName = "Author2",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Diff("TestPage", "commit1", "commit2", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        Assert.Equal("TestPage", model.PageName);
        Assert.False(model.CanEdit);
    }

    [Fact]
    public async Task Diff_WithCultureAndPageLevelPermissions_ChecksPermissions()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "translators" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = false
        };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("TestPage", new[] { "translators" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var fromPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Contenu Original",
            ContentHash = "hash1",
            HtmlContent = "<h1>Contenu Original</h1>",
            Title = "Page Test",
            Culture = "fr"
        };

        var toPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Contenu Modifié",
            ContentHash = "hash2",
            HtmlContent = "<h1>Contenu Modifié</h1>",
            Title = "Page Test",
            Culture = "fr"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit1",
                Message = "Initial",
                AuthorName = "Translator1",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new WikiHistoryItem
            {
                CommitId = "commit2",
                Message = "Update",
                AuthorName = "Translator2",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", "fr", "commit1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromPage);

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", "fr", "commit2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(toPage);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", "fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Diff("TestPage", "commit1", "commit2", "fr", CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageDiffViewModel>(viewResult.Model);
        Assert.Equal("TestPage", model.PageName);
        Assert.Equal("fr", model.Culture);

        // Note: Permission check is on the page name, not the culture-specific version
        _mockAccessControlService.Verify(
            x => x.CheckPageAccessAsync("TestPage", new[] { "translators" }, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
