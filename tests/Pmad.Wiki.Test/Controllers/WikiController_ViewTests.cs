using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pmad.Wiki.Models;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Controllers;

public class WikiController_ViewTests : WikiControllerTestBase
{
    #region View Action Tests

    [Fact]
    public async Task View_WithExistingPage_ReturnsViewWithPage()
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

        _mockPageService
            .Setup(x => x.GetPageAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("TestPage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "en", "fr" });

        // Act
        var result = await _controller.View("TestPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        
        Assert.Equal("TestPage", model.PageName);
        Assert.Equal("<h1>Test Content</h1>", model.HtmlContent);
        Assert.Equal("Test Page", model.Title);
        Assert.False(model.CanEdit);
        Assert.Null(model.Culture);
        Assert.Equal(2, model.AvailableCultures.Count);
        Assert.Equal("testuser", model.LastModifiedBy);
        Assert.NotNull(model.LastModified);
    }

    [Fact]
    public async Task View_WithEmptyId_UsesHomePageName()
    {
        // Arrange
        var page = new WikiPage
        {
            PageName = "Home",
            Content = "# Home",
            ContentHash = "hash123",
            HtmlContent = "<h1>Home</h1>",
            Title = "Home Page"
        };

        _mockPageService
            .Setup(x => x.GetPageAsync("Home", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("Home", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _controller.View("", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.Equal("Home", model.PageName);
    }

    [Fact]
    public async Task View_WithNullId_UsesHomePageName()
    {
        // Arrange
        var page = new WikiPage
        {
            PageName = "Home",
            Content = "# Home",
            ContentHash = "hash123",
            HtmlContent = "<h1>Home</h1>",
            Title = "Home Page"
        };

        _mockPageService
            .Setup(x => x.GetPageAsync("Home", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("Home", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _controller.View(null!, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.Equal("Home", model.PageName);
    }

    [Fact]
    public async Task View_WithInvalidPageName_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.View("../../../etc/passwd", null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid page name.", badRequestResult.Value);
    }

    [Fact]
    public async Task View_WithInvalidCulture_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.View("TestPage", "invalid-culture-code", CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid culture identifier.", badRequestResult.Value);
    }

    [Fact]
    public async Task View_WhenAnonymousViewingDisabledAndUserNotAuthenticated_ReturnsChallenge()
    {
        // Arrange
        _options.AllowAnonymousViewing = false;

        // Act
        var result = await _controller.View("TestPage", null, CancellationToken.None);

        // Assert
        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task View_WhenUserAuthenticatedButCannotView_ReturnsForbid()
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
        var result = await _controller.View("TestPage", null, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task View_WithAuthenticatedUserAndCanView_ReturnsPage()
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

        _mockPageService
            .Setup(x => x.GetPageAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("TestPage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.View("TestPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.Equal("TestPage", model.PageName);
        Assert.False(model.CanEdit);
    }

    [Fact]
    public async Task View_WithPageLevelPermissionsEnabledAndNoReadAccess_ReturnsForbid()
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
        var result = await _controller.View("AdminPage", null, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task View_WithPageLevelPermissionsEnabledAndUnauthenticatedUser_ReturnsChallenge()
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
        var result = await _controller.View("AdminPage", null, CancellationToken.None);

        // Assert
        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task View_WithPageLevelPermissionsAndReadAccess_ReturnsPage()
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

        _mockPageService
            .Setup(x => x.GetPageAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("TestPage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.View("TestPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.Equal("TestPage", model.PageName);
        Assert.False(model.CanEdit);
    }

    [Fact]
    public async Task View_WhenPageDoesNotExistAndUserCanEdit_RedirectsToEdit()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.GetPageAsync("NewPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WikiPage?)null);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.View("NewPage", null, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirectResult.ActionName);
        Assert.Equal("NewPage", redirectResult.RouteValues?["id"]);
    }

    [Fact]
    public async Task View_WhenPageDoesNotExistAndUserCannotEdit_ReturnsNotFound()
    {
        // Arrange
        _mockPageService
            .Setup(x => x.GetPageAsync("NonExistent", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WikiPage?)null);

        // Act
        var result = await _controller.View("NonExistent", null, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task View_WhenPageDoesNotExistAndUserCanEditWithPageLevelPermissions_ChecksEditPermission()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "editors" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.GetPageAsync("NewPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WikiPage?)null);

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = true
        };

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("NewPage", new[] { "editors" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.View("NewPage", null, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirectResult.ActionName);
        Assert.Equal("NewPage", redirectResult.RouteValues?["id"]);

        // Verify CheckPageAccessAsync was called twice (once for read, once for edit)
        _mockPageService.Verify(
            x => x.CheckPageAccessAsync("NewPage", new[] { "editors" }, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task View_WhenPageDoesNotExistAndUserCanEditButNoEditPermission_ReturnsNotFound()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "users" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.GetPageAsync("RestrictedPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WikiPage?)null);

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = false
        };

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("RestrictedPage", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.View("RestrictedPage", null, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task View_WithCulture_ReturnsPageInSpecificCulture()
    {
        // Arrange
        var page = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Contenu Test",
            ContentHash = "hash123",
            HtmlContent = "<h1>Contenu Test</h1>",
            Title = "Page Test",
            Culture = "fr"
        };

        _mockPageService
            .Setup(x => x.GetPageAsync("TestPage", "fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("TestPage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "en", "fr", "de" });

        // Act
        var result = await _controller.View("TestPage", "fr", CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        
        Assert.Equal("TestPage", model.PageName);
        Assert.Equal("fr", model.Culture);
        Assert.Equal("Page Test", model.Title);
        Assert.Equal(3, model.AvailableCultures.Count);
    }

    [Fact]
    public async Task View_WithUserWhoCanEdit_SetsCanEditToTrue()
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

        _mockPageService
            .Setup(x => x.GetPageAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("TestPage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.View("TestPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.True(model.CanEdit);
    }

    [Fact]
    public async Task View_WithPageLevelPermissionsAndEditAccess_SetsCanEditToTrue()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "editors" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = true
        };

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("TestPage", new[] { "editors" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var page = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Test",
            ContentHash = "hash123",
            HtmlContent = "<h1>Test</h1>",
            Title = "Test Page"
        };

        _mockPageService
            .Setup(x => x.GetPageAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("TestPage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.View("TestPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.True(model.CanEdit);
    }

    [Fact]
    public async Task View_WithPageLevelPermissionsAndNoEditAccess_SetsCanEditToFalse()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.CanEdit).Returns(true);
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
            .Setup(x => x.CheckPageAccessAsync("RestrictedPage", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var page = new WikiPage
        {
            PageName = "RestrictedPage",
            Content = "# Restricted",
            ContentHash = "hash123",
            HtmlContent = "<h1>Restricted</h1>",
            Title = "Restricted Page"
        };

        _mockPageService
            .Setup(x => x.GetPageAsync("RestrictedPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("RestrictedPage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.View("RestrictedPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.False(model.CanEdit);
    }

    [Fact]
    public async Task View_WithNestedPagePath_ReturnsView()
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

        _mockPageService
            .Setup(x => x.GetPageAsync("docs/api/reference", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("docs/api/reference", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _controller.View("docs/api/reference", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.Equal("docs/api/reference", model.PageName);
    }

    [Fact]
    public async Task View_GeneratesBreadcrumb()
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

        _mockPageService
            .Setup(x => x.GetPageAsync("docs/guide", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("docs/guide", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("docs", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Documentation");

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("docs/guide", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("User Guide");

        // Act
        var result = await _controller.View("docs/guide", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        
        Assert.Equal(2, model.Breadcrumb.Count);
        Assert.Equal("docs", model.Breadcrumb[0].PageName);
        Assert.Equal("Documentation", model.Breadcrumb[0].PageTitle);
        Assert.Equal("docs/guide", model.Breadcrumb[1].PageName);
        Assert.Equal("User Guide", model.Breadcrumb[1].PageTitle);
    }

    [Fact]
    public async Task View_GeneratesBreadcrumbWithCulture()
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

        _mockPageService
            .Setup(x => x.GetPageAsync("docs/guide", "fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("docs/guide", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "en", "fr" });

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("docs", "fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Documentation");

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("docs/guide", "fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Guide Utilisateur");

        // Act
        var result = await _controller.View("docs/guide", "fr", CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        
        Assert.Equal("fr", model.Culture);
        Assert.Equal(2, model.Breadcrumb.Count);
        Assert.Equal("docs", model.Breadcrumb[0].PageName);
        Assert.Equal("docs/guide", model.Breadcrumb[1].PageName);
    }

    [Fact]
    public async Task View_WithDeeplyNestedPath_GeneratesCompleteBreadcrumb()
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

        _mockPageService
            .Setup(x => x.GetPageAsync("docs/api/v2/reference", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("docs/api/v2/reference", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

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
        var result = await _controller.View("docs/api/v2/reference", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        
        Assert.Equal(4, model.Breadcrumb.Count);
        Assert.Equal("docs", model.Breadcrumb[0].PageName);
        Assert.Equal("docs/api", model.Breadcrumb[1].PageName);
        Assert.Equal("docs/api/v2", model.Breadcrumb[2].PageName);
        Assert.Equal("docs/api/v2/reference", model.Breadcrumb[3].PageName);
    }

    [Fact]
    public async Task View_BreadcrumbUsesPageNameAsTitleWhenTitleNotFound()
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

        _mockPageService
            .Setup(x => x.GetPageAsync("docs/guide", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("docs/guide", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("docs", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("docs/guide", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("User Guide");

        // Act
        var result = await _controller.View("docs/guide", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        
        Assert.Equal(2, model.Breadcrumb.Count);
        Assert.Equal("docs", model.Breadcrumb[0].PageName);
        Assert.Equal("docs", model.Breadcrumb[0].PageTitle);
    }

    [Fact]
    public async Task View_WithEmptyAvailableCultures_ReturnsEmptyList()
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

        _mockPageService
            .Setup(x => x.GetPageAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("TestPage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _controller.View("TestPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.Empty(model.AvailableCultures);
    }

    [Fact]
    public async Task View_WithMultipleAvailableCultures_ReturnsAllCultures()
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

        _mockPageService
            .Setup(x => x.GetPageAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("TestPage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "en", "fr", "de", "es", "it" });

        // Act
        var result = await _controller.View("TestPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.Equal(5, model.AvailableCultures.Count);
        Assert.Contains("en", model.AvailableCultures);
        Assert.Contains("fr", model.AvailableCultures);
        Assert.Contains("de", model.AvailableCultures);
        Assert.Contains("es", model.AvailableCultures);
        Assert.Contains("it", model.AvailableCultures);
    }

    [Fact]
    public async Task View_WithNullLastModifiedBy_ReturnsNull()
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

        _mockPageService
            .Setup(x => x.GetPageAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("TestPage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _controller.View("TestPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.Null(model.LastModifiedBy);
        Assert.Null(model.LastModified);
    }

    [Fact]
    public async Task View_WithAnonymousUserAndAllowAnonymousViewing_ReturnsPage()
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

        _mockPageService
            .Setup(x => x.GetPageAsync("PublicPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("PublicPage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _controller.View("PublicPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.Equal("PublicPage", model.PageName);
        Assert.False(model.CanEdit);
    }

    [Fact]
    public async Task View_WithAuthenticatedUserAndNullWikiUser_DoesNotForbid()
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

        _mockPageService
            .Setup(x => x.GetPageAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("TestPage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.View("TestPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.Equal("TestPage", model.PageName);
    }

    [Fact]
    public async Task View_WithPageLevelPermissionsAndAnonymousUser_UsesEmptyGroups()
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

        _mockPageService
            .Setup(x => x.GetPageAsync("PublicPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("PublicPage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _controller.View("PublicPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        Assert.Equal("PublicPage", model.PageName);

        _mockPageService.Verify(
            x => x.CheckPageAccessAsync("PublicPage", Array.Empty<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task View_WithSingleSegmentPage_GeneratesSingleItemBreadcrumb()
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

        _mockPageService
            .Setup(x => x.GetPageAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetAvailableCulturesForPageAsync("TestPage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test Page");

        // Act
        var result = await _controller.View("TestPage", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageViewModel>(viewResult.Model);
        
        Assert.Single(model.Breadcrumb);
        Assert.Equal("TestPage", model.Breadcrumb[0].PageName);
        Assert.Equal("Test Page", model.Breadcrumb[0].PageTitle);
    }

    [Fact]
    public async Task View_WithRedirectToEditForNewPage_PassesCulture()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.GetPageAsync("NewPage", "de", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WikiPage?)null);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.View("NewPage", "de", CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirectResult.ActionName);
        Assert.Equal("NewPage", redirectResult.RouteValues?["id"]);
        Assert.Equal("de", redirectResult.RouteValues?["culture"]);
    }

    #endregion
}
