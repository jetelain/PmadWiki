using Microsoft.AspNetCore.Mvc;

namespace Pmad.Wiki.Test.Controllers;

public class WikiController_RelativeMediaTests : WikiControllerTestBase
{
    #region Input validation

    [Theory]
    [InlineData("")]
    [InlineData("invalid page name!")]
    [InlineData("../traversal")]
    public void RelativeMedia_InvalidPageName_ReturnsBadRequest(string pageName)
    {
        // Act
        var result = _controller.RelativeMedia(pageName, "image.png");

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid page name.", badRequest.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid path!.png")]
    public void RelativeMedia_InvalidRelativePath_ReturnsBadRequest(string relativePath)
    {
        // Act
        var result = _controller.RelativeMedia("docs/page", relativePath);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid media path.", badRequest.Value);
    }

    [Fact]
    public void RelativeMedia_PathWithNoExtension_ReturnsBadRequest()
    {
        // Arrange: "../../../etc/passwd" has no allowed extension → "Unsupported media file type."

        // Act
        var result = _controller.RelativeMedia("docs/page", "../../../etc/passwd");

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Unsupported media file type.", badRequest.Value);
    }

    [Fact]
    public void RelativeMedia_DisallowedExtension_ReturnsBadRequest()
    {
        // Act
        var result = _controller.RelativeMedia("docs/page", "script.exe");

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Unsupported media file type.", badRequest.Value);
    }

    [Fact]
    public void RelativeMedia_ResolvedPathFailsMediaPathValidation_ReturnsBadRequest()
    {
        // Arrange: ".hidden.png" passes MediaPathMarkdownRegex (dots are allowed) and the extension
        // check (.png), but the resolved path "docs/.hidden.png" fails IsValidMediaPath because
        // MediaPathRegex requires each segment to start with [a-zA-Z0-9_-].

        // Act
        var result = _controller.RelativeMedia("docs/page", ".hidden.png");

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid media path.", badRequest.Value);
    }

    #endregion

    #region Redirect behaviour

    [Fact]
    public void RelativeMedia_SiblingFile_RedirectsToResolvedAbsolutePath()
    {
        // Arrange: page is "docs/page", relative path is "image.png" → resolves to "docs/image.png"

        // Act
        var result = _controller.RelativeMedia("docs/page", "image.png");

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Media", redirect.ActionName);
        Assert.Equal("docs/image.png", redirect.RouteValues?["id"]);
    }

    [Fact]
    public void RelativeMedia_ParentDirectory_RedirectsToResolvedAbsolutePath()
    {
        // Arrange: page is "docs/sub/page", relative path is "../image.png" → resolves to "docs/image.png"

        // Act
        var result = _controller.RelativeMedia("docs/sub/page", "../image.png");

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Media", redirect.ActionName);
        Assert.Equal("docs/image.png", redirect.RouteValues?["id"]);
    }

    [Fact]
    public void RelativeMedia_SubDirectory_RedirectsToResolvedAbsolutePath()
    {
        // Arrange: page is "docs/page", relative path is "assets/image.png" → resolves to "docs/assets/image.png"

        // Act
        var result = _controller.RelativeMedia("docs/page", "assets/image.png");

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Media", redirect.ActionName);
        Assert.Equal("docs/assets/image.png", redirect.RouteValues?["id"]);
    }

    [Fact]
    public void RelativeMedia_RootLevelPage_RedirectsToResolvedAbsolutePath()
    {
        // Arrange: page is "Home" (no directory), relative path is "logo.png" → resolves to "logo.png"

        // Act
        var result = _controller.RelativeMedia("Home", "logo.png");

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Media", redirect.ActionName);
        Assert.Equal("logo.png", redirect.RouteValues?["id"]);
    }

    [Fact]
    public void RelativeMedia_AllowedJpegExtension_RedirectsSuccessfully()
    {
        // Act
        var result = _controller.RelativeMedia("docs/page", "photo.jpg");

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Media", redirect.ActionName);
    }

    #endregion
}
