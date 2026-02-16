using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pmad.Wiki.Controllers;
using Pmad.Wiki.Models;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Controllers;

public class WikiController_CreateTests : WikiControllerTestBase
{
    #region Create GET Action Tests

    [Fact]
    public async Task Create_WhenUserCannotEdit_ReturnsForbid()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(false);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Create(null, null, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Create_WhenUserNotAuthenticated_ReturnsForbid()
    {
        // Arrange
        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        // Act
        var result = await _controller.Create(null, null, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Create_WithValidUser_ReturnsViewWithTemplates()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var templates = new List<WikiTemplate>
        {
            new() { TemplateName = "Template1", Content = "# Template 1", DisplayName = "Template One" },
            new() { TemplateName = "Template2", Content = "# Template 2", DisplayName = "Template Two" }
        };

        _mockTemplateService
            .Setup(x => x.GetAllTemplatesAsync(mockUser.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Create(null, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreateFromTemplateViewModel>(viewResult.Model);
        
        Assert.Equal(2, model.Templates.Count);
        Assert.Equal("Template1", model.Templates[0].TemplateName);
        Assert.Equal("Template2", model.Templates[1].TemplateName);
        Assert.Null(model.Culture);
        Assert.Null(model.FromPage);
    }

    [Fact]
    public async Task Create_WithCulture_ReturnsViewWithCulture()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var templates = new List<WikiTemplate>();
        _mockTemplateService
            .Setup(x => x.GetAllTemplatesAsync(mockUser.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Create(null, "fr", CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreateFromTemplateViewModel>(viewResult.Model);
        
        Assert.Equal("fr", model.Culture);
    }

    [Fact]
    public async Task Create_WithFromPage_ReturnsViewWithFromPage()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var templates = new List<WikiTemplate>();
        _mockTemplateService
            .Setup(x => x.GetAllTemplatesAsync(mockUser.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Create("docs/guide", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreateFromTemplateViewModel>(viewResult.Model);
        
        Assert.Equal("docs/guide", model.FromPage);
    }

    [Fact]
    public async Task Create_WithFromPageAndCulture_ReturnsViewWithBoth()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var templates = new List<WikiTemplate>();
        _mockTemplateService
            .Setup(x => x.GetAllTemplatesAsync(mockUser.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Create("docs/guide", "de", CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreateFromTemplateViewModel>(viewResult.Model);
        
        Assert.Equal("docs/guide", model.FromPage);
        Assert.Equal("de", model.Culture);
    }

    [Fact]
    public async Task Create_WithNoTemplates_ReturnsViewWithEmptyTemplateList()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var templates = new List<WikiTemplate>();
        _mockTemplateService
            .Setup(x => x.GetAllTemplatesAsync(mockUser.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.Create(null, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreateFromTemplateViewModel>(viewResult.Model);
        
        Assert.Empty(model.Templates);
    }

    #endregion

    #region CreatePage GET Action Tests

    [Fact]
    public async Task CreatePage_WhenUserCannotEdit_ReturnsForbid()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(false);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePage(null, null, null, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task CreatePage_WhenUserNotAuthenticated_ReturnsForbid()
    {
        // Arrange
        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        // Act
        var result = await _controller.CreatePage(null, null, null, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task CreatePage_WithoutTemplateId_ReturnsViewWithDefaultValues()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePage(null, null, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreatePageViewModel>(viewResult.Model);
        
        Assert.Null(model.TemplateId);
        Assert.Null(model.TemplateName);
        Assert.Null(model.Culture);
        Assert.Null(model.FromPage);
        Assert.Equal(string.Empty, model.Location);
        Assert.Equal("NewPage", model.PageName);
    }

    [Fact]
    public async Task CreatePage_WithInvalidTemplateId_ReturnsNotFound()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockTemplateService
            .Setup(x => x.GetTemplateAsync(mockUser.Object, "invalid-template", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WikiTemplate?)null);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePage("invalid-template", null, null, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreatePage_WithValidTemplateId_ReturnsViewWithTemplateInfo()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var template = new WikiTemplate
        {
            TemplateName = "meeting-notes",
            Content = "# Meeting Notes\n\nDate: {{date}}\n",
            DisplayName = "Meeting Notes",
            Description = "Template for meeting notes"
        };

        _mockTemplateService
            .Setup(x => x.GetTemplateAsync(mockUser.Object, "meeting-notes", It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePage("meeting-notes", null, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreatePageViewModel>(viewResult.Model);
        
        Assert.Equal("meeting-notes", model.TemplateId);
        Assert.Equal("Meeting Notes", model.TemplateName);
        Assert.Equal("NewPage", model.PageName);
        Assert.Equal(string.Empty, model.Location);
    }

    [Fact]
    public async Task CreatePage_WithTemplateHavingNamePattern_UsesSuggestedName()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var template = new WikiTemplate
        {
            TemplateName = "daily-report",
            Content = "# Daily Report\n",
            DisplayName = "Daily Report",
            NamePattern = "Report-{{date}}"
        };

        _mockTemplateService
            .Setup(x => x.GetTemplateAsync(mockUser.Object, "daily-report", It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _mockTemplateService
            .Setup(x => x.ResolvePlaceHolders("Report-{{date}}"))
            .Returns("Report-2024-01-15");

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePage("daily-report", null, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreatePageViewModel>(viewResult.Model);
        
        Assert.Equal("Report-2024-01-15", model.PageName);
        
        _mockTemplateService.Verify(
            x => x.ResolvePlaceHolders("Report-{{date}}"),
            Times.Once);
    }

    [Fact]
    public async Task CreatePage_WithTemplateHavingDefaultLocation_UsesDefaultLocation()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var template = new WikiTemplate
        {
            TemplateName = "project-doc",
            Content = "# Project Documentation\n",
            DisplayName = "Project Documentation",
            DefaultLocation = "projects/{{year}}"
        };

        _mockTemplateService
            .Setup(x => x.GetTemplateAsync(mockUser.Object, "project-doc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _mockTemplateService
            .Setup(x => x.ResolvePlaceHolders("projects/{{year}}"))
            .Returns("projects/2024");

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePage("project-doc", null, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreatePageViewModel>(viewResult.Model);
        
        Assert.Equal("projects/2024", model.Location);
        
        _mockTemplateService.Verify(
            x => x.ResolvePlaceHolders("projects/{{year}}"),
            Times.Once);
    }

    [Fact]
    public async Task CreatePage_WithFromPage_UsesFromPageDirectoryAsDefaultLocation()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePage(null, "docs/api/reference", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreatePageViewModel>(viewResult.Model);
        
        Assert.Equal("docs/api/reference", model.FromPage);
        Assert.Equal("docs/api", model.Location);
    }

    [Fact]
    public async Task CreatePage_WithTemplateAndFromPage_PrefersTemplateDefaultLocation()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var template = new WikiTemplate
        {
            TemplateName = "blog-post",
            Content = "# Blog Post\n",
            DisplayName = "Blog Post",
            DefaultLocation = "blog/posts"
        };

        _mockTemplateService
            .Setup(x => x.GetTemplateAsync(mockUser.Object, "blog-post", It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _mockTemplateService
            .Setup(x => x.ResolvePlaceHolders("blog/posts"))
            .Returns("blog/posts");

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePage("blog-post", "docs/guide", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreatePageViewModel>(viewResult.Model);
        
        Assert.Equal("blog/posts", model.Location);
        Assert.Equal("docs/guide", model.FromPage);
    }

    [Fact]
    public async Task CreatePage_WithCulture_PassesCultureToModel()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePage(null, null, "es", CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreatePageViewModel>(viewResult.Model);
        
        Assert.Equal("es", model.Culture);
    }

    [Fact]
    public async Task CreatePage_WithTemplateNameOnly_UsesTemplateNameAsDisplayName()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var template = new WikiTemplate
        {
            TemplateName = "simple-template",
            Content = "# Simple Template\n",
            DisplayName = null
        };

        _mockTemplateService
            .Setup(x => x.GetTemplateAsync(mockUser.Object, "simple-template", It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePage("simple-template", null, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreatePageViewModel>(viewResult.Model);
        
        Assert.Equal("simple-template", model.TemplateName);
    }

    [Fact]
    public async Task CreatePage_WithEmptyNamePattern_UsesDefaultPageName()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var template = new WikiTemplate
        {
            TemplateName = "basic-template",
            Content = "# Template\n",
            DisplayName = "Basic Template",
            NamePattern = ""
        };

        _mockTemplateService
            .Setup(x => x.GetTemplateAsync(mockUser.Object, "basic-template", It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePage("basic-template", null, null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreatePageViewModel>(viewResult.Model);
        
        Assert.Equal("NewPage", model.PageName);
        
        _mockTemplateService.Verify(
            x => x.ResolvePlaceHolders(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task CreatePage_WithRootLevelFromPage_UsesEmptyLocation()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePage(null, "Home", null, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<WikiCreatePageViewModel>(viewResult.Model);
        
        Assert.Equal("Home", model.FromPage);
        Assert.Equal(string.Empty, model.Location);
    }

    #endregion

    #region CreatePageConfirm POST Action Tests

    [Fact]
    public async Task CreatePageConfirm_WhenUserCannotEdit_ReturnsForbid()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(false);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var model = new WikiCreatePageViewModel
        {
            PageName = "TestPage"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task CreatePageConfirm_WhenUserNotAuthenticated_ReturnsForbid()
    {
        // Arrange
        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        var model = new WikiCreatePageViewModel
        {
            PageName = "TestPage"
        };

        // Act
        var result = await _controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task CreatePageConfirm_WithValidPageName_RedirectsToEdit()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.PageExistsAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var model = new WikiCreatePageViewModel
        {
            PageName = "TestPage",
            Location = null
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WikiController.Edit), redirectResult.ActionName);
        Assert.Equal("TestPage", redirectResult.RouteValues?["id"]);
        Assert.Null(redirectResult.RouteValues?["culture"]);
        Assert.Null(redirectResult.RouteValues?["templateId"]);
    }

    [Fact]
    public async Task CreatePageConfirm_WithLocationAndPageName_CombinesIntoFullPath()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.PageExistsAsync("docs/api/MyNewPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var model = new WikiCreatePageViewModel
        {
            PageName = "MyNewPage",
            Location = "docs/api"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WikiController.Edit), redirectResult.ActionName);
        Assert.Equal("docs/api/MyNewPage", redirectResult.RouteValues?["id"]);
        
        _mockPageService.Verify(
            x => x.PageExistsAsync("docs/api/MyNewPage", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreatePageConfirm_WithWhitespaceInLocation_TrimsWhitespace()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.PageExistsAsync("docs/NewPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var model = new WikiCreatePageViewModel
        {
            PageName = " NewPage ",
            Location = " docs "
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("docs/NewPage", redirectResult.RouteValues?["id"]);
    }

    [Fact]
    public async Task CreatePageConfirm_WithEmptyLocation_UsesOnlyPageName()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.PageExistsAsync("RootPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var model = new WikiCreatePageViewModel
        {
            PageName = "RootPage",
            Location = ""
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("RootPage", redirectResult.RouteValues?["id"]);
    }

    [Fact]
    public async Task CreatePageConfirm_WithCulture_PassesCultureToEdit()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.PageExistsAsync("TestPage", "de", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var model = new WikiCreatePageViewModel
        {
            PageName = "TestPage",
            Culture = "de"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("de", redirectResult.RouteValues?["culture"]);
    }

    [Fact]
    public async Task CreatePageConfirm_WithTemplateId_PassesTemplateIdToEdit()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.PageExistsAsync("TestPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var model = new WikiCreatePageViewModel
        {
            PageName = "TestPage",
            TemplateId = "meeting-notes"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("meeting-notes", redirectResult.RouteValues?["templateId"]);
    }

    [Fact]
    public async Task CreatePageConfirm_WithInvalidPageName_ReturnsViewWithError()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var model = new WikiCreatePageViewModel
        {
            PageName = "../../../etc/passwd"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("CreatePage", viewResult.ViewName);
        Assert.False(_controller.ModelState.IsValid);
        Assert.True(_controller.ModelState.ContainsKey(nameof(model.PageName)));
        
        var errors = _controller.ModelState[nameof(model.PageName)]!.Errors;
        Assert.Contains(errors, e => e.ErrorMessage.Contains("Invalid page name"));
        
        _mockPageService.Verify(
            x => x.PageExistsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreatePageConfirm_WithInvalidCulture_ReturnsViewWithError()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var model = new WikiCreatePageViewModel
        {
            PageName = "TestPage",
            Culture = "invalid-culture-code"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("CreatePage", viewResult.ViewName);
        Assert.False(_controller.ModelState.IsValid);
        Assert.True(_controller.ModelState.ContainsKey(nameof(model.Culture)));
        
        var errors = _controller.ModelState[nameof(model.Culture)]!.Errors;
        Assert.Contains(errors, e => e.ErrorMessage.Contains("Invalid culture identifier"));
    }

    [Fact]
    public async Task CreatePageConfirm_WhenPageAlreadyExists_ReturnsViewWithError()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.PageExistsAsync("ExistingPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var model = new WikiCreatePageViewModel
        {
            PageName = "ExistingPage"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("CreatePage", viewResult.ViewName);
        Assert.False(_controller.ModelState.IsValid);
        Assert.True(_controller.ModelState.ContainsKey(string.Empty));
        
        var errors = _controller.ModelState[string.Empty]!.Errors;
        Assert.Contains(errors, e => e.ErrorMessage.Contains("A page with this name already exists"));
    }

    [Fact]
    public async Task CreatePageConfirm_WhenPageExistsWithDifferentCulture_Succeeds()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.PageExistsAsync("ExistingPage", "fr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var model = new WikiCreatePageViewModel
        {
            PageName = "ExistingPage",
            Culture = "fr"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WikiController.Edit), redirectResult.ActionName);
    }

    [Fact]
    public async Task CreatePageConfirm_WithPageLevelPermissionsAndNoEditAccess_ReturnsForbid()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
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

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("admin/SecretPage", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var model = new WikiCreatePageViewModel
        {
            PageName = "SecretPage",
            Location = "admin"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
        
        _mockPageService.Verify(
            x => x.PageExistsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreatePageConfirm_WithPageLevelPermissionsAndEditAccess_Succeeds()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "admins" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = true
        };

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("admin/SecretPage", new[] { "admins" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        _mockPageService
            .Setup(x => x.PageExistsAsync("admin/SecretPage", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var model = new WikiCreatePageViewModel
        {
            PageName = "SecretPage",
            Location = "admin"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WikiController.Edit), redirectResult.ActionName);
        Assert.Equal("admin/SecretPage", redirectResult.RouteValues?["id"]);
    }

    [Fact]
    public async Task CreatePageConfirm_WithInvalidModelState_ReturnsViewWithoutChecking()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var model = new WikiCreatePageViewModel
        {
            PageName = "TestPage"
        };

        SetupUserContext("testuser");
        _controller.ModelState.AddModelError("SomeField", "Some error");

        // Act
        var result = await _controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("CreatePage", viewResult.ViewName);
        
        _mockPageService.Verify(
            x => x.PageExistsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreatePageConfirm_WithNestedLocationAndPageName_CreatesCorrectPath()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.PageExistsAsync("projects/2024/Q1/Report", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var model = new WikiCreatePageViewModel
        {
            PageName = "Report",
            Location = "projects/2024/Q1"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("projects/2024/Q1/Report", redirectResult.RouteValues?["id"]);
    }

    [Fact]
    public async Task CreatePageConfirm_WithAllParameters_PassesAllToEdit()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.PageExistsAsync("docs/MyPage", "pt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var model = new WikiCreatePageViewModel
        {
            PageName = "MyPage",
            Location = "docs",
            Culture = "pt",
            TemplateId = "standard-doc"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WikiController.Edit), redirectResult.ActionName);
        Assert.Equal("docs/MyPage", redirectResult.RouteValues?["id"]);
        Assert.Equal("pt", redirectResult.RouteValues?["culture"]);
        Assert.Equal("standard-doc", redirectResult.RouteValues?["templateId"]);
    }

    [Fact]
    public async Task CreatePageConfirm_WithInvalidLocationAndValidPageName_ValidatesFullPath()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var model = new WikiCreatePageViewModel
        {
            PageName = "ValidPage",
            Location = "../../etc"
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.CreatePageConfirm(model, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("CreatePage", viewResult.ViewName);
        Assert.False(_controller.ModelState.IsValid);
        Assert.True(_controller.ModelState.ContainsKey(nameof(model.PageName)));
    }

    #endregion
}
