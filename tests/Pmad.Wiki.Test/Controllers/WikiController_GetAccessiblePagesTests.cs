using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pmad.Wiki.Controllers;
using Pmad.Wiki.Models;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Controllers;

public class WikiController_GetAccessiblePagesTests : WikiControllerTestBase
{
    [Fact]
    public async Task GetAccessiblePages_WithValidUser_ReturnsPartialViewWithPages()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "Home",
                Title = "Home Page"
            },
            new WikiPageInfo
            {
                PageName = "About",
                Title = "About Us"
            },
            new WikiPageInfo
            {
                PageName = "docs/guide",
                Title = "User Guide"
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetAccessiblePages("Home", CancellationToken.None);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_PageLinkList", partialViewResult.ViewName);
        
        var model = Assert.IsType<List<WikiPageLinkInfo>>(partialViewResult.Model);
        Assert.Equal(3, model.Count);
        Assert.Equal("About", model[0].PageName);
        Assert.Equal("docs/guide", model[1].PageName);
        Assert.Equal("Home", model[2].PageName);
    }

    [Fact]
    public async Task GetAccessiblePages_OrdersPagesByPageName()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Zebra", Title = "Z Page" },
            new WikiPageInfo { PageName = "Alpha", Title = "A Page" },
            new WikiPageInfo { PageName = "Gamma", Title = "G Page" },
            new WikiPageInfo { PageName = "Beta", Title = "B Page" }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetAccessiblePages("Home", CancellationToken.None);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<List<WikiPageLinkInfo>>(partialViewResult.Model);
        
        Assert.Equal(4, model.Count);
        Assert.Equal("Alpha", model[0].PageName);
        Assert.Equal("Beta", model[1].PageName);
        Assert.Equal("Gamma", model[2].PageName);
        Assert.Equal("Zebra", model[3].PageName);
    }

    [Fact]
    public async Task GetAccessiblePages_CalculatesRelativePaths()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Home", Title = "Home Page" },
            new WikiPageInfo { PageName = "docs/guide", Title = "User Guide" },
            new WikiPageInfo { PageName = "docs/api/reference", Title = "API Reference" }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetAccessiblePages("docs/guide", CancellationToken.None);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<List<WikiPageLinkInfo>>(partialViewResult.Model);
        
        Assert.Equal(3, model.Count);
        
        var guidePage = model.First(p => p.PageName == "docs/guide");
        Assert.Equal("guide", guidePage.RelativePath);
        
        var homePage = model.First(p => p.PageName == "Home");
        Assert.Equal("../Home", homePage.RelativePath);
        
        var apiPage = model.First(p => p.PageName == "docs/api/reference");
        Assert.Equal("api/reference", apiPage.RelativePath);
    }

    [Fact]
    public async Task GetAccessiblePages_IncludesTitleInformation()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Home", Title = "Welcome Home" },
            new WikiPageInfo { PageName = "About", Title = null }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetAccessiblePages("Home", CancellationToken.None);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<List<WikiPageLinkInfo>>(partialViewResult.Model);
        
        var homePage = model.First(p => p.PageName == "Home");
        Assert.Equal("Welcome Home", homePage.Title);
        
        var aboutPage = model.First(p => p.PageName == "About");
        Assert.Null(aboutPage.Title);
    }

    [Fact]
    public async Task GetAccessiblePages_WhenUserNotAuthenticated_ReturnsForbid()
    {
        // Arrange
        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        // Act
        var result = await _controller.GetAccessiblePages("Home", CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetAccessiblePages_WhenUserCannotEdit_ReturnsForbid()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(false);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetAccessiblePages("Home", CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetAccessiblePages_WithInvalidPageName_ReturnsBadRequest()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetAccessiblePages("../../../etc/passwd", CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetAccessiblePages_WithPageLevelPermissionsEnabled_FiltersPages()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "users" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Home", Title = "Home Page" },
            new WikiPageInfo { PageName = "admin/settings", Title = "Admin Settings" },
            new WikiPageInfo { PageName = "About", Title = "About Us" }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        // Setup permissions: users can read Home and About, but not admin pages
        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("Home", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = true });

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("About", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = true });

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("admin/settings", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = false, CanEdit = false });

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetAccessiblePages("Home", CancellationToken.None);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<List<WikiPageLinkInfo>>(partialViewResult.Model);
        
        Assert.Equal(2, model.Count);
        Assert.Contains(model, p => p.PageName == "Home");
        Assert.Contains(model, p => p.PageName == "About");
        Assert.DoesNotContain(model, p => p.PageName == "admin/settings");
    }

    [Fact]
    public async Task GetAccessiblePages_WithPageLevelPermissionsDisabled_ReturnsAllPages()
    {
        // Arrange
        _options.UsePageLevelPermissions = false;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "users" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Home", Title = "Home Page" },
            new WikiPageInfo { PageName = "admin/settings", Title = "Admin Settings" },
            new WikiPageInfo { PageName = "About", Title = "About Us" }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetAccessiblePages("Home", CancellationToken.None);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<List<WikiPageLinkInfo>>(partialViewResult.Model);
        
        Assert.Equal(3, model.Count);
        Assert.Contains(model, p => p.PageName == "Home");
        Assert.Contains(model, p => p.PageName == "About");
        Assert.Contains(model, p => p.PageName == "admin/settings");
        
        // Verify CheckPageAccessAsync was never called when permissions are disabled
        _mockPageService.Verify(
            x => x.CheckPageAccessAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAccessiblePages_WithEmptyUserGroups_FiltersBasedOnEmptyGroups()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "public", Title = "Public Page" },
            new WikiPageInfo { PageName = "private", Title = "Private Page" }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("public", Array.Empty<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = true });

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("private", Array.Empty<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = false, CanEdit = false });

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetAccessiblePages("public", CancellationToken.None);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<List<WikiPageLinkInfo>>(partialViewResult.Model);
        
        Assert.Single(model);
        Assert.Equal("public", model[0].PageName);
    }

    [Fact]
    public async Task GetAccessiblePages_WithNoPages_ReturnsEmptyList()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WikiPageInfo>());

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetAccessiblePages("Home", CancellationToken.None);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<List<WikiPageLinkInfo>>(partialViewResult.Model);
        
        Assert.Empty(model);
    }

    [Fact]
    public async Task GetAccessiblePages_WithMultipleUserGroups_UsesAllGroups()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "users", "editors", "admins" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "admin/page", Title = "Admin Page" }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("admin/page", new[] { "users", "editors", "admins" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = true });

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetAccessiblePages("Home", CancellationToken.None);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<List<WikiPageLinkInfo>>(partialViewResult.Model);
        
        Assert.Single(model);
        Assert.Equal("admin/page", model[0].PageName);
        
        _mockPageService.Verify(
            x => x.CheckPageAccessAsync("admin/page", new[] { "users", "editors", "admins" }, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAccessiblePages_FromRootPage_CalculatesCorrectRelativePaths()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Home", Title = "Home" },
            new WikiPageInfo { PageName = "admin/settings", Title = "Settings" }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetAccessiblePages("Home", CancellationToken.None);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<List<WikiPageLinkInfo>>(partialViewResult.Model);
        
        var homePage = model.First(p => p.PageName == "Home");
        Assert.Equal("Home", homePage.RelativePath);
        
        var adminPage = model.First(p => p.PageName == "admin/settings");
        Assert.Equal("admin/settings", adminPage.RelativePath);
    }

    [Fact]
    public async Task GetAccessiblePages_FromNestedPage_CalculatesCorrectRelativePaths()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Home", Title = "Home" },
            new WikiPageInfo { PageName = "admin/settings", Title = "Settings" },
            new WikiPageInfo { PageName = "admin/users", Title = "Users" }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetAccessiblePages("admin/settings", CancellationToken.None);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<List<WikiPageLinkInfo>>(partialViewResult.Model);
        
        var homePage = model.First(p => p.PageName == "Home");
        Assert.Equal("../Home", homePage.RelativePath);
        
        var settingsPage = model.First(p => p.PageName == "admin/settings");
        Assert.Equal("settings", settingsPage.RelativePath);
        
        var usersPage = model.First(p => p.PageName == "admin/users");
        Assert.Equal("users", usersPage.RelativePath);
    }

    [Fact]
    public async Task GetAccessiblePages_WithDeeplyNestedPages_CalculatesCorrectRelativePaths()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Home", Title = "Home" },
            new WikiPageInfo { PageName = "docs/api/v1/reference", Title = "API v1" },
            new WikiPageInfo { PageName = "docs/api/v2/reference", Title = "API v2" }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>())        )
            .ReturnsAsync(allPages);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetAccessiblePages("docs/api/v1/reference", CancellationToken.None);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<List<WikiPageLinkInfo>>(partialViewResult.Model);
        
        var homePage = model.First(p => p.PageName == "Home");
        Assert.Equal("../../../Home", homePage.RelativePath);
        
        var v1Page = model.First(p => p.PageName == "docs/api/v1/reference");
        Assert.Equal("reference", v1Page.RelativePath);
        
        var v2Page = model.First(p => p.PageName == "docs/api/v2/reference");
        Assert.Equal("../v2/reference", v2Page.RelativePath);
    }

    [Fact]
    public async Task GetAccessiblePages_WithSpecialCharactersInPageName_HandlesCorrectly()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Page-With-Dashes", Title = "Dashed Page" },
            new WikiPageInfo { PageName = "Page_With_Underscores", Title = "Underscored Page" }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetAccessiblePages("Home", CancellationToken.None);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<List<WikiPageLinkInfo>>(partialViewResult.Model);
        
        Assert.Equal(2, model.Count);
        Assert.Contains(model, p => p.PageName == "Page-With-Dashes");
        Assert.Contains(model, p => p.PageName == "Page_With_Underscores");
    }

    [Fact]
    public async Task GetAccessiblePages_WithPageLevelPermissions_OnlyIncludesReadablePages()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "editors" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "page1", Title = "Page 1" },
            new WikiPageInfo { PageName = "page2", Title = "Page 2" },
            new WikiPageInfo { PageName = "page3", Title = "Page 3" }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        // page1: can read and edit
        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("page1", new[] { "editors" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = true });

        // page2: can read but not edit (should still be included)
        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("page2", new[] { "editors" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = false });

        // page3: cannot read
        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("page3", new[] { "editors" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = false, CanEdit = false });

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetAccessiblePages("Home", CancellationToken.None);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<List<WikiPageLinkInfo>>(partialViewResult.Model);
        
        Assert.Equal(2, model.Count);
        Assert.Contains(model, p => p.PageName == "page1");
        Assert.Contains(model, p => p.PageName == "page2");
        Assert.DoesNotContain(model, p => p.PageName == "page3");
    }

    [Fact]
    public async Task GetAccessiblePages_WithSameLevelPages_CalculatesSiblingPaths()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "docs/guide1", Title = "Guide 1" },
            new WikiPageInfo { PageName = "docs/guide2", Title = "Guide 2" },
            new WikiPageInfo { PageName = "docs/guide3", Title = "Guide 3" }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetAccessiblePages("docs/guide2", CancellationToken.None);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<List<WikiPageLinkInfo>>(partialViewResult.Model);
        
        Assert.Equal(3, model.Count);
        Assert.All(model, p => Assert.DoesNotContain("..", p.RelativePath));
        
        Assert.Equal("guide1", model.First(p => p.PageName == "docs/guide1").RelativePath);
        Assert.Equal("guide2", model.First(p => p.PageName == "docs/guide2").RelativePath);
        Assert.Equal("guide3", model.First(p => p.PageName == "docs/guide3").RelativePath);
    }
}
