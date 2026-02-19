using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pmad.Wiki.Controllers;
using Pmad.Wiki.Models;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Controllers;

public class WikiController_GetMediaGalleryTests : WikiControllerTestBase
{
    [Fact]
    public async Task GetMediaGallery_WithValidUser_ReturnsPartialViewWithMedia()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var allMedia = new List<MediaGalleryItem>
        {
            new MediaGalleryItem
            {
                Path = "images/logo.png",
                FileName = "logo.png",
                Url = string.Empty,
                MediaType = MediaType.Image,
                LastModified = DateTimeOffset.UtcNow
            },
            new MediaGalleryItem
            {
                Path = "documents/manual.pdf",
                FileName = "manual.pdf",
                Url = string.Empty,
                MediaType = MediaType.Document,
                LastModified = DateTimeOffset.UtcNow.AddDays(-1)
            }
        };

        _mockPageService
            .Setup(x => x.GetAllMediaFilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allMedia);

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = false });

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetMediaGallery(CancellationToken.None);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_MediaGalleryList", partialViewResult.ViewName);

        var model = Assert.IsType<List<MediaGalleryItem>>(partialViewResult.Model);
        Assert.Equal(2, model.Count);
        Assert.Contains(model, m => m.Path == "images/logo.png");
        Assert.Contains(model, m => m.Path == "documents/manual.pdf");
        Assert.All(model, m => Assert.NotEmpty(m.Url));
    }

    [Fact]
    public async Task GetMediaGallery_WithPermissionsEnabled_FiltersMediaByPermissions()
    {
        // Arrange
        _options.UsePageLevelPermissions = true;

        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(Array.Empty<string>());

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var allMedia = new List<MediaGalleryItem>
        {
            new MediaGalleryItem
            {
                Path = "images/logo.png",
                FileName = "logo.png",
                Url = string.Empty,
                MediaType = MediaType.Image
            },
            new MediaGalleryItem
            {
                Path = "restricted/secret.pdf",
                FileName = "secret.pdf",
                Url = string.Empty,
                MediaType = MediaType.Document
            }
        };

        _mockPageService
            .Setup(x => x.GetAllMediaFilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allMedia);

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("images/logo.png", It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = true, CanEdit = false });

        _mockAccessControlService
            .Setup(x => x.CheckPageAccessAsync("restricted/secret.pdf", It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAccessPermissions { CanRead = false, CanEdit = false });

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetMediaGallery(CancellationToken.None);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<List<MediaGalleryItem>>(partialViewResult.Model);

        Assert.Single(model);
        Assert.Equal("images/logo.png", model[0].Path);
    }

    [Fact]
    public async Task GetMediaGallery_WithAnonymousUser_ReturnsForbid()
    {
        // Arrange
        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        // Act
        var result = await _controller.GetMediaGallery(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetMediaGallery_WithUserWithoutEditPermission_ReturnsForbid()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(false);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.GetMediaGallery(CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }
}

