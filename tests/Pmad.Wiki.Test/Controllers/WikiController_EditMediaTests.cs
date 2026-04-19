using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Controllers;

public class WikiController_EditMediaTests : WikiControllerTestBase
{
    #region EditMedia Action Tests

    [Fact]
    public async Task EditMedia_WithValidPath_ReturnsOkWithTemporaryId()
    {
        // Arrange
        var existingMediaPath = "images/diagram.excalidraw.svg";
        var temporaryId = "abc123def456abc123def456";

        _options.AllowedMediaExtensions = new List<string> { ".png", ".jpg", ".jpeg", ".gif", ".svg", ".pdf", ".mp4" };

        var mockWikiUser = Mock.Of<IWikiUser>();
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());
        mockUser.Setup(x => x.User).Returns(mockWikiUser);

        _mockUserService
            .Setup(x => x.GetWikiUserAsync(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockWikiPageEditService
            .Setup(x => x.CreateEditableMediaFromExisting(mockWikiUser, existingMediaPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporaryId);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.EditMedia(existingMediaPath, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic response = okResult.Value!;
        Assert.Equal(temporaryId, (string)response.TemporaryId);
        Assert.NotEmpty((string)response.Url);
    }

    [Fact]
    public async Task EditMedia_WhenUserNotAuthenticated_ReturnsForbid()
    {
        // Arrange
        _mockUserService
            .Setup(x => x.GetWikiUserAsync(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        // Act
        var result = await _controller.EditMedia("images/diagram.excalidraw.svg", CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task EditMedia_WhenUserCannotEdit_ReturnsForbid()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(false);

        _mockUserService
            .Setup(x => x.GetWikiUserAsync(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.EditMedia("images/diagram.excalidraw.svg", CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("../traversal/path.svg")]
    [InlineData("/absolute/path.svg")]
    public async Task EditMedia_WithInvalidPath_ReturnsBadRequest(string? mediaPath)
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUserAsync(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.EditMedia(mediaPath!, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid existing media path.", badRequestResult.Value);
    }

    [Fact]
    public async Task EditMedia_WithNonEditableFileType_ReturnsBadRequest()
    {
        // Arrange
        var existingMediaPath = "images/photo.png"; // Not an editable media (not .excalidraw.svg)

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUserAsync(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.EditMedia(existingMediaPath, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Unsupported media file type.", badRequestResult.Value);
    }

    [Fact]
    public async Task EditMedia_WithDisallowedExtension_ReturnsBadRequest()
    {
        // Arrange
        var existingMediaPath = "images/diagram.excalidraw.svg";

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUserAsync(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        // Override options to exclude svg
        _options.AllowedMediaExtensions = new List<string> { ".png", ".jpg" };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.EditMedia(existingMediaPath, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Unsupported media file type.", badRequestResult.Value);
    }

    [Fact]
    public async Task EditMedia_WhenUserLacksPagePermission_ReturnsForbid()
    {
        // Arrange
        var existingMediaPath = "images/diagram.excalidraw.svg";

        _options.AllowedMediaExtensions = new List<string> { ".png", ".jpg", ".jpeg", ".gif", ".svg", ".pdf", ".mp4" };
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "users" });
        mockUser.Setup(x => x.User).Returns(Mock.Of<IWikiUser>());

        _mockUserService
            .Setup(x => x.GetWikiUserAsync(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync(existingMediaPath, new[] { "users" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = false });

        SetupUserContext("testuser");

        // Act
        var result = await _controller.EditMedia(existingMediaPath, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task EditMedia_WhenCreateEditableMediaReturnsNull_ReturnsNotFound()
    {
        // Arrange
        var existingMediaPath = "images/diagram.excalidraw.svg";

        _options.AllowedMediaExtensions = new List<string> { ".png", ".jpg", ".jpeg", ".gif", ".svg", ".pdf", ".mp4" };

        var mockWikiUser = Mock.Of<IWikiUser>();
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());
        mockUser.Setup(x => x.User).Returns(mockWikiUser);

        _mockUserService
            .Setup(x => x.GetWikiUserAsync(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockWikiPageEditService
            .Setup(x => x.CreateEditableMediaFromExisting(mockWikiUser, existingMediaPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.EditMedia(existingMediaPath, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditMedia_WhenCreateEditableMediaReturnsEmptyString_ReturnsNotFound()
    {
        // Arrange
        var existingMediaPath = "images/diagram.excalidraw.svg";

        _options.AllowedMediaExtensions = new List<string> { ".png", ".jpg", ".jpeg", ".gif", ".svg", ".pdf", ".mp4" };

        var mockWikiUser = Mock.Of<IWikiUser>();
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());
        mockUser.Setup(x => x.User).Returns(mockWikiUser);

        _mockUserService
            .Setup(x => x.GetWikiUserAsync(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockWikiPageEditService
            .Setup(x => x.CreateEditableMediaFromExisting(mockWikiUser, existingMediaPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.EditMedia(existingMediaPath, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditMedia_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var existingMediaPath = "docs/architecture.excalidraw.svg";
        var temporaryId = "deadbeefdeadbeefdeadbeef";

        _options.AllowedMediaExtensions = new List<string> { ".png", ".jpg", ".jpeg", ".gif", ".svg", ".pdf", ".mp4" };

        var mockWikiUser = Mock.Of<IWikiUser>();
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());
        mockUser.Setup(x => x.User).Returns(mockWikiUser);

        _mockUserService
            .Setup(x => x.GetWikiUserAsync(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockWikiPageEditService
            .Setup(x => x.CreateEditableMediaFromExisting(mockWikiUser, existingMediaPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporaryId);

        SetupUserContext("testuser");

        // Act
        await _controller.EditMedia(existingMediaPath, CancellationToken.None);

        // Assert
        _mockWikiPageEditService.Verify(
            x => x.CreateEditableMediaFromExisting(mockWikiUser, existingMediaPath, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
