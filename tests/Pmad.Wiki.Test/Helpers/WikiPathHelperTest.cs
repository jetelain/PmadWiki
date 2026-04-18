using Pmad.Wiki.Helpers;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Helpers;

public class WikiPathHelperTest
{
    [Theory]
    [InlineData("dir", "file.md", "dir/file.md")]
    [InlineData("dir/sub", "file.md", "dir/sub/file.md")]
    [InlineData("", "file.md", "file.md")]
    [InlineData(null, "file.md", "file.md")]
    public void Combine_ReturnsExpectedResult(string? directoryName, string fileName, string expected)
    {
        var result = WikiPathHelper.Combine(directoryName, fileName);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("file.md", "file", ".md")]
    [InlineData("file.MD", "file", ".md")]
    [InlineData("file.excalidraw.svg", "file", ".excalidraw.svg")]
    [InlineData("drawing.excalidraw.svg", "drawing", ".excalidraw.svg")]
    [InlineData("DRAWING.EXCALIDRAW.SVG", "DRAWING", ".excalidraw.svg")]
    [InlineData("image.PNG", "image", ".png")]
    [InlineData("archive.tar.gz", "archive.tar", ".gz")]
    [InlineData("directory/file.md", "file", ".md")]
    [InlineData("directory/file.MD", "file", ".md")]
    [InlineData("directory/file.excalidraw.svg", "file", ".excalidraw.svg")]
    [InlineData("directory/drawing.excalidraw.svg", "drawing", ".excalidraw.svg")]
    [InlineData("directory/DRAWING.EXCALIDRAW.SVG", "DRAWING", ".excalidraw.svg")]
    [InlineData("directory/image.PNG", "image", ".png")]
    [InlineData("directory/archive.tar.gz", "archive.tar", ".gz")]
    public void GetFileNameAndExtension_ReturnsExpectedResult(string fileName, string expectedName, string expectedExtension)
    {
        var (name, extension) = WikiPathHelper.GetFileNameAndExtension(fileName);
        Assert.Equal(expectedName, name);
        Assert.Equal(expectedExtension, extension);
    }

    [Theory]
    [InlineData("image.png", "00000000000000000000000000000001", "image_00000000000000000000000000000001.png")]
    [InlineData("My Photo.jpg", "00000000000000000000000000000002", "My-Photo_00000000000000000000000000000002.jpg")]
    [InlineData("drawing.excalidraw.svg", "00000000000000000000000000000003", "drawing_00000000000000000000000000000003.excalidraw.svg")]
    public void GenerateSafeFileName_ReturnsExpectedResult(string originalFileName, string temporaryId, string expected)
    {
        var mediaInfo = new TemporaryMediaInfo
        {
            TemporaryId = temporaryId,
            OriginalFileName = originalFileName,
            FilePath = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = WikiPathHelper.GenerateSafeFileName(mediaInfo);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("image_00000000000000000000000000000001.png", "image.png")]
    [InlineData("drawing_00000000000000000000000000000003.excalidraw.svg", "drawing.excalidraw.svg")]
    [InlineData("image.png", "image.png")]
    [InlineData("no_guid_suffix.png", "no_guid_suffix.png")]
    [InlineData("directory/image_00000000000000000000000000000001.png", "image.png")]
    [InlineData("directory/drawing_00000000000000000000000000000003.excalidraw.svg", "drawing.excalidraw.svg")]
    [InlineData("directory/image.png", "image.png")]
    [InlineData("directory/no_guid_suffix.png", "no_guid_suffix.png")]
    public void GetOriginalFileName_ReturnsExpectedResult(string mediaPath, string expected)
    {
        var result = WikiPathHelper.GetOriginalFileName(mediaPath);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GenerateSafeFileName_RoundTrip_GetOriginalFileName()
    {
        var originalFileName = "My Image.png";
        var mediaInfo = new TemporaryMediaInfo
        {
            TemporaryId = Guid.NewGuid().ToString("N"),
            OriginalFileName = originalFileName,
            FilePath = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var safeName = WikiPathHelper.GenerateSafeFileName(mediaInfo);
        var recovered = WikiPathHelper.GetOriginalFileName(safeName);

        Assert.Equal("My-Image.png", recovered);
    }

    [Theory]
    [InlineData("drawing.excalidraw.svg")]
    [InlineData("drawing_00000000000000000000000000000003.excalidraw.svg")]
    [InlineData("directory/drawing_00000000000000000000000000000003.excalidraw.svg")]
    public void IsEditableMedia_Editable_ReturnsTrue(string mediaPath)
    {
        Assert.True(WikiPathHelper.IsEditableMedia(mediaPath));
    }

    [Theory]
    [InlineData("image_00000000000000000000000000000001.png")]
    [InlineData("image.png")]
    [InlineData("no_guid_suffix.png")]
    [InlineData("directory/image_00000000000000000000000000000001.png")]
    [InlineData("directory/image.png")]
    [InlineData("directory/no_guid_suffix.png")]
    public void IsEditableMedia_NotEditable_ReturnsFalse(string mediaPath)
    {
        Assert.False(WikiPathHelper.IsEditableMedia(mediaPath));
    }
}
