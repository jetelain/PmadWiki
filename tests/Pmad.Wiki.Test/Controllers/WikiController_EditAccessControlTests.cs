using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pmad.Wiki.Models;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Controllers;

public class WikiController_EditAccessControlTests : WikiControllerTestBase
{
    #region GET EditAccessControl Tests

    [Fact]
    public async Task EditAccessControl_Get_WithAdminUser_ReturnsViewWithSerializedRules()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

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
        var result = await _controller.EditAccessControl(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiAccessControlEditViewModel>(viewResult.Model);
        
        Assert.NotEmpty(model.Content);
        Assert.Contains("admin/**", model.Content);
        Assert.Contains("admins", model.Content);
    }

    [Fact]
    public async Task EditAccessControl_Get_WhenUserNotAuthenticated_ReturnsForbid()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        // Act
        var result = await _controller.EditAccessControl(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task EditAccessControl_Get_WhenUserCannotAdmin_ReturnsForbid()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(false);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("user");

        // Act
        var result = await _controller.EditAccessControl(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task EditAccessControl_Get_WhenPermissionsDisabled_ReturnsBadRequest()
    {
        // Arrange
        _options.UsePageLevelPermissions = false;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("admin");

        // Act
        var result = await _controller.EditAccessControl(CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Page-level permissions are not enabled.", badRequestResult.Value);
    }

    [Fact]
    public async Task EditAccessControl_Get_WithNoRules_ReturnsViewWithExamples()
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
        var result = await _controller.EditAccessControl(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiAccessControlEditViewModel>(viewResult.Model);
        
        Assert.NotEmpty(model.Content);
        Assert.Contains("Examples:", model.Content);
    }

    [Fact]
    public async Task EditAccessControl_Get_CallsGetRulesAsync()
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
        await _controller.EditAccessControl(CancellationToken.None);

        // Assert
        _mockAccessControlService.Verify(
            x => x.GetRulesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EditAccessControl_Get_WithComplexRules_SerializesCorrectly()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("admin/**", new[] { "group1", "group2" }, new[] { "group1" }, 0),
            new PageAccessRule("private/*", Array.Empty<string>(), new[] { "editors" }, 1)
        };

        _mockAccessControlService
            .Setup(x => x.GetRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);

        SetupUserContext("admin");

        // Act
        var result = await _controller.EditAccessControl(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiAccessControlEditViewModel>(viewResult.Model);
        
        Assert.Contains("admin/**", model.Content);
        Assert.Contains("private/*", model.Content);
    }

    [Fact]
    public async Task EditAccessControl_Get_ReturnsViewResult()
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
        var result = await _controller.EditAccessControl(CancellationToken.None);

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    #endregion

    #region POST EditAccessControl Tests

    [Fact]
    public async Task EditAccessControl_Post_WithValidRules_SavesAndRedirects()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);
        mockUser.Setup(x => x.User).Returns(new Mock<IWikiUser>().Object);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockAccessControlService
            .Setup(x => x.SaveRulesAsync(It.IsAny<List<PageAccessRule>>(), It.IsAny<string>(), It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupUserContext("admin");

        var model = new WikiAccessControlEditViewModel
        {
            Content = "admin/** | admins | admins\n* | users | editors",
            CommitMessage = "Update access control rules"
        };

        // Act
        var result = await _controller.EditAccessControl(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AccessControl", redirectResult.ActionName);

        _mockAccessControlService.Verify(
            x => x.SaveRulesAsync(It.IsAny<List<PageAccessRule>>(), "Update access control rules", It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EditAccessControl_Post_WhenUserNotAuthenticated_ReturnsForbid()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        var model = new WikiAccessControlEditViewModel
        {
            Content = "* | | ",
            CommitMessage = "Test"
        };

        // Act
        var result = await _controller.EditAccessControl(model, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task EditAccessControl_Post_WhenUserCannotAdmin_ReturnsForbid()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(false);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("user");

        var model = new WikiAccessControlEditViewModel
        {
            Content = "* | | ",
            CommitMessage = "Test"
        };

        // Act
        var result = await _controller.EditAccessControl(model, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task EditAccessControl_Post_WhenPermissionsDisabled_ReturnsBadRequest()
    {
        // Arrange
        _options.UsePageLevelPermissions = false;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("admin");

        var model = new WikiAccessControlEditViewModel
        {
            Content = "* | | ",
            CommitMessage = "Test"
        };

        // Act
        var result = await _controller.EditAccessControl(model, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Page-level permissions are not enabled.", badRequestResult.Value);
    }

    [Fact]
    public async Task EditAccessControl_Post_WithInvalidModelState_ReturnsView()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("admin");

        var model = new WikiAccessControlEditViewModel
        {
            Content = "* | | ",
            CommitMessage = "Test"
        };

        _controller.ModelState.AddModelError("Content", "Invalid content");

        // Act
        var result = await _controller.EditAccessControl(model, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var returnedModel = Assert.IsType<WikiAccessControlEditViewModel>(viewResult.Model);
        Assert.Equal(model.Content, returnedModel.Content);
    }

    [Fact]
    public async Task EditAccessControl_Post_WithInvalidRulesFormat_ReturnsViewWithError()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("admin");

        var model = new WikiAccessControlEditViewModel
        {
            Content = "invalid format without pipes",
            CommitMessage = "Test"
        };

        // Act
        var result = await _controller.EditAccessControl(model, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var returnedModel = Assert.IsType<WikiAccessControlEditViewModel>(viewResult.Model);
        Assert.False(_controller.ModelState.IsValid);
        Assert.Contains("Error saving rules", _controller.ModelState[string.Empty]?.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task EditAccessControl_Post_WithException_ReturnsViewWithError()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);
        mockUser.Setup(x => x.User).Returns(new Mock<IWikiUser>().Object);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockAccessControlService
            .Setup(x => x.SaveRulesAsync(It.IsAny<List<PageAccessRule>>(), It.IsAny<string>(), It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Save failed"));

        SetupUserContext("admin");

        var model = new WikiAccessControlEditViewModel
        {
            Content = "admin/** | admins | admins",
            CommitMessage = "Test"
        };

        // Act
        var result = await _controller.EditAccessControl(model, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var returnedModel = Assert.IsType<WikiAccessControlEditViewModel>(viewResult.Model);
        Assert.False(_controller.ModelState.IsValid);
        Assert.Contains("Error saving rules: Save failed", _controller.ModelState[string.Empty]?.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task EditAccessControl_Post_WithEmptyContent_SavesEmptyRules()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);
        mockUser.Setup(x => x.User).Returns(new Mock<IWikiUser>().Object);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        List<PageAccessRule>? capturedRules = null;
        _mockAccessControlService
            .Setup(x => x.SaveRulesAsync(It.IsAny<List<PageAccessRule>>(), It.IsAny<string>(), It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()))
            .Callback<List<PageAccessRule>, string, IWikiUser, CancellationToken>((rules, msg, user, ct) => capturedRules = rules)
            .Returns(Task.CompletedTask);

        SetupUserContext("admin");

        var model = new WikiAccessControlEditViewModel
        {
            Content = "# Just comments\n# No actual rules",
            CommitMessage = "Clear all rules"
        };

        // Act
        var result = await _controller.EditAccessControl(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(capturedRules);
        Assert.Empty(capturedRules);
    }

    [Fact]
    public async Task EditAccessControl_Post_CallsSaveRulesAsyncWithCorrectParameters()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockWikiUser = new Mock<IWikiUser>();
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser.Object);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockAccessControlService
            .Setup(x => x.SaveRulesAsync(It.IsAny<List<PageAccessRule>>(), It.IsAny<string>(), It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupUserContext("admin");

        var model = new WikiAccessControlEditViewModel
        {
            Content = "test/** | group1 | group2",
            CommitMessage = "Custom commit message"
        };

        // Act
        await _controller.EditAccessControl(model, CancellationToken.None);

        // Assert
        _mockAccessControlService.Verify(
            x => x.SaveRulesAsync(
                It.Is<List<PageAccessRule>>(r => r.Count == 1 && r[0].Pattern == "test/**"),
                "Custom commit message",
                mockWikiUser.Object,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EditAccessControl_Post_WithMultipleRules_ParsesAndSavesAll()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);
        mockUser.Setup(x => x.User).Returns(new Mock<IWikiUser>().Object);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        List<PageAccessRule>? capturedRules = null;
        _mockAccessControlService
            .Setup(x => x.SaveRulesAsync(It.IsAny<List<PageAccessRule>>(), It.IsAny<string>(), It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()))
            .Callback<List<PageAccessRule>, string, IWikiUser, CancellationToken>((rules, msg, user, ct) => capturedRules = rules)
            .Returns(Task.CompletedTask);

        SetupUserContext("admin");

        var model = new WikiAccessControlEditViewModel
        {
            Content = @"admin/** | admins | admins
private/* | editors | editors
* | users | ",
            CommitMessage = "Multiple rules"
        };

        // Act
        var result = await _controller.EditAccessControl(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(capturedRules);
        Assert.Equal(3, capturedRules.Count);
        Assert.Equal("admin/**", capturedRules[0].Pattern);
        Assert.Equal("private/*", capturedRules[1].Pattern);
        Assert.Equal("*", capturedRules[2].Pattern);
    }

    [Fact]
    public async Task EditAccessControl_Post_WithCommentsAndBlankLines_IgnoresThem()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);
        mockUser.Setup(x => x.User).Returns(new Mock<IWikiUser>().Object);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        List<PageAccessRule>? capturedRules = null;
        _mockAccessControlService
            .Setup(x => x.SaveRulesAsync(It.IsAny<List<PageAccessRule>>(), It.IsAny<string>(), It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()))
            .Callback<List<PageAccessRule>, string, IWikiUser, CancellationToken>((rules, msg, user, ct) => capturedRules = rules)
            .Returns(Task.CompletedTask);

        SetupUserContext("admin");

        var model = new WikiAccessControlEditViewModel
        {
            Content = @"# Comment line
admin/** | admins | admins

# Another comment
* | users | editors",
            CommitMessage = "Test"
        };

        // Act
        var result = await _controller.EditAccessControl(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(capturedRules);
        Assert.Equal(2, capturedRules.Count);
    }

    [Fact]
    public async Task EditAccessControl_Post_RedirectsToAccessControlAction()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);
        mockUser.Setup(x => x.User).Returns(new Mock<IWikiUser>().Object);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockAccessControlService
            .Setup(x => x.SaveRulesAsync(It.IsAny<List<PageAccessRule>>(), It.IsAny<string>(), It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupUserContext("admin");

        var model = new WikiAccessControlEditViewModel
        {
            Content = "* | | ",
            CommitMessage = "Test"
        };

        // Act
        var result = await _controller.EditAccessControl(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AccessControl", redirectResult.ActionName);
        Assert.Null(redirectResult.ControllerName);
    }

    [Fact]
    public async Task EditAccessControl_Post_CallsGetWikiUserWithRequireUserTrue()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);
        mockUser.Setup(x => x.User).Returns(new Mock<IWikiUser>().Object);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockAccessControlService
            .Setup(x => x.SaveRulesAsync(It.IsAny<List<PageAccessRule>>(), It.IsAny<string>(), It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupUserContext("admin");

        var model = new WikiAccessControlEditViewModel
        {
            Content = "* | | ",
            CommitMessage = "Test"
        };

        // Act
        await _controller.EditAccessControl(model, CancellationToken.None);

        // Assert
        _mockUserService.Verify(
            x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EditAccessControl_Post_WithRulesHavingEmptyGroups_SavesCorrectly()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanAdmin).Returns(true);
        mockUser.Setup(x => x.User).Returns(new Mock<IWikiUser>().Object);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        List<PageAccessRule>? capturedRules = null;
        _mockAccessControlService
            .Setup(x => x.SaveRulesAsync(It.IsAny<List<PageAccessRule>>(), It.IsAny<string>(), It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()))
            .Callback<List<PageAccessRule>, string, IWikiUser, CancellationToken>((rules, msg, user, ct) => capturedRules = rules)
            .Returns(Task.CompletedTask);

        SetupUserContext("admin");

        var model = new WikiAccessControlEditViewModel
        {
            Content = "public/** | | ",
            CommitMessage = "Test"
        };

        // Act
        var result = await _controller.EditAccessControl(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(capturedRules);
        Assert.Single(capturedRules);
        Assert.Empty(capturedRules[0].ReadGroups);
        Assert.Empty(capturedRules[0].WriteGroups);
    }

    [Fact]
    public async Task EditAccessControl_Post_WithNullWikiUser_ReturnsForbid()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        SetupUserContext("user");

        var model = new WikiAccessControlEditViewModel
        {
            Content = "* | | ",
            CommitMessage = "Test"
        };

        // Act
        var result = await _controller.EditAccessControl(model, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);

        _mockAccessControlService.Verify(
            x => x.SaveRulesAsync(It.IsAny<List<PageAccessRule>>(), It.IsAny<string>(), It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion
}
