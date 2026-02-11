using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Moq;
using Pmad.Wiki.Controllers;
using Pmad.Wiki.Models;
using Pmad.Wiki.Services;
using Pmad.Wiki.Test.Infrastructure;

namespace Pmad.Wiki.Test.Controllers;

public class WikiControllerTest
{
    private readonly Mock<IWikiPageService> _mockPageService;
    private readonly Mock<IWikiUserService> _mockUserService;
    private readonly Mock<IPageAccessControlService> _mockAccessControlService;
    private readonly Mock<IMarkdownRenderService> _mockMarkdownRenderService;
    private readonly Mock<ITemporaryMediaStorageService> _mockTemporaryMediaStorage;
    private readonly Mock<IWikiPageEditService> _mockWikiPageEditService;
    private readonly WikiOptions _options;
    private readonly WikiController _controller;
    private readonly LinkGenerator _linkGenerator;

    public WikiControllerTest()
    {
        _mockPageService = new Mock<IWikiPageService>();
        _mockUserService = new Mock<IWikiUserService>();
        _mockAccessControlService = new Mock<IPageAccessControlService>();
        _mockMarkdownRenderService = new Mock<IMarkdownRenderService>();
        _mockTemporaryMediaStorage = new Mock<ITemporaryMediaStorageService>();
        _mockWikiPageEditService = new Mock<IWikiPageEditService>();
        _linkGenerator = new TestLinkGenerator();

        _options = new WikiOptions
        {
            RepositoryRoot = "/test/repos",
            WikiRepositoryName = "wiki",
            BranchName = "main",
            NeutralMarkdownPageCulture = "en",
            HomePageName = "Home",
            AllowAnonymousViewing = true,
            UsePageLevelPermissions = false,
            AllowedMediaExtensions = new List<string> { ".png", ".jpg", ".jpeg", ".gif", ".pdf", ".mp4" }
        };

        var optionsWrapper = Options.Create(_options);

        _controller = new WikiController(
            _mockPageService.Object,
            _mockUserService.Object,
            _mockAccessControlService.Object,
            _mockMarkdownRenderService.Object,
            _mockTemporaryMediaStorage.Object,
            _mockWikiPageEditService.Object,
            optionsWrapper);

        // Setup default HTTP context
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor());
        _controller.ControllerContext = new ControllerContext(actionContext);
        
        // Mock URL helper to return test URLs
        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper
            .Setup(x => x.Action(It.IsAny<UrlActionContext>()))
            .Returns((UrlActionContext context) =>
            {
                var id = (context.Values as RouteValueDictionary)?["id"]?.ToString() ?? "unknown";
                return $"/Wiki/{context.Action}/{id}";
            });
        _controller.Url = mockUrlHelper.Object;
    }

    #region Media Action Tests

    [Fact]
    public async Task Media_WithValidPngFile_ReturnsFileResult()
    {
        // Arrange
        var mediaContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
        
        _mockPageService
            .Setup(x => x.GetMediaFileAsync("images/logo.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        var result = await _controller.Media("images/logo.png", CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(mediaContent, fileResult.FileContents);
        Assert.Equal("image/png", fileResult.ContentType);
    }

    [Fact]
    public async Task Media_WithValidJpegFile_ReturnsFileResult()
    {
        // Arrange
        var mediaContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG header
        
        _mockPageService
            .Setup(x => x.GetMediaFileAsync("photos/image.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        var result = await _controller.Media("photos/image.jpg", CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(mediaContent, fileResult.FileContents);
        Assert.Equal("image/jpeg", fileResult.ContentType);
    }

    [Fact]
    public async Task Media_WithValidPdfFile_ReturnsFileResult()
    {
        // Arrange
        var mediaContent = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF header
        
        _mockPageService
            .Setup(x => x.GetMediaFileAsync("documents/manual.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        var result = await _controller.Media("documents/manual.pdf", CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(mediaContent, fileResult.FileContents);
        Assert.Equal("application/pdf", fileResult.ContentType);
    }

    [Fact]
    public async Task Media_WithValidMp4File_ReturnsFileResult()
    {
        // Arrange
        var mediaContent = new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70 }; // MP4 header
        
        _mockPageService
            .Setup(x => x.GetMediaFileAsync("videos/demo.mp4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        var result = await _controller.Media("videos/demo.mp4", CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(mediaContent, fileResult.FileContents);
        Assert.Equal("video/mp4", fileResult.ContentType);
    }

    [Fact]
    public async Task Media_WhenFileDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        _mockPageService
            .Setup(x => x.GetMediaFileAsync("images/nonexistent.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _controller.Media("images/nonexistent.png", CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Media_WithInvalidPath_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Media("../../../etc/passwd", CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Media_WithPathContainingDoubleSlash_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Media("images//logo.png", CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Media_WithAbsolutePath_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Media("/images/logo.png", CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Media_WithUnsupportedExtension_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Media("scripts/malicious.exe", CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Unsupported media file type.", badRequestResult.Value);
    }

    [Fact]
    public async Task Media_WithUnsupportedTextFile_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Media("data/config.txt", CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Unsupported media file type.", badRequestResult.Value);
    }

    [Fact]
    public async Task Media_WhenAnonymousViewingDisabledAndUserNotAuthenticated_ReturnsChallenge()
    {
        // Arrange
        _options.AllowAnonymousViewing = false;

        // Act
        var result = await _controller.Media("images/logo.png", CancellationToken.None);

        // Assert
        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task Media_WhenUserAuthenticatedButCannotView_ReturnsForbid()
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

        _mockPageService
            .Setup(x => x.GetMediaFileAsync("images/logo.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        // Act
        var result = await _controller.Media("images/logo.png", CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Media_WithPageLevelPermissionsEnabled_ChecksFullMediaFilePath()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        
        var mediaContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
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
            .Setup(x => x.CheckPageAccessAsync("admin/images/logo.png", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        _mockPageService
            .Setup(x => x.GetMediaFileAsync("admin/images/logo.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Media("admin/images/logo.png", CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(mediaContent, fileResult.FileContents);
        
        _mockPageService.Verify(
            x => x.CheckPageAccessAsync("admin/images/logo.png", new[] { "users" }, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Media_WithPageLevelPermissionsEnabledAndNoReadAccess_ReturnsForbid()
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
            .Setup(x => x.CheckPageAccessAsync("admin/images/logo.png", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Media("admin/images/logo.png", CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Media_WithPageLevelPermissionsEnabledAndUnauthenticatedUser_ReturnsChallenge()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        
        var pageAccess = new PageAccessPermissions
        {
            CanRead = false,
            CanEdit = false
        };

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("admin/images/logo.png", Array.Empty<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        // Act
        var result = await _controller.Media("admin/images/logo.png", CancellationToken.None);

        // Assert
        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task Media_WithFileInRootDirectory_ChecksPageAccessForFullPath()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        
        var mediaContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        
        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = false
        };

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("logo.png", Array.Empty<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        _mockPageService
            .Setup(x => x.GetMediaFileAsync("logo.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        var result = await _controller.Media("logo.png", CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(mediaContent, fileResult.FileContents);
        
        // Should call CheckPageAccessAsync with full path even for root files
        _mockPageService.Verify(
            x => x.CheckPageAccessAsync("logo.png", Array.Empty<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Media_WithNestedDirectory_ChecksFullMediaFilePath()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;
        
        var mediaContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
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
            .Setup(x => x.CheckPageAccessAsync("docs/api/images/diagram.png", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        _mockPageService
            .Setup(x => x.GetMediaFileAsync("docs/api/images/diagram.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Media("docs/api/images/diagram.png", CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(mediaContent, fileResult.FileContents);
        
        _mockPageService.Verify(
            x => x.CheckPageAccessAsync("docs/api/images/diagram.png", new[] { "users" }, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Media_WithAuthenticatedUserAndValidAccess_ReturnsFile()
    {
        // Arrange
        var mediaContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "users" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockPageService
            .Setup(x => x.GetMediaFileAsync("images/logo.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Media("images/logo.png", CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(mediaContent, fileResult.FileContents);
    }

    [Fact]
    public async Task Media_WithEmptyPath_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Media("", CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Media_WithTrailingSlash_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Media("images/", CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Media_WithCaseInsensitiveExtension_ReturnsFile()
    {
        // Arrange
        var mediaContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        
        _mockPageService
            .Setup(x => x.GetMediaFileAsync("images/logo.PNG", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        var result = await _controller.Media("images/logo.PNG", CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(mediaContent, fileResult.FileContents);
    }

    [Fact]
    public async Task Media_WithGifFile_ReturnsFileWithCorrectContentType()
    {
        // Arrange
        var mediaContent = new byte[] { 0x47, 0x49, 0x46 }; // GIF header
        
        _mockPageService
            .Setup(x => x.GetMediaFileAsync("animations/spinner.gif", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        var result = await _controller.Media("animations/spinner.gif", CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(mediaContent, fileResult.FileContents);
        Assert.Equal("image/gif", fileResult.ContentType);
    }

    [Fact]
    public async Task Media_WithRestrictedAdminPath_ChecksFullPathNotDirectory()
    {
        // Arrange
        // This test verifies the fix: admin/config.png should be checked as "admin/config.png"
        // not as "admin", so patterns like "admin/**" will correctly restrict it
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
            CanEdit = false,
            MatchedPattern = "admin/**"
        };

        // The controller should check "admin/config.png", not "admin"
        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("admin/config.png", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Media("admin/config.png", CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
        
        // Verify it checked the full path, not just "admin"
        _mockPageService.Verify(
            x => x.CheckPageAccessAsync("admin/config.png", new[] { "users" }, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Media_WithDeeplyNestedAdminFile_ChecksFullPath()
    {
        // Arrange
        // Verify that admin/settings/images/screenshot.png is checked as the full path
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
            CanEdit = false,
            MatchedPattern = "admin/**"
        };

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("admin/settings/images/screenshot.png", new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Media("admin/settings/images/screenshot.png", CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
        
        _mockPageService.Verify(
            x => x.CheckPageAccessAsync("admin/settings/images/screenshot.png", new[] { "users" }, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Media_WithSpecificFilePattern_ChecksFullPath()
    {
        // Arrange
        // Verify that a specific file pattern like "admin/*.png" can be matched
        _options.UsePageLevelPermissions = true;
        
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "admin" });

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var pageAccess = new PageAccessPermissions
        {
            CanRead = true,
            CanEdit = false,
            MatchedPattern = "admin/*.png"
        };

        _mockPageService
            .Setup(x => x.CheckPageAccessAsync("admin/logo.png", new[] { "admin" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageAccess);

        var mediaContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        _mockPageService
            .Setup(x => x.GetMediaFileAsync("admin/logo.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "admin") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.Media("admin/logo.png", CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(mediaContent, fileResult.FileContents);
        
        // Verify the full path was checked
        _mockPageService.Verify(
            x => x.CheckPageAccessAsync("admin/logo.png", new[] { "admin" }, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region UploadMedia Action Tests

    [Fact]
    public async Task UploadMedia_WithValidPngFile_ReturnsOkWithFileInfo()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(Mock.Of<IWikiUser>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var fileName = "test.png";
        var formFile = CreateFormFile(fileName, fileContent);

        var tempId = "abc123def456";
        _mockTemporaryMediaStorage
            .Setup(x => x.StoreTemporaryMediaAsync(It.IsAny<IWikiUser>(), fileName, fileContent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempId);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.UploadMedia(formFile, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UploadMediaResponse>(okResult.Value);
        
        Assert.Equal(tempId, response.TemporaryId);
        Assert.Equal(fileName, response.FileName);
        Assert.Equal(fileContent.Length, response.Size);
        Assert.NotEmpty(response.Url);
    }

    [Fact]
    public async Task UploadMedia_WhenUserNotAuthenticated_ReturnsForbid()
    {
        // Arrange
        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        var formFile = CreateFormFile("test.png", new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        // Act
        var result = await _controller.UploadMedia(formFile, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UploadMedia_WhenUserCannotEdit_ReturnsForbid()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(false);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var formFile = CreateFormFile("test.png", new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        SetupUserContext("testuser");

        // Act
        var result = await _controller.UploadMedia(formFile, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UploadMedia_WithNullFile_ReturnsBadRequest()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.UploadMedia(null!, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<UploadMediaErrorResponse>(badRequestResult.Value);
        Assert.Equal("No file uploaded.", errorResponse.Error);
    }

    [Fact]
    public async Task UploadMedia_WithEmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var formFile = CreateFormFile("test.png", Array.Empty<byte>());

        SetupUserContext("testuser");

        // Act
        var result = await _controller.UploadMedia(formFile, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<UploadMediaErrorResponse>(badRequestResult.Value);
        Assert.Equal("No file uploaded.", errorResponse.Error);
    }

    [Fact]
    public async Task UploadMedia_WithDisallowedExtension_ReturnsBadRequest()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var formFile = CreateFormFile("malicious.exe", new byte[] { 0x4D, 0x5A });

        SetupUserContext("testuser");

        // Act
        var result = await _controller.UploadMedia(formFile, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<UploadMediaErrorResponse>(badRequestResult.Value);
        Assert.Equal("File type .exe is not allowed.", errorResponse.Error);
    }

    [Fact]
    public async Task UploadMedia_WithFileTooLarge_ReturnsBadRequest()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        // Create a file larger than 10MB
        var largeContent = new byte[11 * 1024 * 1024];
        var formFile = CreateFormFile("large.png", largeContent);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.UploadMedia(formFile, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<UploadMediaErrorResponse>(badRequestResult.Value);
        Assert.Equal("File size exceeds 10MB limit.", errorResponse.Error);
    }

    [Theory]
    [InlineData("image.jpg", ".jpg")]
    [InlineData("document.pdf", ".pdf")]
    [InlineData("video.mp4", ".mp4")]
    [InlineData("animation.gif", ".gif")]
    public async Task UploadMedia_WithDifferentAllowedExtensions_ReturnsOk(string fileName, string extension)
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(Mock.Of<IWikiUser>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var fileContent = new byte[] { 0x01, 0x02, 0x03 };
        var formFile = CreateFormFile(fileName, fileContent);

        var tempId = "tempid123";
        _mockTemporaryMediaStorage
            .Setup(x => x.StoreTemporaryMediaAsync(It.IsAny<IWikiUser>(), fileName, fileContent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempId);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.UploadMedia(formFile, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task UploadMedia_WithUppercaseExtension_IsAccepted()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(Mock.Of<IWikiUser>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var formFile = CreateFormFile("image.PNG", fileContent);

        var tempId = "tempid123";
        _mockTemporaryMediaStorage
            .Setup(x => x.StoreTemporaryMediaAsync(It.IsAny<IWikiUser>(), "image.PNG", fileContent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempId);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.UploadMedia(formFile, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UploadMedia_StoresFileWithCorrectData()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A };
        var fileName = "test.png";
        var formFile = CreateFormFile(fileName, fileContent);

        var tempId = "storedid";
        _mockTemporaryMediaStorage
            .Setup(x => x.StoreTemporaryMediaAsync(mockWikiUser, fileName, fileContent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempId);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.UploadMedia(formFile, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockTemporaryMediaStorage.Verify(
            x => x.StoreTemporaryMediaAsync(mockWikiUser, fileName, fileContent, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region TempMedia Action Tests

    [Fact]
    public async Task TempMedia_WithValidId_ReturnsFileResult()
    {
        // Arrange
        var tempId = "abc123def456";
        var fileName = "test.png";
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockTemporaryMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(mockWikiUser, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileContent);

        var tempMedia = new Dictionary<string, TemporaryMediaInfo>
        {
            [tempId] = new TemporaryMediaInfo
            {
                TemporaryId = tempId,
                OriginalFileName = fileName,
                FilePath = "/temp/path",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockTemporaryMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(mockWikiUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempMedia);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.TempMedia(tempId, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(fileContent, fileResult.FileContents);
        Assert.Equal("image/png", fileResult.ContentType);
    }

    [Fact]
    public async Task TempMedia_WhenUserNotAuthenticated_ReturnsForbid()
    {
        // Arrange
        var tempId = "abc123";
        
        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        // Act
        var result = await _controller.TempMedia(tempId, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task TempMedia_WhenUserCannotEdit_ReturnsForbid()
    {
        // Arrange
        var tempId = "abc123";
        
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(false);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.TempMedia(tempId, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task TempMedia_WithInvalidId_ReturnsBadRequest()
    {
        // Arrange
        var invalidId = "INVALID-ID-WITH-UPPERCASE";
        
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.TempMedia(invalidId, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid temporary media ID.", badRequestResult.Value);
    }

    [Fact]
    public async Task TempMedia_WhenFileNotFound_ReturnsNotFound()
    {
        // Arrange
        var tempId = "abc123";
        
        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockTemporaryMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(mockWikiUser, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.TempMedia(tempId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task TempMedia_WithoutMediaInfo_ReturnsOctetStream()
    {
        // Arrange
        var tempId = "abc123";
        var fileContent = new byte[] { 0x01, 0x02, 0x03 };

        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockTemporaryMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(mockWikiUser, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileContent);

        _mockTemporaryMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(mockWikiUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TemporaryMediaInfo>());

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.TempMedia(tempId, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(fileContent, fileResult.FileContents);
        Assert.Equal("application/octet-stream", fileResult.ContentType);
    }

    [Theory]
    [InlineData("image.jpg", "image/jpeg")]
    [InlineData("document.pdf", "application/pdf")]
    [InlineData("video.mp4", "video/mp4")]
    [InlineData("animation.gif", "image/gif")]
    public async Task TempMedia_WithDifferentFileTypes_ReturnsCorrectContentType(string fileName, string expectedContentType)
    {
        // Arrange
        var tempId = "abc123";
        var fileContent = new byte[] { 0x01, 0x02, 0x03 };

        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockTemporaryMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(mockWikiUser, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileContent);

        var tempMedia = new Dictionary<string, TemporaryMediaInfo>
        {
            [tempId] = new TemporaryMediaInfo
            {
                TemporaryId = tempId,
                OriginalFileName = fileName,
                FilePath = "/temp/path",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockTemporaryMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(mockWikiUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempMedia);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.TempMedia(tempId, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(expectedContentType, fileResult.ContentType);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task TempMedia_WithEmptyOrNullId_ReturnsBadRequest(string? tempId)
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.TempMedia(tempId!, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Temporary media ID cannot be null or empty.", badRequestResult.Value);
    }

    [Fact]
    public async Task TempMedia_OnlyReturnsFilesForCurrentUser()
    {
        // Arrange
        var tempId = "abc123";
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockTemporaryMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(mockWikiUser, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileContent);

        var tempMedia = new Dictionary<string, TemporaryMediaInfo>
        {
            [tempId] = new TemporaryMediaInfo
            {
                TemporaryId = tempId,
                OriginalFileName = "test.png",
                FilePath = "/temp/path",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _mockTemporaryMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(mockWikiUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempMedia);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "TestAuth"));
        _controller.ControllerContext.HttpContext = new DefaultHttpContext { User = user };

        // Act
        var result = await _controller.TempMedia(tempId, CancellationToken.None);

        // Assert
        Assert.IsType<FileContentResult>(result);
        
        // Verify that we're fetching files specifically for this user
        _mockTemporaryMediaStorage.Verify(
            x => x.GetTemporaryMediaAsync(mockWikiUser, tempId, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockTemporaryMediaStorage.Verify(
            x => x.GetUserTemporaryMediaAsync(mockWikiUser, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    private static IFormFile CreateFormFile(string fileName, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };
    }

    private void SetupUserContext(string userName)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, userName) }, "TestAuth"));
        var httpContext = new DefaultHttpContext { User = user };
        
        var actionContext = new ActionContext(httpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor());
        _controller.ControllerContext = new ControllerContext(actionContext);
        
        // Mock URL helper to return test URLs
        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper
            .Setup(x => x.Action(It.IsAny<UrlActionContext>()))
            .Returns((UrlActionContext context) =>
            {
                var id = (context.Values as RouteValueDictionary)?["id"]?.ToString() ?? "unknown";
                return $"/Wiki/{context.Action}/{id}";
            });
        _controller.Url = mockUrlHelper.Object;
    }
}




