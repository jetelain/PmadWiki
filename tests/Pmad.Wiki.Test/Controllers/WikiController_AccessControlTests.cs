using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pmad.Wiki.Models;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Controllers;

public class WikiController_AccessControlTests : WikiControllerTestBase
{
    #region AccessControl Action Tests

    [Fact]
    public async Task AccessControl_WithAdminUser_ReturnsViewWithRules()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("admin/**", new[] { "admins" }, new[] { "admins" }, 0),
            new PageAccessRule("*", new[] { "users" }, new[] { "editors" }, 1)
        };

        _mockAccessControlService
            .Setup(x => x.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        SetupUserContext("admin");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiAccessControlViewModel>(viewResult.Model);
        
        Assert.Equal(2, model.Rules.Count);
        Assert.Equal("admin/**", model.Rules[0].Pattern);
        Assert.Equal("admins", model.Rules[0].ReadGroups);
        Assert.Equal("admins", model.Rules[0].WriteGroups);
        Assert.Equal(0, model.Rules[0].Order);
        
        Assert.Equal("*", model.Rules[1].Pattern);
        Assert.Equal("users", model.Rules[1].ReadGroups);
        Assert.Equal("editors", model.Rules[1].WriteGroups);
        Assert.Equal(1, model.Rules[1].Order);
    }

    [Fact]
    public async Task AccessControl_WhenUserNotAuthenticated_ReturnsForbid()
    {
        // Arrange
        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AccessControl_WhenUserCannotAdmin_ReturnsForbid()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(false);
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.CanView).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("user");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AccessControl_WithNoRules_ReturnsEmptyList()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockAccessControlService
            .Setup(x => x.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PageAccessRule>());

        SetupUserContext("admin");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiAccessControlViewModel>(viewResult.Model);
        
        Assert.Empty(model.Rules);
    }

    [Fact]
    public async Task AccessControl_SetsIsEnabledFromOptions()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockAccessControlService
            .Setup(x => x.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PageAccessRule>());

        SetupUserContext("admin");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiAccessControlViewModel>(viewResult.Model);
        
        Assert.True(model.IsEnabled);
    }

    [Fact]
    public async Task AccessControl_WhenPermissionsDisabled_SetsIsEnabledFalse()
    {
        // Arrange
        _options.UsePageLevelPermissions = false;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockAccessControlService
            .Setup(x => x.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PageAccessRule>());

        SetupUserContext("admin");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiAccessControlViewModel>(viewResult.Model);
        
        Assert.False(model.IsEnabled);
    }

    [Fact]
    public async Task AccessControl_WithMultipleGroups_FormatsCorrectly()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("test", new[] { "group1", "group2", "group3" }, new[] { "admin1", "admin2" }, 0)
        };

        _mockAccessControlService
            .Setup(x => x.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        SetupUserContext("admin");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiAccessControlViewModel>(viewResult.Model);
        
        Assert.Single(model.Rules);
        Assert.Equal("group1, group2, group3", model.Rules[0].ReadGroups);
        Assert.Equal("admin1, admin2", model.Rules[0].WriteGroups);
    }

    [Fact]
    public async Task AccessControl_WithEmptyGroups_FormatsAsEmptyString()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("*", Array.Empty<string>(), Array.Empty<string>(), 0)
        };

        _mockAccessControlService
            .Setup(x => x.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        SetupUserContext("admin");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiAccessControlViewModel>(viewResult.Model);
        
        Assert.Single(model.Rules);
        Assert.Equal("", model.Rules[0].ReadGroups);
        Assert.Equal("", model.Rules[0].WriteGroups);
    }

    [Fact]
    public async Task AccessControl_WithComplexPatterns_PreservesPatterns()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("admin/**", new[] { "admins" }, new[] { "admins" }, 0),
            new PageAccessRule("private/*", new[] { "users" }, new[] { "editors" }, 1),
            new PageAccessRule("docs/*/public", new[] { "all" }, new[] { "editors" }, 2),
            new PageAccessRule("*", Array.Empty<string>(), Array.Empty<string>(), 3)
        };

        _mockAccessControlService
            .Setup(x => x.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        SetupUserContext("admin");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiAccessControlViewModel>(viewResult.Model);
        
        Assert.Equal(4, model.Rules.Count);
        Assert.Equal("admin/**", model.Rules[0].Pattern);
        Assert.Equal("private/*", model.Rules[1].Pattern);
        Assert.Equal("docs/*/public", model.Rules[2].Pattern);
        Assert.Equal("*", model.Rules[3].Pattern);
    }

    [Fact]
    public async Task AccessControl_PreservesOrderFromService()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("pattern1", new[] { "group1" }, new[] { "group1" }, 0),
            new PageAccessRule("pattern2", new[] { "group2" }, new[] { "group2" }, 1),
            new PageAccessRule("pattern3", new[] { "group3" }, new[] { "group3" }, 2)
        };

        _mockAccessControlService
            .Setup(x => x.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        SetupUserContext("admin");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiAccessControlViewModel>(viewResult.Model);
        
        Assert.Equal(3, model.Rules.Count);
        Assert.Equal(0, model.Rules[0].Order);
        Assert.Equal(1, model.Rules[1].Order);
        Assert.Equal(2, model.Rules[2].Order);
    }

    [Fact]
    public async Task AccessControl_WithSingleGroup_FormatsWithoutComma()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("test", new[] { "readers" }, new[] { "writers" }, 0)
        };

        _mockAccessControlService
            .Setup(x => x.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        SetupUserContext("admin");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiAccessControlViewModel>(viewResult.Model);
        
        Assert.Single(model.Rules);
        Assert.Equal("readers", model.Rules[0].ReadGroups);
        Assert.Equal("writers", model.Rules[0].WriteGroups);
    }

    [Fact]
    public async Task AccessControl_CallsGetRulesAsync()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockAccessControlService
            .Setup(x => x.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PageAccessRule>());

        SetupUserContext("admin");

        // Act
        await _controller.AccessControl(CancellationToken.None);

        // Assert
        _mockAccessControlService.Verify(
            x => x.GetRulesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AccessControl_CallsGetWikiUserWithCorrectParameters()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockAccessControlService
            .Setup(x => x.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PageAccessRule>());

        SetupUserContext("admin");

        // Act
        await _controller.AccessControl(CancellationToken.None);

        // Assert
        _mockUserService.Verify(
            x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AccessControl_WithUserWhoCanEditButNotAdmin_ReturnsForbid()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(false);
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("editor");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AccessControl_WithUserWhoCanViewButNotAdmin_ReturnsForbid()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(false);
        mockUser.Setup(x => x.CanView).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("viewer");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AccessControl_WithManyRules_ReturnsAllRules()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var rules = new List<PageAccessRule>();
        for (int i = 0; i < 10; i++)
        {
            rules.Add(new PageAccessRule($"pattern{i}", new[] { $"group{i}" }, new[] { $"group{i}" }, i));
        }

        _mockAccessControlService
            .Setup(x => x.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        SetupUserContext("admin");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiAccessControlViewModel>(viewResult.Model);
        
        Assert.Equal(10, model.Rules.Count);
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal($"pattern{i}", model.Rules[i].Pattern);
            Assert.Equal(i, model.Rules[i].Order);
        }
    }

    [Fact]
    public async Task AccessControl_WithSpecialCharactersInGroupNames_PreservesCharacters()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("test", new[] { "group-1", "group_2", "group.3" }, new[] { "admin-users" }, 0)
        };

        _mockAccessControlService
            .Setup(x => x.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        SetupUserContext("admin");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiAccessControlViewModel>(viewResult.Model);
        
        Assert.Single(model.Rules);
        Assert.Equal("group-1, group_2, group.3", model.Rules[0].ReadGroups);
        Assert.Equal("admin-users", model.Rules[0].WriteGroups);
    }

    [Fact]
    public async Task AccessControl_WithReadOnlyRule_ShowsOnlyReadGroups()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("readonly/**", new[] { "viewers" }, Array.Empty<string>(), 0)
        };

        _mockAccessControlService
            .Setup(x => x.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        SetupUserContext("admin");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiAccessControlViewModel>(viewResult.Model);
        
        Assert.Single(model.Rules);
        Assert.Equal("viewers", model.Rules[0].ReadGroups);
        Assert.Equal("", model.Rules[0].WriteGroups);
    }

    [Fact]
    public async Task AccessControl_WithWriteOnlyRule_ShowsOnlyWriteGroups()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("writeonly/**", Array.Empty<string>(), new[] { "editors" }, 0)
        };

        _mockAccessControlService
            .Setup(x => x.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        SetupUserContext("admin");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiAccessControlViewModel>(viewResult.Model);
        
        Assert.Single(model.Rules);
        Assert.Equal("", model.Rules[0].ReadGroups);
        Assert.Equal("editors", model.Rules[0].WriteGroups);
    }

    [Fact]
    public async Task AccessControl_WithNullWikiUser_ReturnsForbid()
    {
        // Arrange
        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        SetupUserContext("user");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AccessControl_ReturnsViewResult()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockAccessControlService
            .Setup(x => x.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PageAccessRule>());

        SetupUserContext("admin");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task AccessControl_WithLongPatterns_PreservesFullPattern()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var longPattern = "very/deeply/nested/path/to/some/specific/directory/**";
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule(longPattern, new[] { "group" }, new[] { "group" }, 0)
        };

        _mockAccessControlService
            .Setup(x => x.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        SetupUserContext("admin");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiAccessControlViewModel>(viewResult.Model);
        
        Assert.Single(model.Rules);
        Assert.Equal(longPattern, model.Rules[0].Pattern);
    }

    [Fact]
    public async Task AccessControl_WithMixedReadAndWritePermissions_FormatsCorrectly()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("rule1", new[] { "group1", "group2" }, new[] { "group1" }, 0),
            new PageAccessRule("rule2", new[] { "group3" }, new[] { "group3", "group4" }, 1)
        };

        _mockAccessControlService
            .Setup(x => x.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        SetupUserContext("admin");

        // Act
        var result = await _controller.AccessControl(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiAccessControlViewModel>(viewResult.Model);
        
        Assert.Equal(2, model.Rules.Count);
        Assert.Equal("group1, group2", model.Rules[0].ReadGroups);
        Assert.Equal("group1", model.Rules[0].WriteGroups);
        Assert.Equal("group3", model.Rules[1].ReadGroups);
        Assert.Equal("group3, group4", model.Rules[1].WriteGroups);
    }

    #endregion
}
