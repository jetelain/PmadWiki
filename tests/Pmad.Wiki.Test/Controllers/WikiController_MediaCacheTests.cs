using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Controllers;

public class WikiController_MediaCacheTests : WikiControllerTestBase
{
    private const string ExcalidrawPath = "diagrams/diagram.excalidraw.svg";
    private const string NormalMediaPath = "images/logo.png";

    public WikiController_MediaCacheTests()
    {
        _options.AllowedMediaExtensions = [.. _options.AllowedMediaExtensions, ".svg"];
    }

    private static string ComputeEtag(byte[] content)
    {
        return $"\"{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content))[..16]}\"";
    }

    private static TemporaryMediaInfo MakeTempMediaInfo(string tempId, string fileName, EditableMediaInfo? editableMediaInfo = null)
    {
        return new TemporaryMediaInfo
        {
            TemporaryId = tempId,
            OriginalFileName = fileName,
            FilePath = "/temp/path",
            CreatedAt = DateTimeOffset.UtcNow,
            EditableMediaInfo = editableMediaInfo
        };
    }

    #region Media – normal files

    [Fact]
    public async Task Media_NormalFile_SetsFourHourCacheControlHeader()
    {
        // Arrange
        var mediaContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        _mockPageService
            .Setup(x => x.GetMediaFileAsync(NormalMediaPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        var result = await _controller.Media(NormalMediaPath, CancellationToken.None);

        // Assert
        Assert.IsType<FileContentResult>(result);
        Assert.Equal("private, max-age=14400", _controller.Response.Headers.CacheControl.ToString());
        Assert.False(_controller.Response.Headers.ContainsKey("ETag"));
    }

    #endregion

    #region Media – editable media (excalidraw.svg)

    [Fact]
    public async Task Media_EditableMedia_SetsCacheControlNoCacheAndEtagHeader()
    {
        // Arrange
        var mediaContent = "<svg>diagram</svg>"u8.ToArray();

        _mockPageService
            .Setup(x => x.GetMediaFileAsync(ExcalidrawPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        // Act
        var result = await _controller.Media(ExcalidrawPath, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(mediaContent, fileResult.FileContents);
        Assert.Equal("no-cache", _controller.Response.Headers.CacheControl.ToString());
        Assert.Equal(ComputeEtag(mediaContent), _controller.Response.Headers.ETag.ToString());
    }

    [Fact]
    public async Task Media_EditableMedia_WhenIfNoneMatchMatchesEtag_Returns304()
    {
        // Arrange
        var mediaContent = "<svg>diagram</svg>"u8.ToArray();
        var etag = ComputeEtag(mediaContent);

        _mockPageService
            .Setup(x => x.GetMediaFileAsync(ExcalidrawPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        _controller.ControllerContext.HttpContext.Request.Headers.IfNoneMatch = etag;

        // Act
        var result = await _controller.Media(ExcalidrawPath, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status304NotModified, statusResult.StatusCode);
        Assert.Equal("no-cache", _controller.Response.Headers.CacheControl.ToString());
        Assert.Equal(etag, _controller.Response.Headers.ETag.ToString());
    }

    [Fact]
    public async Task Media_EditableMedia_WhenIfNoneMatchDiffersFromEtag_ReturnsFileResult()
    {
        // Arrange
        var mediaContent = "<svg>diagram</svg>"u8.ToArray();

        _mockPageService
            .Setup(x => x.GetMediaFileAsync(ExcalidrawPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaContent);

        _controller.ControllerContext.HttpContext.Request.Headers.IfNoneMatch = "\"stale-etag-value\"";

        // Act
        var result = await _controller.Media(ExcalidrawPath, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(mediaContent, fileResult.FileContents);
        Assert.Equal("no-cache", _controller.Response.Headers.CacheControl.ToString());
        Assert.Equal(ComputeEtag(mediaContent), _controller.Response.Headers.ETag.ToString());
    }

    #endregion

    #region TempMedia – non-editable files

    [Fact]
    public async Task TempMedia_NonEditableFile_SetsFourHourCacheControlHeader()
    {
        // Arrange
        var tempId = "abc123def456";
        var fileContent = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);

        _mockUserService
            .Setup(x => x.GetWikiUserAsync(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockTemporaryMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(mockWikiUser, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileContent);

        _mockTemporaryMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(mockWikiUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TemporaryMediaInfo>
            {
                [tempId] = MakeTempMediaInfo(tempId, "image.png")
            });

        SetupUserContext("testuser");

        // Act
        var result = await _controller.TempMedia(tempId, CancellationToken.None);

        // Assert
        Assert.IsType<FileContentResult>(result);
        Assert.Equal("private, max-age=14400", _controller.Response.Headers.CacheControl.ToString());
        Assert.False(_controller.Response.Headers.ContainsKey("ETag"));
    }

    #endregion

    #region TempMedia – editable files (excalidraw.svg)

    [Fact]
    public async Task TempMedia_EditableMedia_SetsCacheControlNoCacheAndEtagHeader()
    {
        // Arrange
        var tempId = "abc123def456";
        var fileContent = "<svg>diagram</svg>"u8.ToArray();

        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);

        _mockUserService
            .Setup(x => x.GetWikiUserAsync(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockTemporaryMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(mockWikiUser, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileContent);

        _mockTemporaryMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(mockWikiUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TemporaryMediaInfo>
            {
                [tempId] = MakeTempMediaInfo(tempId, "diagram.excalidraw.svg", new EditableMediaInfo { IsEditable = true })
            });

        SetupUserContext("testuser");

        // Act
        var result = await _controller.TempMedia(tempId, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(fileContent, fileResult.FileContents);
        Assert.Equal("no-cache", _controller.Response.Headers.CacheControl.ToString());
        Assert.Equal(ComputeEtag(fileContent), _controller.Response.Headers.ETag.ToString());
    }

    [Fact]
    public async Task TempMedia_EditableMedia_WhenIfNoneMatchMatchesEtag_Returns304()
    {
        // Arrange
        var tempId = "abc123def456";
        var fileContent = "<svg>diagram</svg>"u8.ToArray();
        var etag = ComputeEtag(fileContent);

        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);

        _mockUserService
            .Setup(x => x.GetWikiUserAsync(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockTemporaryMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(mockWikiUser, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileContent);

        _mockTemporaryMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(mockWikiUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TemporaryMediaInfo>
            {
                [tempId] = MakeTempMediaInfo(tempId, "diagram.excalidraw.svg", new EditableMediaInfo { IsEditable = true })
            });

        SetupUserContext("testuser");
        _controller.ControllerContext.HttpContext.Request.Headers.IfNoneMatch = etag;

        // Act
        var result = await _controller.TempMedia(tempId, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status304NotModified, statusResult.StatusCode);
        Assert.Equal("no-cache", _controller.Response.Headers.CacheControl.ToString());
        Assert.Equal(etag, _controller.Response.Headers.ETag.ToString());
    }

    [Fact]
    public async Task TempMedia_EditableMedia_WhenIfNoneMatchDiffersFromEtag_ReturnsFileResult()
    {
        // Arrange
        var tempId = "abc123def456";
        var fileContent = "<svg>diagram</svg>"u8.ToArray();

        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);

        _mockUserService
            .Setup(x => x.GetWikiUserAsync(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        _mockTemporaryMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(mockWikiUser, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileContent);

        _mockTemporaryMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(mockWikiUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, TemporaryMediaInfo>
            {
                [tempId] = MakeTempMediaInfo(tempId, "diagram.excalidraw.svg", new EditableMediaInfo { IsEditable = true })
            });

        SetupUserContext("testuser");
        _controller.ControllerContext.HttpContext.Request.Headers.IfNoneMatch = "\"stale-etag-value\"";

        // Act
        var result = await _controller.TempMedia(tempId, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(fileContent, fileResult.FileContents);
        Assert.Equal("no-cache", _controller.Response.Headers.CacheControl.ToString());
        Assert.Equal(ComputeEtag(fileContent), _controller.Response.Headers.ETag.ToString());
    }

    [Fact]
    public async Task TempMedia_EditableMedia_WhenContentChanges_EtagChanges()
    {
        // Arrange
        var tempId = "abc123def456";
        var fileContentV1 = "<svg>diagram v1</svg>"u8.ToArray();
        var fileContentV2 = "<svg>diagram v2</svg>"u8.ToArray();

        var mockUser = new Mock<IWikiUserWithPermissions>();
        var mockWikiUser = Mock.Of<IWikiUser>();
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.User).Returns(mockWikiUser);

        _mockUserService
            .Setup(x => x.GetWikiUserAsync(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var tempMediaEntry = new Dictionary<string, TemporaryMediaInfo>
        {
            [tempId] = MakeTempMediaInfo(tempId, "diagram.excalidraw.svg", new EditableMediaInfo { IsEditable = true })
        };

        _mockTemporaryMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(mockWikiUser, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileContentV1);
        _mockTemporaryMediaStorage
            .Setup(x => x.GetUserTemporaryMediaAsync(mockWikiUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tempMediaEntry);

        SetupUserContext("testuser");

        var resultV1 = await _controller.TempMedia(tempId, CancellationToken.None);
        var etagV1 = _controller.Response.Headers.ETag.ToString();

        // Simulate content update – rebuild context to get a fresh response
        SetupUserContext("testuser");
        _mockTemporaryMediaStorage
            .Setup(x => x.GetTemporaryMediaAsync(mockWikiUser, tempId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileContentV2);

        var resultV2 = await _controller.TempMedia(tempId, CancellationToken.None);
        var etagV2 = _controller.Response.Headers.ETag.ToString();

        // Assert
        Assert.IsType<FileContentResult>(resultV1);
        Assert.IsType<FileContentResult>(resultV2);
        Assert.NotEqual(etagV1, etagV2);
    }

    #endregion
}
