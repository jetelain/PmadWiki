using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pmad.Wiki.Models;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Controllers;

public class WikiController_UploadMediaTests : WikiControllerTestBase
{
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

}
