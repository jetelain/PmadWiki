using Pmad.Wiki.Helpers;

namespace Pmad.Wiki.Test.Helpers;

public class MarkdownTitleExtractorTest
{
    [Fact]
    public void ExtractFirstTitle_WithH1_ReturnsTitle()
    {
        // Arrange
        var markdown = "# Welcome to My Page\n\nThis is the content.";

        // Act
        var title = MarkdownTitleExtractor.ExtractFirstTitle(markdown, "Fallback");

        // Assert
        Assert.Equal("Welcome to My Page", title);
    }

    [Fact]
    public void ExtractFirstTitle_WithMultipleH1_ReturnsFirst()
    {
        // Arrange
        var markdown = @"# First Title

Some content here.

# Second Title

More content.";

        // Act
        var title = MarkdownTitleExtractor.ExtractFirstTitle(markdown, "Fallback");

        // Assert
        Assert.Equal("First Title", title);
    }

    [Fact]
    public void ExtractFirstTitle_WithNoH1_ReturnsFallback()
    {
        // Arrange
        var markdown = "## This is H2\n\nSome content.";

        // Act
        var title = MarkdownTitleExtractor.ExtractFirstTitle(markdown, "MyPage");

        // Assert
        Assert.Equal("MyPage", title);
    }

    [Fact]
    public void ExtractFirstTitle_WithEmptyContent_ReturnsFallback()
    {
        // Arrange
        var markdown = "";

        // Act
        var title = MarkdownTitleExtractor.ExtractFirstTitle(markdown, "MyPage");

        // Assert
        Assert.Equal("MyPage", title);
    }

    [Fact]
    public void ExtractFirstTitle_WithWhitespaceOnly_ReturnsFallback()
    {
        // Arrange
        var markdown = "   \n  \n  ";

        // Act
        var title = MarkdownTitleExtractor.ExtractFirstTitle(markdown, "MyPage");

        // Assert
        Assert.Equal("MyPage", title);
    }

    [Fact]
    public void ExtractFirstTitle_WithH1AfterContent_ReturnsTitle()
    {
        // Arrange
        var markdown = @"Some preamble text

# The Actual Title

Content follows.";

        // Act
        var title = MarkdownTitleExtractor.ExtractFirstTitle(markdown, "Fallback");

        // Assert
        Assert.Equal("The Actual Title", title);
    }

    [Fact]
    public void ExtractFirstTitle_WithExtraSpaces_TrimsWhitespace()
    {
        // Arrange
        var markdown = "#   Title With Spaces   \n\nContent.";

        // Act
        var title = MarkdownTitleExtractor.ExtractFirstTitle(markdown, "Fallback");

        // Assert
        Assert.Equal("Title With Spaces", title);
    }

    [Fact]
    public void ExtractFirstTitle_WithSpecialCharacters_PreservesCharacters()
    {
        // Arrange
        var markdown = "# Welcome to C# & .NET Wiki!\n\nContent.";

        // Act
        var title = MarkdownTitleExtractor.ExtractFirstTitle(markdown, "Fallback");

        // Assert
        Assert.Equal("Welcome to C# & .NET Wiki!", title);
    }

    [Fact]
    public void ExtractFirstTitle_WithMarkdownInTitle_PreservesMarkdown()
    {
        // Arrange
        var markdown = "# Welcome to `Code` and **Bold**\n\nContent.";

        // Act
        var title = MarkdownTitleExtractor.ExtractFirstTitle(markdown, "Fallback");

        // Assert
        Assert.Equal("Welcome to `Code` and **Bold**", title);
    }
}
