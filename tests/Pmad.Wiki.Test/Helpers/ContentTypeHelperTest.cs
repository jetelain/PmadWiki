using Pmad.Wiki.Helpers;

namespace Pmad.Wiki.Test.Helpers;

public class ContentTypeHelperTest
{
    [Theory]
    [InlineData("image.jpg", "image/jpeg")]
    [InlineData("image.jpeg", "image/jpeg")]
    [InlineData("image.png", "image/png")]
    [InlineData("image.gif", "image/gif")]
    [InlineData("image.bmp", "image/bmp")]
    [InlineData("image.webp", "image/webp")]
    [InlineData("image.svg", "image/svg+xml")]
    [InlineData("image.ico", "image/x-icon")]
    public void GetContentType_WithImageExtensions_ReturnsCorrectContentType(string path, string expectedContentType)
    {
        // Act
        var contentType = ContentTypeHelper.GetContentType(path);

        // Assert
        Assert.Equal(expectedContentType, contentType);
    }

    [Theory]
    [InlineData("video.mp4", "video/mp4")]
    [InlineData("video.webm", "video/webm")]
    [InlineData("video.ogg", "video/ogg")]
    [InlineData("video.avi", "video/x-msvideo")]
    public void GetContentType_WithVideoExtensions_ReturnsCorrectContentType(string path, string expectedContentType)
    {
        // Act
        var contentType = ContentTypeHelper.GetContentType(path);

        // Assert
        Assert.Equal(expectedContentType, contentType);
    }

    [Theory]
    [InlineData("audio.mp3", "audio/mpeg")]
    [InlineData("audio.wav", "audio/wav")]
    public void GetContentType_WithAudioExtensions_ReturnsCorrectContentType(string path, string expectedContentType)
    {
        // Act
        var contentType = ContentTypeHelper.GetContentType(path);

        // Assert
        Assert.Equal(expectedContentType, contentType);
    }

    [Theory]
    [InlineData("document.pdf", "application/pdf")]
    [InlineData("document.json", "application/json")]
    [InlineData("document.xml", "text/xml")]
    [InlineData("archive.zip", "application/x-zip-compressed")]
    public void GetContentType_WithDocumentExtensions_ReturnsCorrectContentType(string path, string expectedContentType)
    {
        // Act
        var contentType = ContentTypeHelper.GetContentType(path);

        // Assert
        Assert.Equal(expectedContentType, contentType);
    }

    [Theory]
    [InlineData("text.txt", "text/plain")]
    [InlineData("text.html", "text/html")]
    [InlineData("text.css", "text/css")]
    [InlineData("text.js", "text/javascript")]
    public void GetContentType_WithTextExtensions_ReturnsCorrectContentType(string path, string expectedContentType)
    {
        // Act
        var contentType = ContentTypeHelper.GetContentType(path);

        // Assert
        Assert.Equal(expectedContentType, contentType);
    }

    [Theory]
    [InlineData("file.unknown")]
    [InlineData("file.xyz")]
    [InlineData("file")]
    [InlineData("file.")]
    public void GetContentType_WithUnknownExtension_ReturnsOctetStream(string path)
    {
        // Act
        var contentType = ContentTypeHelper.GetContentType(path);

        // Assert
        Assert.Equal("application/octet-stream", contentType);
    }

    [Fact]
    public void GetContentType_WithFullPath_ReturnsCorrectContentType()
    {
        // Act
        var contentType = ContentTypeHelper.GetContentType("path/to/media/image.png");

        // Assert
        Assert.Equal("image/png", contentType);
    }

    [Fact]
    public void GetContentType_WithCaseInsensitiveExtension_ReturnsCorrectContentType()
    {
        // Act
        var contentType1 = ContentTypeHelper.GetContentType("image.PNG");
        var contentType2 = ContentTypeHelper.GetContentType("image.PnG");
        var contentType3 = ContentTypeHelper.GetContentType("image.png");

        // Assert
        Assert.Equal("image/png", contentType1);
        Assert.Equal("image/png", contentType2);
        Assert.Equal("image/png", contentType3);
    }

    [Fact]
    public void GetContentType_WithPathContainingDots_ReturnsContentTypeBasedOnLastExtension()
    {
        // Act
        var contentType = ContentTypeHelper.GetContentType("file.backup.png");

        // Assert
        Assert.Equal("image/png", contentType);
    }

    [Fact]
    public void GetContentType_WithRelativePath_ReturnsCorrectContentType()
    {
        // Act
        var contentType = ContentTypeHelper.GetContentType("../media/video.mp4");

        // Assert
        Assert.Equal("video/mp4", contentType);
    }
}
