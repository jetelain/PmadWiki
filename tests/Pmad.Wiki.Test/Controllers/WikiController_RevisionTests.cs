using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pmad.Wiki.Models;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Controllers;

public class WikiController_RevisionTests : WikiControllerTestBase
{
    #region Revision Action Tests

    [Fact]
    public async Task Revision_WithValidPageNameAndCommitId_ReturnsViewWithRevision()
    {
        // Arrange
        var page = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Test Content",
            ContentHash = "hash123",
            HtmlContent = "<h1>Test Content</h1>",
            Title = "Test Page",
            LastModifiedBy = "testuser",
            LastModified = DateTimeOffset.UtcNow
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit123",
                Message = "Initial commit",
                AuthorName = "John Doe",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Revision("TestPage", "commit123", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        
        Assert.Equal("TestPage", model.PageName);
        Assert.Equal("<h1>Test Content</h1>", model.HtmlContent);
        Assert.Equal("Test Page", model.Title);
        Assert.Null(model.Culture);
        Assert.Equal("commit123", model.CommitId);
        Assert.Equal("John Doe", model.AuthorName);
        Assert.Equal("Initial commit", model.Message);
        Assert.False(model.CanEdit);
    }

    [Fact]
    public async Task Revision_WithEmptyPageName_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Revision("", "commit123", null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Page name is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task Revision_WithNullPageName_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Revision(null!, "commit123", null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Page name is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task Revision_WithEmptyCommitId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Revision("TestPage", "", null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Commit ID is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task Revision_WithNullCommitId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Revision("TestPage", null!, null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Commit ID is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task Revision_WithInvalidPageName_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Revision("../../../etc/passwd", "commit123", null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid page name.", badRequestResult.Value);
    }

    [Fact]
    public async Task Revision_WithInvalidCulture_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Revision("TestPage", "commit123", "invalid-culture-code", CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid culture identifier.", badRequestResult.Value);
    }

    [Fact]
    public async Task Revision_WhenAnonymousViewingDisabledAndUserNotAuthenticated_ReturnsChallenge()
    {
        // Arrange
        _options.AllowAnonymousViewing = false;

        // Act
        var result = await _controller.Revision("TestPage", "commit123", null, CancellationToken.None);

        // Assert
        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task Revision_WhenUserAuthenticatedButCannotView_ReturnsForbid()
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
        var result = await _controller.Revision("TestPage", "commit123", null, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Revision_WhenPageRevisionNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("NonExistent", null, "commit123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WikiPage?)null);

        // Act
        var result = await _controller.Revision("NonExistent", "commit123", null, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Revision_WithCulture_ReturnsRevisionInSpecificCulture()
    {
        // Arrange
        var page = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Contenu Test",
            ContentHash = "hash123",
            HtmlContent = "<h1>Contenu Test</h1>",
            Title = "Page Test",
            Culture = "fr",
            LastModifiedBy = "testuser",
            LastModified = DateTimeOffset.UtcNow.AddDays(-2)
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit456",
                Message = "Création initiale",
                AuthorName = "Jean Dupont",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-2)
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", "fr", "commit456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", "fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Revision("TestPage", "commit456", "fr", CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        
        Assert.Equal("TestPage", model.PageName);
        Assert.Equal("fr", model.Culture);
        Assert.Equal("Page Test", model.Title);
        Assert.Equal("commit456", model.CommitId);
        Assert.Equal("Jean Dupont", model.AuthorName);
        Assert.Equal("Création initiale", model.Message);
    }

    [Fact]
    public async Task Revision_WithPageLevelPermissionsEnabledAndNoReadAccess_ReturnsForbid()
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

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("AdminPage", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Revision("AdminPage", "commit123", null, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Revision_WithPageLevelPermissionsEnabledAndUnauthenticatedUser_ReturnsChallenge()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var pageAccess = new PageAccessPermissions
        {
            CanRead = false,
            CanEdit = false
        };

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("AdminPage", Array.Empty<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        // Act
        var result = await _controller.Revision("AdminPage", "commit123", null, CancellationToken.None);

        // Assert
        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task Revision_WithPageLevelPermissionsAndReadAccess_ReturnsRevision()
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

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("TestPage", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var page = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Test",
            ContentHash = "hash123",
            HtmlContent = "<h1>Test</h1>",
            Title = "Test Page"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit123",
                Message = "Test",
                AuthorName = "Test User",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Revision("TestPage", "commit123", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        Assert.Equal("TestPage", model.PageName);
        Assert.Equal("commit123", model.CommitId);
        Assert.False(model.CanEdit);
    }

    [Fact]
    public async Task Revision_WithUserWhoCanEdit_SetsCanEditToTrue()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var page = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Test",
            ContentHash = "hash123",
            HtmlContent = "<h1>Test</h1>",
            Title = "Test Page"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit123",
                Message = "Test",
                AuthorName = "Test User",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Revision("TestPage", "commit123", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        Assert.True(model.CanEdit);
    }

    [Fact]
    public async Task Revision_WhenHistoryEntryNotFound_UsesPageMetadata()
    {
        // Arrange
        var page = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Test",
            ContentHash = "hash123",
            HtmlContent = "<h1>Test</h1>",
            Title = "Test Page",
            LastModifiedBy = "pageauthor",
            LastModified = DateTimeOffset.UtcNow.AddDays(-5)
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "differentcommit",
                Message = "Different commit",
                AuthorName = "Other User",
                Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit999", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Revision("TestPage", "commit999", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        
        Assert.Equal("commit999", model.CommitId);
        Assert.Equal("pageauthor", model.AuthorName);
        Assert.Equal(page.LastModified, model.Timestamp);
        Assert.Equal("", model.Message);
    }

    [Fact]
    public async Task Revision_WhenPageMetadataIsNull_UsesDefaultValues()
    {
        // Arrange
        var page = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Test",
            ContentHash = "hash123",
            HtmlContent = "<h1>Test</h1>",
            Title = "Test Page",
            LastModifiedBy = null,
            LastModified = null
        };

        var history = new List<WikiHistoryItem>();

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit999", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Revision("TestPage", "commit999", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        
        Assert.Equal("commit999", model.CommitId);
        Assert.Equal("Unknown", model.AuthorName);
        Assert.Equal(DateTimeOffset.MinValue, model.Timestamp);
        Assert.Equal("", model.Message);
    }

    [Fact]
    public async Task Revision_WithNestedPagePath_ReturnsRevision()
    {
        // Arrange
        var page = new WikiPage
        {
            PageName = "docs/api/reference",
            Content = "# API Reference",
            ContentHash = "hash123",
            HtmlContent = "<h1>API Reference</h1>",
            Title = "API Reference"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit789",
                Message = "Initial version",
                AuthorName = "Author",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("docs/api/reference", null, "commit789", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("docs/api/reference", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Revision("docs/api/reference", "commit789", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        Assert.Equal("docs/api/reference", model.PageName);
        Assert.Equal("commit789", model.CommitId);
    }

    [Fact]
    public async Task Revision_GeneratesBreadcrumb()
    {
        // Arrange
        var page = new WikiPage
        {
            PageName = "docs/guide",
            Content = "# User Guide",
            ContentHash = "hash123",
            HtmlContent = "<h1>User Guide</h1>",
            Title = "User Guide"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit123",
                Message = "Update",
                AuthorName = "Author",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("docs/guide", null, "commit123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

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
        var result = await _controller.Revision("docs/guide", "commit123", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        
        Assert.Equal(2, model.Breadcrumb.Count);
        Assert.Equal("docs", model.Breadcrumb[0].PageName);
        Assert.Equal("Documentation", model.Breadcrumb[0].PageTitle);
        Assert.Equal("docs/guide", model.Breadcrumb[1].PageName);
        Assert.Equal("User Guide", model.Breadcrumb[1].PageTitle);
    }

    [Fact]
    public async Task Revision_GeneratesBreadcrumbWithCulture()
    {
        // Arrange
        var page = new WikiPage
        {
            PageName = "docs/guide",
            Content = "# Guide Utilisateur",
            ContentHash = "hash123",
            HtmlContent = "<h1>Guide Utilisateur</h1>",
            Title = "Guide Utilisateur",
            Culture = "fr"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit123",
                Message = "Mise à jour",
                AuthorName = "Auteur",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("docs/guide", "fr", "commit123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

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
        var result = await _controller.Revision("docs/guide", "commit123", "fr", CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        
        Assert.Equal("fr", model.Culture);
        Assert.Equal(2, model.Breadcrumb.Count);
        Assert.Equal("docs", model.Breadcrumb[0].PageName);
        Assert.Equal("docs/guide", model.Breadcrumb[1].PageName);
    }

    [Fact]
    public async Task Revision_WithLongCommitId_ReturnsFullCommitId()
    {
        // Arrange
        var longCommitId = "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0";
        var page = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Test",
            ContentHash = "hash123",
            HtmlContent = "<h1>Test</h1>",
            Title = "Test Page"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = longCommitId,
                Message = "Test commit",
                AuthorName = "Test Author",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, longCommitId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Revision("TestPage", longCommitId, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        
        Assert.Equal(longCommitId, model.CommitId);
    }

    [Fact]
    public async Task Revision_WithSpecialCharactersInMessage_PreservesMessage()
    {
        // Arrange
        var specialMessage = "Fix: Added <strong>bold</strong> & 'quoted' text with \"quotes\"";
        var page = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Test",
            ContentHash = "hash123",
            HtmlContent = "<h1>Test</h1>",
            Title = "Test Page"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit123",
                Message = specialMessage,
                AuthorName = "Test Author",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Revision("TestPage", "commit123", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        
        Assert.Equal(specialMessage, model.Message);
    }

    [Fact]
    public async Task Revision_WithAnonymousUserAndAllowAnonymousViewing_ReturnsRevision()
    {
        // Arrange
        _options.AllowAnonymousViewing = true;

        var page = new WikiPage
        {
            PageName = "PublicPage",
            Content = "# Public",
            ContentHash = "hash123",
            HtmlContent = "<h1>Public</h1>",
            Title = "Public Page"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit123",
                Message = "Public commit",
                AuthorName = "Public Author",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("PublicPage", null, "commit123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("PublicPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Revision("PublicPage", "commit123", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        Assert.Equal("PublicPage", model.PageName);
        Assert.Equal("commit123", model.CommitId);
        Assert.False(model.CanEdit);
    }

    [Fact]
    public async Task Revision_WithPageLevelPermissionsAndAnonymousUser_UsesEmptyGroups()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = false
        };

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("PublicPage", Array.Empty<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var page = new WikiPage
        {
            PageName = "PublicPage",
            Content = "# Public",
            ContentHash = "hash123",
            HtmlContent = "<h1>Public</h1>",
            Title = "Public Page"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit123",
                Message = "Test",
                AuthorName = "Test User",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("PublicPage", null, "commit123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("PublicPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Revision("PublicPage", "commit123", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        Assert.Equal("PublicPage", model.PageName);

        _mockPageService.Verify(
            x => x.CheckPageAccessAsync("PublicPage", Array.Empty<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Revision_WithAuthenticatedUserAndNullWikiUser_AllowsIfAnonymousViewingEnabled()
    {
        // Arrange
        _options.AllowAnonymousViewing = true;

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        var page = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Test",
            ContentHash = "hash123",
            HtmlContent = "<h1>Test</h1>",
            Title = "Test Page"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit123",
                Message = "Test",
                AuthorName = "Test User",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Revision("TestPage", "commit123", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        Assert.Equal("TestPage", model.PageName);
    }

    [Fact]
    public async Task Revision_MapsAllPropertiesToViewModel()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var page = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Test Content",
            ContentHash = "hash123",
            HtmlContent = "<h1>Test Content</h1>",
            Title = "Test Page Title"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "abc123def456",
                Message = "Complete message text",
                AuthorName = "Full Author Name",
                Timestamp = timestamp
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "abc123def456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Revision("TestPage", "abc123def456", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        
        Assert.Equal("TestPage", model.PageName);
        Assert.Equal("<h1>Test Content</h1>", model.HtmlContent);
        Assert.Equal("Test Page Title", model.Title);
        Assert.Null(model.Culture);
        Assert.Equal("abc123def456", model.CommitId);
        Assert.Equal("Full Author Name", model.AuthorName);
        Assert.Equal(timestamp, model.Timestamp);
        Assert.Equal("Complete message text", model.Message);
        Assert.False(model.CanEdit);
    }

    [Fact]
    public async Task Revision_WithDeeplyNestedPath_GeneratesCompleteBreadcrumb()
    {
        // Arrange
        var page = new WikiPage
        {
            PageName = "docs/api/v2/reference",
            Content = "# API v2 Reference",
            ContentHash = "hash123",
            HtmlContent = "<h1>API v2 Reference</h1>",
            Title = "API v2 Reference"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit123",
                Message = "Update",
                AuthorName = "Author",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("docs/api/v2/reference", null, "commit123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

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
        var result = await _controller.Revision("docs/api/v2/reference", "commit123", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        
        Assert.Equal(4, model.Breadcrumb.Count);
        Assert.Equal("docs", model.Breadcrumb[0].PageName);
        Assert.Equal("docs/api", model.Breadcrumb[1].PageName);
        Assert.Equal("docs/api/v2", model.Breadcrumb[2].PageName);
        Assert.Equal("docs/api/v2/reference", model.Breadcrumb[3].PageName);
    }

    [Fact]
    public async Task Revision_BreadcrumbUsesPageNameAsTitleWhenTitleNotFound()
    {
        // Arrange
        var page = new WikiPage
        {
            PageName = "docs/guide",
            Content = "# User Guide",
            ContentHash = "hash123",
            HtmlContent = "<h1>User Guide</h1>",
            Title = "User Guide"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit123",
                Message = "Update",
                AuthorName = "Author",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("docs/guide", null, "commit123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

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
        var result = await _controller.Revision("docs/guide", "commit123", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        
        Assert.Equal(2, model.Breadcrumb.Count);
        Assert.Equal("docs", model.Breadcrumb[0].PageName);
        Assert.Equal("docs", model.Breadcrumb[0].PageTitle);
    }

    [Fact]
    public async Task Revision_WithSingleSegmentPage_GeneratesSingleItemBreadcrumb()
    {
        // Arrange
        var page = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Test",
            ContentHash = "hash123",
            HtmlContent = "<h1>Test</h1>",
            Title = "Test Page"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit123",
                Message = "Update",
                AuthorName = "Author",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test Page");

        // Act
        var result = await _controller.Revision("TestPage", "commit123", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        
        Assert.Single(model.Breadcrumb);
        Assert.Equal("TestPage", model.Breadcrumb[0].PageName);
        Assert.Equal("Test Page", model.Breadcrumb[0].PageTitle);
    }

    [Fact]
    public async Task Revision_WithAuthenticatedUserAndCanView_ReturnsRevision()
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

        var page = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Test",
            ContentHash = "hash123",
            HtmlContent = "<h1>Test</h1>",
            Title = "Test Page"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit123",
                Message = "Test",
                AuthorName = "Test User",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Revision("TestPage", "commit123", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        Assert.Equal("TestPage", model.PageName);
        Assert.False(model.CanEdit);
    }

    [Fact]
    public async Task Revision_WithEmptyHistory_UsesPageMetadata()
    {
        // Arrange
        var page = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Test",
            ContentHash = "hash123",
            HtmlContent = "<h1>Test</h1>",
            Title = "Test Page",
            LastModifiedBy = "testauthor",
            LastModified = DateTimeOffset.UtcNow.AddDays(-3)
        };

        var history = new List<WikiHistoryItem>();

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, "commit123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _controller.Revision("TestPage", "commit123", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        
        Assert.Equal("commit123", model.CommitId);
        Assert.Equal("testauthor", model.AuthorName);
        Assert.Equal(page.LastModified, model.Timestamp);
        Assert.Equal("", model.Message);
    }

    [Fact]
    public async Task Revision_WithCultureAndPageLevelPermissions_ChecksPermissions()
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

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("TestPage", new[] { "translators" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var page = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Test",
            ContentHash = "hash123",
            HtmlContent = "<h1>Test</h1>",
            Title = "Test Page",
            Culture = "fr"
        };

        var history = new List<WikiHistoryItem>
        {
            new WikiHistoryItem
            {
                CommitId = "commit123",
                Message = "Traduction initiale",
                AuthorName = "Translator",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", "fr", "commit123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetPageHistoryAsync("TestPage", "fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Revision("TestPage", "commit123", "fr", CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageRevisionViewModel>(viewResult.Model);
        Assert.Equal("TestPage", model.PageName);
        Assert.Equal("fr", model.Culture);
        Assert.Equal("commit123", model.CommitId);

        // Note: Permission check is on the page name, not the culture-specific version
        _mockPageService.Verify(
            x => x.CheckPageAccessAsync("TestPage", new[] { "translators" }, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
