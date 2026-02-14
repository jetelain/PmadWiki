using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pmad.Wiki.Controllers;
using Pmad.Wiki.Models;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Controllers;

public class WikiController_EditTests : WikiControllerTestBase
{
    #region Edit GET Action Tests

    [Fact]
    public async Task Edit_Get_WithExistingPage_ReturnsViewWithContent()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var page = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Existing Content",
            ContentHash = "hash123",
            HtmlContent = "<h1>Existing Content</h1>",
            Title = "Test Page"
        };

        _mockPageService
            .Setup(x => x.GetPageAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit("TestPage", null, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageEditViewModel>(viewResult.Model);
        
        Assert.Equal("TestPage", model.PageName);
        Assert.Equal("# Existing Content", model.Content);
        Assert.Equal("Update page TestPage", model.CommitMessage);
        Assert.False(model.IsNew);
        Assert.Equal("hash123", model.OriginalContentHash);
        Assert.Null(model.Culture);
    }

    [Fact]
    public async Task Edit_Get_WithNewPage_ReturnsViewWithEmptyContent()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.GetPageAsync("NewPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WikiPage?)null);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit("NewPage", null, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageEditViewModel>(viewResult.Model);
        
        Assert.Equal("NewPage", model.PageName);
        Assert.Equal(string.Empty, model.Content);
        Assert.Equal("Create page NewPage", model.CommitMessage);
        Assert.True(model.IsNew);
        Assert.Null(model.OriginalContentHash);
    }

    [Fact]
    public async Task Edit_Get_WithCulture_ReturnsViewWithCulture()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var page = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Contenu",
            ContentHash = "hash123",
            HtmlContent = "<h1>Contenu</h1>",
            Title = "Page Test",
            Culture = "fr"
        };

        _mockPageService
            .Setup(x => x.GetPageAsync("TestPage", "fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit("TestPage", "fr", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageEditViewModel>(viewResult.Model);
        
        Assert.Equal("TestPage", model.PageName);
        Assert.Equal("# Contenu", model.Content);
        Assert.Equal("fr", model.Culture);
    }

    [Fact]
    public async Task Edit_Get_WithRestoreFromCommit_LoadsPageAtRevision()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var commitId = "abc123def456";
        var historicalPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Old Version Content",
            ContentHash = "oldhash",
            HtmlContent = "<h1>Old Version Content</h1>",
            Title = "Test Page"
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, commitId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(historicalPage);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit("TestPage", null, commitId, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageEditViewModel>(viewResult.Model);
        
        Assert.Equal("TestPage", model.PageName);
        Assert.Equal("# Old Version Content", model.Content);
        Assert.Equal("Restore page TestPage to revision abc123de", model.CommitMessage);
        Assert.False(model.IsNew);
        Assert.Equal("oldhash", model.OriginalContentHash);

        _mockPageService.Verify(
            x => x.GetPageAtRevisionAsync("TestPage", null, commitId, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockPageService.Verify(
            x => x.GetPageAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Edit_Get_WithRestoreFromShortCommit_TruncatesCorrectly()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var commitId = "abc12";
        var historicalPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Old Content",
            ContentHash = "oldhash",
            HtmlContent = "<h1>Old Content</h1>",
            Title = "Test Page"
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", null, commitId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(historicalPage);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit("TestPage", null, commitId, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageEditViewModel>(viewResult.Model);
        
        Assert.Equal("Restore page TestPage to revision abc12", model.CommitMessage);
    }

    [Fact]
    public async Task Edit_Get_WhenUserNotAuthenticated_ReturnsForbid()
    {
        // Arrange
        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        // Act
        var result = await _controller.Edit("TestPage", null, null, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Edit_Get_WhenUserCannotEdit_ReturnsForbid()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(false);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit("TestPage", null, null, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Edit_Get_WithEmptyPageName_ReturnsBadRequest()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit("", null, null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Page name is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task Edit_Get_WithInvalidPageName_ReturnsBadRequest()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit("../../../etc/passwd", null, null, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Edit_Get_WithInvalidCulture_ReturnsBadRequest()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit("TestPage", "invalid-culture-code", null, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Edit_Get_WithPageLevelPermissionsAndNoEditAccess_ReturnsForbid()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "users" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = false
        };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("AdminPage", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit("AdminPage", null, null, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Edit_Get_WithPageLevelPermissionsAndEditAccess_ReturnsView()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "admins" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = true
        };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("AdminPage", new[] { "admins" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var page = new WikiPage
        {
            PageName = "AdminPage",
            Content = "# Admin Content",
            ContentHash = "hash123",
            HtmlContent = "<h1>Admin Content</h1>",
            Title = "Admin Page"
        };

        _mockPageService
            .Setup(x => x.GetPageAsync("AdminPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit("AdminPage", null, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageEditViewModel>(viewResult.Model);
        Assert.Equal("AdminPage", model.PageName);
    }

    [Fact]
    public async Task Edit_Get_WithNestedPagePath_ReturnsView()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.GetPageAsync("docs/api/reference", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WikiPage?)null);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit("docs/api/reference", null, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageEditViewModel>(viewResult.Model);
        
        Assert.Equal("docs/api/reference", model.PageName);
        Assert.Equal("Create page docs/api/reference", model.CommitMessage);
    }

    [Fact]
    public async Task Edit_Get_GeneratesBreadcrumb()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var page = new WikiPage
        {
            PageName = "docs/guide",
            Content = "# Guide",
            ContentHash = "hash123",
            HtmlContent = "<h1>Guide</h1>",
            Title = "User Guide"
        };

        _mockPageService
            .Setup(x => x.GetPageAsync("docs/guide", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("docs", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Documentation");

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("docs/guide", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("User Guide");

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit("docs/guide", null, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageEditViewModel>(viewResult.Model);
        
        Assert.Equal(2, model.Breadcrumb.Count);
        Assert.Equal("docs", model.Breadcrumb[0].PageName);
        Assert.Equal("docs/guide", model.Breadcrumb[1].PageName);
    }

    [Fact]
    public async Task Edit_Get_WithRestoreAndCulture_LoadsCorrectRevision()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var commitId = "xyz789";
        var historicalPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Ancien contenu",
            ContentHash = "oldhash",
            HtmlContent = "<h1>Ancien contenu</h1>",
            Title = "Page Test",
            Culture = "fr"
        };

        _mockPageService
            .Setup(x => x.GetPageAtRevisionAsync("TestPage", "fr", commitId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(historicalPage);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit("TestPage", "fr", commitId, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageEditViewModel>(viewResult.Model);
        
        Assert.Equal("TestPage", model.PageName);
        Assert.Equal("fr", model.Culture);
        Assert.Equal("# Ancien contenu", model.Content);
        Assert.Contains("Restore", model.CommitMessage);

        _mockPageService.Verify(
            x => x.GetPageAtRevisionAsync("TestPage", "fr", commitId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Edit_Get_NewPageWithCulture_CreatesEmptyPageWithCulture()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.GetPageAsync("NewPage", "de", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WikiPage?)null);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit("NewPage", "de", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiPageEditViewModel>(viewResult.Model);
        
        Assert.Equal("NewPage", model.PageName);
        Assert.Equal("de", model.Culture);
        Assert.Equal(string.Empty, model.Content);
        Assert.True(model.IsNew);
    }

    #endregion

    #region Edit POST Action Tests

    [Fact]
    public async Task Edit_Post_WithValidModel_SavesPageAndRedirects()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var model = new WikiPageEditViewModel
        {
            PageName = "TestPage",
            Content = "# Test Content",
            CommitMessage = "Create test page",
            Culture = null,
            IsNew = true
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WikiController.View), redirectResult.ActionName);
        Assert.Equal("TestPage", redirectResult.RouteValues?["id"]);

        _mockWikiPageEditService.Verify(
            x => x.SavePageAsync("TestPage", null, "# Test Content", "Create test page", mockWikiUser, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Edit_Post_WithCulture_SavesPageWithCulture()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var model = new WikiPageEditViewModel
        {
            PageName = "TestPage",
            Content = "# Contenu de Test",
            CommitMessage = "Créer la page de test",
            Culture = "fr",
            IsNew = true
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("fr", redirectResult.RouteValues?["culture"]);

        _mockWikiPageEditService.Verify(
            x => x.SavePageAsync("TestPage", "fr", "# Contenu de Test", "Créer la page de test", mockWikiUser, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Edit_Post_WhenUserNotAuthenticated_ReturnsForbid()
    {
        // Arrange
        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        var model = new WikiPageEditViewModel
        {
            PageName = "TestPage",
            Content = "# Test",
            CommitMessage = "Test",
            IsNew = true
        };

        // Act
        var result = await _controller.Edit(model, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
        _mockWikiPageEditService.Verify(
            x => x.SavePageAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Edit_Post_WhenUserCannotEdit_ReturnsForbid()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(false);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var model = new WikiPageEditViewModel
        {
            PageName = "TestPage",
            Content = "# Test",
            CommitMessage = "Test",
            IsNew = true
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit(model, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
        _mockWikiPageEditService.Verify(
            x => x.SavePageAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Edit_Post_WithInvalidPageName_ReturnsViewWithModelError()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var model = new WikiPageEditViewModel
        {
            PageName = "../../../etc/passwd",
            Content = "# Test",
            CommitMessage = "Test",
            IsNew = true
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit(model, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.True(_controller.ModelState.ContainsKey(nameof(model.PageName)));
        _mockWikiPageEditService.Verify(
            x => x.SavePageAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Edit_Post_WithInvalidCulture_ReturnsViewWithModelError()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var model = new WikiPageEditViewModel
        {
            PageName = "TestPage",
            Content = "# Test",
            CommitMessage = "Test",
            Culture = "invalid-culture-code",
            IsNew = true
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit(model, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.True(_controller.ModelState.ContainsKey(nameof(model.Culture)));
        _mockWikiPageEditService.Verify(
            x => x.SavePageAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Edit_Post_WithPageLevelPermissionsAndNoEditAccess_ReturnsForbid()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "users" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = false
        };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("AdminPage", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var model = new WikiPageEditViewModel
        {
            PageName = "AdminPage",
            Content = "# Admin Content",
            CommitMessage = "Update admin page",
            IsNew = false
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit(model, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
        _mockWikiPageEditService.Verify(
            x => x.SavePageAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Edit_Post_WithPageLevelPermissionsAndEditAccess_SavesPage()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);
        mockUser.Setup(x => x.Groups).Returns(new[] { "admins" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = true
        };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("AdminPage", new[] { "admins" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var model = new WikiPageEditViewModel
        {
            PageName = "AdminPage",
            Content = "# Admin Content",
            CommitMessage = "Update admin page",
            IsNew = false
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WikiController.View), redirectResult.ActionName);

        _mockWikiPageEditService.Verify(
            x => x.SavePageAsync("AdminPage", null, "# Admin Content", "Update admin page", mockWikiUser, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Edit_Post_WithContentHashMismatch_ReturnsViewWithWarning()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var currentPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Different Content",
            ContentHash = "newhash123",
            HtmlContent = "<h1>Different Content</h1>",
            LastModifiedBy = "otheruser",
            Title = "Test Page"
        };

        _mockPageService
            .Setup(x => x.GetPageAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentPage);

        var model = new WikiPageEditViewModel
        {
            PageName = "TestPage",
            Content = "# My Content",
            CommitMessage = "Update page",
            IsNew = false,
            OriginalContentHash = "oldhash456"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit(model, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.True(_controller.ModelState.ContainsKey(string.Empty));
        
        var errors = _controller.ModelState[string.Empty]!.Errors;
        Assert.Contains(errors, e => e.ErrorMessage.Contains("modified by otheruser"));
        
        Assert.Equal("newhash123", model.OriginalContentHash);
        
        _mockWikiPageEditService.Verify(
            x => x.SavePageAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Edit_Post_WithUnchangedContent_RedirectsWithoutSaving()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var currentPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Same Content",
            ContentHash = "hash123",
            HtmlContent = "<h1>Same Content</h1>",
            Title = "Test Page"
        };

        _mockPageService
            .Setup(x => x.GetPageAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentPage);

        var model = new WikiPageEditViewModel
        {
            PageName = "TestPage",
            Content = "# Same Content",
            CommitMessage = "Update page",
            IsNew = false,
            OriginalContentHash = "hash123"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WikiController.View), redirectResult.ActionName);
        Assert.Equal("TestPage", redirectResult.RouteValues?["id"]);

        _mockWikiPageEditService.Verify(
            x => x.SavePageAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Edit_Post_WhenSaveThrowsException_ReturnsViewWithError()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockWikiPageEditService
            .Setup(x => x.SavePageAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Repository error"));

        var model = new WikiPageEditViewModel
        {
            PageName = "TestPage",
            Content = "# Test",
            CommitMessage = "Test",
            IsNew = true
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit(model, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.True(_controller.ModelState.ContainsKey(string.Empty));
        
        var errors = _controller.ModelState[string.Empty]!.Errors;
        Assert.Contains(errors, e => e.ErrorMessage.Contains("An error occurred while saving the page"));
        
        // Verify that the error was logged
        _mockLogger.Verify(
            x => x.Log(
                Microsoft.Extensions.Logging.LogLevel.Error,
                It.IsAny<Microsoft.Extensions.Logging.EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error saving page TestPage")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Edit_Post_WithTemporaryMediaIds_CleansUpTemporaryMedia()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var model = new WikiPageEditViewModel
        {
            PageName = "TestPage",
            Content = "# Test with media",
            CommitMessage = "Add page with media",
            IsNew = true,
            TemporaryMediaIds = "abc123,def456,ghi789"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WikiController.View), redirectResult.ActionName);

        _mockWikiPageEditService.Verify(
            x => x.SavePageAsync("TestPage", null, "# Test with media", "Add page with media", mockWikiUser, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockTemporaryMediaStorage.Verify(
            x => x.CleanupUserTemporaryMediaAsync(
                mockWikiUser,
                It.Is<string[]>(ids => ids.Length == 3 && ids[0] == "abc123" && ids[1] == "def456" && ids[2] == "ghi789"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Edit_Post_WithEmptyTemporaryMediaIds_DoesNotCleanup()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var model = new WikiPageEditViewModel
        {
            PageName = "TestPage",
            Content = "# Test",
            CommitMessage = "Test",
            IsNew = true,
            TemporaryMediaIds = null
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WikiController.View), redirectResult.ActionName);

        _mockTemporaryMediaStorage.Verify(
            x => x.CleanupUserTemporaryMediaAsync(It.IsAny<IWikiUser>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Edit_Post_WithNestedPagePath_SavesCorrectly()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var model = new WikiPageEditViewModel
        {
            PageName = "docs/api/reference",
            Content = "# API Reference",
            CommitMessage = "Create API reference",
            IsNew = true
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WikiController.View), redirectResult.ActionName);
        Assert.Equal("docs/api/reference", redirectResult.RouteValues?["id"]);

        _mockWikiPageEditService.Verify(
            x => x.SavePageAsync("docs/api/reference", null, "# API Reference", "Create API reference", mockWikiUser, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Edit_Post_WithContentHashMatchAndChangedContent_SavesPage()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var currentPage = new WikiPage
        {
            PageName = "TestPage",
            Content = "# Old Content",
            ContentHash = "hash123",
            HtmlContent = "<h1>Old Content</h1>",
            Title = "Test Page"
        };

        _mockPageService
            .Setup(x => x.GetPageAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentPage);

        var model = new WikiPageEditViewModel
        {
            PageName = "TestPage",
            Content = "# New Content",
            CommitMessage = "Update content",
            IsNew = false,
            OriginalContentHash = "hash123"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WikiController.View), redirectResult.ActionName);

        _mockWikiPageEditService.Verify(
            x => x.SavePageAsync("TestPage", null, "# New Content", "Update content", mockWikiUser, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Edit_Post_WhenPageDeletedSinceEditing_SavesAsNewPage()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.GetPageAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WikiPage?)null);

        var model = new WikiPageEditViewModel
        {
            PageName = "TestPage",
            Content = "# Content",
            CommitMessage = "Update page",
            IsNew = false,
            OriginalContentHash = "oldhash"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WikiController.View), redirectResult.ActionName);

        _mockWikiPageEditService.Verify(
            x => x.SavePageAsync("TestPage", null, "# Content", "Update page", mockWikiUser, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Edit_Post_WithWhitespaceInTemporaryMediaIds_FiltersEmpty()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var model = new WikiPageEditViewModel
        {
            PageName = "TestPage",
            Content = "# Test",
            CommitMessage = "Test",
            IsNew = true,
            TemporaryMediaIds = "abc123,,def456,  ,ghi789"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        
        // RemoveEmptyEntries only removes empty strings, not whitespace-only strings
        _mockTemporaryMediaStorage.Verify(
            x => x.CleanupUserTemporaryMediaAsync(
                mockWikiUser,
                It.Is<string[]>(ids => ids.Length == 4 && ids[0] == "abc123" && ids[1] == "def456" && ids[2] == "  " && ids[3] == "ghi789"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Edit_Post_WithInvalidModelState_ReturnsViewWithoutSaving()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var model = new WikiPageEditViewModel
        {
            PageName = "TestPage",
            Content = "# Test",
            CommitMessage = "Test",
            IsNew = true
        };

        SetupUserContext("testuser");
        _controller.ModelState.AddModelError("Content", "Content is required");

        // Act
        var result = await _controller.Edit(model, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
        
        _mockWikiPageEditService.Verify(
            x => x.SavePageAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IWikiUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Edit_Post_WithSingleTemporaryMediaId_CleansUpCorrectly()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var model = new WikiPageEditViewModel
        {
            PageName = "TestPage",
            Content = "# Test",
            CommitMessage = "Test",
            IsNew = true,
            TemporaryMediaIds = "singleid123"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Edit(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        
        _mockTemporaryMediaStorage.Verify(
            x => x.CleanupUserTemporaryMediaAsync(
                mockWikiUser,
                It.Is<string[]>(ids => ids.Length == 1 && ids[0] == "singleid123"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
