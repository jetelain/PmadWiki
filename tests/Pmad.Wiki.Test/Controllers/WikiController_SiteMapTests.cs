using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pmad.Wiki.Models;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Controllers;

public class WikiController_SiteMapTests : WikiControllerTestBase
{
    #region SiteMap Action Tests

    [Fact]
    public async Task SiteMap_WithPages_ReturnsViewWithSiteMap()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "Home",
                Title = "Home Page",
                Culture = null,
                LastModified = DateTimeOffset.UtcNow,
                LastModifiedBy = "testuser"
            },
            new WikiPageInfo
            {
                PageName = "About",
                Title = "About Us",
                Culture = null
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Equal("Home", model.HomePageName);
        Assert.False(model.CanEdit);
        Assert.False(model.CanAdmin);
        Assert.Equal(2, model.RootNodes.Count);
    }

    [Fact]
    public async Task SiteMap_WhenAnonymousViewingDisabledAndUserNotAuthenticated_ReturnsChallenge()
    {
        // Arrange
        _options.AllowAnonymousViewing = false;

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task SiteMap_WhenUserAuthenticatedButCannotView_ReturnsForbid()
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
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task SiteMap_WithAuthenticatedUserWhoCanEdit_SetsCanEditToTrue()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.CanAdmin).Returns(false);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "Home",
                Title = "Home Page"
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.True(model.CanEdit);
        Assert.False(model.CanAdmin);
    }

    [Fact]
    public async Task SiteMap_WithAuthenticatedUserWhoCanAdmin_SetsCanAdminToTrue()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.CanAdmin).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "Home",
                Title = "Home Page"
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.True(model.CanEdit);
        Assert.True(model.CanAdmin);
    }

    [Fact]
    public async Task SiteMap_WithAnonymousUserAndAllowAnonymousViewing_ReturnsSiteMap()
    {
        // Arrange
        _options.AllowAnonymousViewing = true;

        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "PublicPage",
                Title = "Public Page"
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Single(model.RootNodes);
        Assert.False(model.CanEdit);
        Assert.False(model.CanAdmin);
    }

    [Fact]
    public async Task SiteMap_WithNoPages_ReturnsEmptyRootNodes()
    {
        // Arrange
        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WikiPageInfo>());

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Empty(model.RootNodes);
    }

    [Fact]
    public async Task SiteMap_WithNestedPages_BuildsHierarchy()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "docs",
                Title = "Documentation"
            },
            new WikiPageInfo
            {
                PageName = "docs/guide",
                Title = "User Guide"
            },
            new WikiPageInfo
            {
                PageName = "docs/api",
                Title = "API Reference"
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Single(model.RootNodes);
        var docsNode = model.RootNodes[0];
        Assert.Equal("docs", docsNode.PageName);
        Assert.Equal("Documentation", docsNode.DisplayName);
        Assert.Equal(2, docsNode.Children.Count);
    }

    [Fact]
    public async Task SiteMap_WithPageLevelPermissionsEnabled_FiltersPages()
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

        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "Home",
                Title = "Home Page"
            },
            new WikiPageInfo
            {
                PageName = "admin/settings",
                Title = "Admin Settings"
            },
            new WikiPageInfo
            {
                PageName = "About",
                Title = "About Us"
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        // Setup permissions: users can read Home and About, but not admin pages
        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("Home", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = false });

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("About", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = false });

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("admin/settings", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = false, CanEdit = false });

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Equal(2, model.RootNodes.Count);
        Assert.Contains(model.RootNodes, n => n.PageName == "Home");
        Assert.Contains(model.RootNodes, n => n.PageName == "About");
        Assert.DoesNotContain(model.RootNodes, n => n.PageName.StartsWith("admin"));
    }

    [Fact]
    public async Task SiteMap_WithPageLevelPermissionsDisabled_ReturnsAllPages()
    {
        // Arrange
        _options.UsePageLevelPermissions = false;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.CanEdit).Returns(false);
        mockUser.Setup(x => x.Groups).Returns(new[] { "users" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "Home",
                Title = "Home Page"
            },
            new WikiPageInfo
            {
                PageName = "admin/settings",
                Title = "Admin Settings"
            },
            new WikiPageInfo
            {
                PageName = "About",
                Title = "About Us"
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Equal(3, model.RootNodes.Count);
        Assert.Contains(model.RootNodes, n => n.PageName == "Home");
        Assert.Contains(model.RootNodes, n => n.PageName == "About");
        
        // Verify CheckPageAccessAsync was never called when permissions are disabled
        _mockPageService.Verify(
            x => x.CheckPageAccessAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SiteMap_SetsHomePageNameFromOptions()
    {
        // Arrange
        _options.HomePageName = "CustomHome";

        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "CustomHome",
                Title = "Custom Home Page"
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Equal("CustomHome", model.HomePageName);
    }

    [Fact]
    public async Task SiteMap_WithDeeplyNestedPages_BuildsCompleteHierarchy()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "docs",
                Title = "Documentation"
            },
            new WikiPageInfo
            {
                PageName = "docs/api",
                Title = "API"
            },
            new WikiPageInfo
            {
                PageName = "docs/api/v1",
                Title = "API v1"
            },
            new WikiPageInfo
            {
                PageName = "docs/api/v1/reference",
                Title = "API v1 Reference"
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Single(model.RootNodes);
        var docsNode = model.RootNodes[0];
        Assert.Equal("docs", docsNode.PageName);
        Assert.Single(docsNode.Children);
        
        var apiNode = docsNode.Children[0];
        Assert.Equal("docs/api", apiNode.PageName);
        Assert.Single(apiNode.Children);
        
        var v1Node = apiNode.Children[0];
        Assert.Equal("docs/api/v1", v1Node.PageName);
        Assert.Single(v1Node.Children);
        
        var refNode = v1Node.Children[0];
        Assert.Equal("docs/api/v1/reference", refNode.PageName);
        Assert.Empty(refNode.Children);
    }

    [Fact]
    public async Task SiteMap_WithMultipleRootPages_ReturnsAllRoots()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
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
                PageName = "Contact",
                Title = "Contact"
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Equal(3, model.RootNodes.Count);
        Assert.Contains(model.RootNodes, n => n.PageName == "Home");
        Assert.Contains(model.RootNodes, n => n.PageName == "About");
        Assert.Contains(model.RootNodes, n => n.PageName == "Contact");
    }

    [Fact]
    public async Task SiteMap_WithMixedHierarchy_BuildsCorrectStructure()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Home", Title = "Home" },
            new WikiPageInfo { PageName = "docs", Title = "Documentation" },
            new WikiPageInfo { PageName = "docs/guide", Title = "Guide" },
            new WikiPageInfo { PageName = "admin", Title = "Admin" },
            new WikiPageInfo { PageName = "admin/settings", Title = "Settings" },
            new WikiPageInfo { PageName = "admin/users", Title = "Users" }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Equal(3, model.RootNodes.Count);
        
        var homeNode = model.RootNodes.First(n => n.PageName == "Home");
        Assert.Empty(homeNode.Children);
        
        var docsNode = model.RootNodes.First(n => n.PageName == "docs");
        Assert.Single(docsNode.Children);
        Assert.Equal("docs/guide", docsNode.Children[0].PageName);
        
        var adminNode = model.RootNodes.First(n => n.PageName == "admin");
        Assert.Equal(2, adminNode.Children.Count);
    }

    [Fact]
    public async Task SiteMap_WithPagesInDifferentCultures_UsesNeutralCulture()
    {
        // Arrange
        _options.NeutralMarkdownPageCulture = "en";

        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "Home",
                Title = "Home Page",
                Culture = "en",
                LastModified = DateTimeOffset.UtcNow,
                LastModifiedBy = "user1"
            },
            new WikiPageInfo
            {
                PageName = "Home",
                Title = "Page d'accueil",
                Culture = "fr",
                LastModified = DateTimeOffset.UtcNow.AddDays(-1),
                LastModifiedBy = "user2"
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Single(model.RootNodes);
        var homeNode = model.RootNodes[0];
        Assert.Equal("Home", homeNode.PageName);
        Assert.Equal("Home Page", homeNode.Title); // Should use the "en" version
    }

    [Fact]
    public async Task SiteMap_WithPageLevelPermissionsAndAnonymousUser_UsesEmptyGroups()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "PublicPage",
                Title = "Public Page"
            },
            new WikiPageInfo
            {
                PageName = "PrivatePage",
                Title = "Private Page"
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("PublicPage", Array.Empty<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = false });

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("PrivatePage", Array.Empty<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = false, CanEdit = false });

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Single(model.RootNodes);
        Assert.Equal("PublicPage", model.RootNodes[0].PageName);
        
        _mockPageService.Verify(
            x => x.CheckPageAccessAsync("PublicPage", Array.Empty<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockPageService.Verify(
            x => x.CheckPageAccessAsync("PrivatePage", Array.Empty<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SiteMap_WithAuthenticatedUserAndNullWikiUser_AllowsIfAnonymousViewingEnabled()
    {
        // Arrange
        _options.AllowAnonymousViewing = true;

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "Home",
                Title = "Home Page"
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Single(model.RootNodes);
        Assert.False(model.CanEdit);
        Assert.False(model.CanAdmin);
    }

    [Fact]
    public async Task SiteMap_BuildsNodeWithCorrectProperties()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "TestPage",
                Title = "Test Page Title",
                Culture = "en",
                LastModified = timestamp,
                LastModifiedBy = "testauthor"
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Single(model.RootNodes);
        var node = model.RootNodes[0];
        
        Assert.Equal("TestPage", node.PageName);
        Assert.Equal("Test Page Title", node.DisplayName);
        Assert.Equal("Test Page Title", node.Title);
        Assert.Equal("en", node.Culture);
        Assert.Equal(timestamp, node.LastModified);
        Assert.Equal("testauthor", node.LastModifiedBy);
        Assert.True(node.HasPage);
        Assert.Equal(0, node.Level);
    }

    [Fact]
    public async Task SiteMap_WithIntermediateFolders_CreatesVirtualNodes()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "docs/api/reference",
                Title = "API Reference"
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Single(model.RootNodes);
        var docsNode = model.RootNodes[0];
        Assert.Equal("docs", docsNode.PageName);
        Assert.Equal("docs", docsNode.DisplayName); // Virtual node uses path segment as display name
        Assert.False(docsNode.HasPage); // It's a virtual folder
        Assert.Equal(0, docsNode.Level);
        
        Assert.Single(docsNode.Children);
        var apiNode = docsNode.Children[0];
        Assert.Equal("docs/api", apiNode.PageName);
        Assert.Equal("api", apiNode.DisplayName);
        Assert.False(apiNode.HasPage);
        Assert.Equal(1, apiNode.Level);
        
        Assert.Single(apiNode.Children);
        var refNode = apiNode.Children[0];
        Assert.Equal("docs/api/reference", refNode.PageName);
        Assert.Equal("API Reference", refNode.DisplayName);
        Assert.True(refNode.HasPage);
        Assert.Equal(2, refNode.Level);
    }

    [Fact]
    public async Task SiteMap_WithPageAndSubPages_BothHaveCorrectFlags()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "docs",
                Title = "Documentation"
            },
            new WikiPageInfo
            {
                PageName = "docs/guide",
                Title = "User Guide"
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Single(model.RootNodes);
        var docsNode = model.RootNodes[0];
        Assert.True(docsNode.HasPage); // docs page exists
        Assert.Equal("Documentation", docsNode.Title);
        
        Assert.Single(docsNode.Children);
        var guideNode = docsNode.Children[0];
        Assert.True(guideNode.HasPage); // docs/guide page exists
        Assert.Equal("User Guide", guideNode.Title);
    }

    [Fact]
    public async Task SiteMap_OrdersPagesAlphabetically()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Zebra", Title = "Z Page" },
            new WikiPageInfo { PageName = "Alpha", Title = "A Page" },
            new WikiPageInfo { PageName = "Gamma", Title = "G Page" },
            new WikiPageInfo { PageName = "Beta", Title = "B Page" }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Equal(4, model.RootNodes.Count);
        Assert.Equal("Alpha", model.RootNodes[0].PageName);
        Assert.Equal("Beta", model.RootNodes[1].PageName);
        Assert.Equal("Gamma", model.RootNodes[2].PageName);
        Assert.Equal("Zebra", model.RootNodes[3].PageName);
    }

    [Fact]
    public async Task SiteMap_WithAuthenticatedUserAndCanView_ReturnsSiteMap()
    {
        // Arrange
        _options.AllowAnonymousViewing = false;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.CanEdit).Returns(false);
        mockUser.Setup(x => x.CanAdmin).Returns(false);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "Home",
                Title = "Home Page"
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Single(model.RootNodes);
        Assert.False(model.CanEdit);
        Assert.False(model.CanAdmin);
    }

    [Fact]
    public async Task SiteMap_WithMultipleCulturesOfSamePage_GroupsCorrectly()
    {
        // Arrange
        _options.NeutralMarkdownPageCulture = "en";

        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "About",
                Title = "About Us",
                Culture = "en"
            },
            new WikiPageInfo
            {
                PageName = "About",
                Title = "À Propos",
                Culture = "fr"
            },
            new WikiPageInfo
            {
                PageName = "About",
                Title = "Über Uns",
                Culture = "de"
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        // Should be grouped as single node
        Assert.Single(model.RootNodes);
        var aboutNode = model.RootNodes[0];
        Assert.Equal("About", aboutNode.PageName);
        Assert.Equal("About Us", aboutNode.Title); // Uses neutral culture (en)
    }

    [Fact]
    public async Task SiteMap_WithPageWithoutTitle_UsesPageNameSegmentAsDisplayName()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "docs/UntitledPage",
                Title = null
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Single(model.RootNodes);
        var docsNode = model.RootNodes[0];
        Assert.Single(docsNode.Children);
        
        var untitledNode = docsNode.Children[0];
        Assert.Equal("UntitledPage", untitledNode.DisplayName);
        Assert.Null(untitledNode.Title);
    }

    [Fact]
    public async Task SiteMap_WithComplexHierarchy_BuildsCorrectLevels()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "level0", Title = "Level 0" },
            new WikiPageInfo { PageName = "level0/level1", Title = "Level 1" },
            new WikiPageInfo { PageName = "level0/level1/level2", Title = "Level 2" },
            new WikiPageInfo { PageName = "level0/level1/level2/level3", Title = "Level 3" }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        var level0Node = model.RootNodes[0];
        Assert.Equal(0, level0Node.Level);
        
        var level1Node = level0Node.Children[0];
        Assert.Equal(1, level1Node.Level);
        
        var level2Node = level1Node.Children[0];
        Assert.Equal(2, level2Node.Level);
        
        var level3Node = level2Node.Children[0];
        Assert.Equal(3, level3Node.Level);
    }

    [Fact]
    public async Task SiteMap_WithPageMetadata_PreservesAllInformation()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "ImportantPage",
                Title = "Important Page",
                Culture = "en",
                LastModified = timestamp,
                LastModifiedBy = "admin"
            }
        };

        _mockPageService
            .Setup(x => x.GetAllPagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        // Act
        var result = await _controller.SiteMap(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiSiteMapViewModel>(viewResult.Model);
        
        Assert.Single(model.RootNodes);
        var node = model.RootNodes[0];
        
        Assert.Equal("ImportantPage", node.PageName);
        Assert.Equal("Important Page", node.Title);
        Assert.Equal("en", node.Culture);
        Assert.Equal(timestamp, node.LastModified);
        Assert.Equal("admin", node.LastModifiedBy);
        Assert.True(node.HasPage);
    }

    #endregion
}
