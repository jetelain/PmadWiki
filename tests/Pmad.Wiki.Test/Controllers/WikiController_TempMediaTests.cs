using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Controllers;

public class WikiController_TempMediaTests : WikiControllerTestBase
{    
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
        Assert.Equal("Invalid temporary media ID.", badRequestResult.Value);
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

}
