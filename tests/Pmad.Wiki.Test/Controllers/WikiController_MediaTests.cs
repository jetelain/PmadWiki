using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Controllers;

public class WikiController_MediaTests : WikiControllerTestBase
{
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
}
