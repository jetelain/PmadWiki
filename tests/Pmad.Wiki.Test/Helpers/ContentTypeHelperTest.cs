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

    [Theory]
    [InlineData(".png", Pmad.Wiki.Models.MediaType.Image)]
    [InlineData(".jpg", Pmad.Wiki.Models.MediaType.Image)]
    [InlineData(".jpeg", Pmad.Wiki.Models.MediaType.Image)]
    [InlineData(".gif", Pmad.Wiki.Models.MediaType.Image)]
    [InlineData(".svg", Pmad.Wiki.Models.MediaType.Image)]
    [InlineData(".webp", Pmad.Wiki.Models.MediaType.Image)]
    public void GetMediaType_WithImageExtensions_ReturnsImageType(string extension, Pmad.Wiki.Models.MediaType expectedType)
    {
        // Act
        var mediaType = ContentTypeHelper.GetMediaType(extension);

        // Assert
        Assert.Equal(expectedType, mediaType);
    }

    [Theory]
    [InlineData(".mp4", Pmad.Wiki.Models.MediaType.Video)]
    [InlineData(".webm", Pmad.Wiki.Models.MediaType.Video)]
    [InlineData(".ogg", Pmad.Wiki.Models.MediaType.Video)]
    public void GetMediaType_WithVideoExtensions_ReturnsVideoType(string extension, Pmad.Wiki.Models.MediaType expectedType)
    {
        // Act
        var mediaType = ContentTypeHelper.GetMediaType(extension);

        // Assert
        Assert.Equal(expectedType, mediaType);
    }

    [Theory]
    [InlineData(".pdf", Pmad.Wiki.Models.MediaType.Document)]
    public void GetMediaType_WithDocumentExtensions_ReturnsDocumentType(string extension, Pmad.Wiki.Models.MediaType expectedType)
    {
        // Act
        var mediaType = ContentTypeHelper.GetMediaType(extension);

        // Assert
        Assert.Equal(expectedType, mediaType);
    }

    [Theory]
    [InlineData(".txt", Pmad.Wiki.Models.MediaType.File)]
    [InlineData(".zip", Pmad.Wiki.Models.MediaType.File)]
    [InlineData(".doc", Pmad.Wiki.Models.MediaType.File)]
    [InlineData(".xlsx", Pmad.Wiki.Models.MediaType.File)]
    [InlineData(".unknown", Pmad.Wiki.Models.MediaType.File)]
    [InlineData("", Pmad.Wiki.Models.MediaType.File)]
    public void GetMediaType_WithOtherExtensions_ReturnsFileType(string extension, Pmad.Wiki.Models.MediaType expectedType)
    {
        // Act
        var mediaType = ContentTypeHelper.GetMediaType(extension);

        // Assert
        Assert.Equal(expectedType, mediaType);
    }

    [Fact]
    public void GetMediaType_IsCaseInsensitive()
    {
        // Act
        var mediaType1 = ContentTypeHelper.GetMediaType(".PNG");
        var mediaType2 = ContentTypeHelper.GetMediaType(".Mp4");
        var mediaType3 = ContentTypeHelper.GetMediaType(".PDF");

        // Assert
        Assert.Equal(Pmad.Wiki.Models.MediaType.Image, mediaType1);
        Assert.Equal(Pmad.Wiki.Models.MediaType.Video, mediaType2);
        Assert.Equal(Pmad.Wiki.Models.MediaType.Document, mediaType3);
    }
}
