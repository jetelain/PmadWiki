using Pmad.Wiki.Helpers;

namespace Pmad.Wiki.Test.Helpers;

public class WikiPageFrontMatterParserTest
{
    [Fact]
    public void Parse_WithNoFrontMatter_ReturnsEmptyObjectAndFullContent()
    {
        // Arrange
        var content = "# Test Page\n\nThis is the content.";

        // Act
        var (frontMatter, parsedContent, rawContent) = WikiPageFrontMatterParser.Parse(content);

        // Assert
        Assert.Null(frontMatter.Title);
        Assert.False(frontMatter.ShowSubPages);
        Assert.False(frontMatter.SubPagesRecursive);
        Assert.Equal(content, parsedContent);
        Assert.Equal(content, rawContent);
    }

    [Fact]
    public void Parse_WithTitleInFrontMatter_ParsesTitle()
    {
        // Arrange
        var content = @"---
title: My Page
---
# Content";

        // Act
        var (frontMatter, parsedContent, rawContent) = WikiPageFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("My Page", frontMatter.Title);
        Assert.Equal("# Content", parsedContent);
        Assert.Equal(content, rawContent);
    }

    [Fact]
    public void Parse_WithEmptyTitle_NormalizesTitleToNull()
    {
        // Arrange
        var content = @"---
title: 
---
Content";

        // Act
        var (frontMatter, parsedContent, rawContent) = WikiPageFrontMatterParser.Parse(content);

        // Assert
        Assert.Null(frontMatter.Title);
        Assert.Equal("Content", parsedContent);
        Assert.Equal(content, rawContent);
    }

    [Fact]
    public void Parse_WithShowSubPagesTrue_ParsesShowSubPages()
    {
        // Arrange
        var content = @"---
showSubPages: true
---
Content";

        // Act
        var (frontMatter, parsedContent, rawContent) = WikiPageFrontMatterParser.Parse(content);

        // Assert
        Assert.True(frontMatter.ShowSubPages);
        Assert.Equal("Content", parsedContent);
        Assert.Equal(content, rawContent);
    }

    [Fact]
    public void Parse_WithSubPagesRecursiveTrue_ParsesSubPagesRecursive()
    {
        // Arrange
        var content = @"---
showSubPages: true
subPagesRecursive: true
---
Content";

        // Act
        var (frontMatter, parsedContent, rawContent) = WikiPageFrontMatterParser.Parse(content);

        // Assert
        Assert.True(frontMatter.ShowSubPages);
        Assert.True(frontMatter.SubPagesRecursive);
        Assert.Equal(content, rawContent);
    }

    [Fact]
    public void Parse_WithAllProperties_ParsesCorrectly()
    {
        // Arrange
        var content = @"---
title: My Wiki Page
showSubPages: true
subPagesRecursive: true
---
# Content

Some text here.";

        // Act
        var (frontMatter, parsedContent, rawContent) = WikiPageFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("My Wiki Page", frontMatter.Title);
        Assert.True(frontMatter.ShowSubPages);
        Assert.True(frontMatter.SubPagesRecursive);
        Assert.Equal("# Content\n\nSome text here.", parsedContent.ReplaceLineEndings("\n"));
        Assert.Equal(content, rawContent);
    }

    [Fact]
    public void Parse_WithOnlyFrontMatter_ReturnsEmptyContent()
    {
        // Arrange
        var content = @"---
title: Test
---
";

        // Act
        var (frontMatter, parsedContent, rawContent) = WikiPageFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("Test", frontMatter.Title);
        Assert.Equal(string.Empty, parsedContent);
        Assert.Equal(content, rawContent);
    }

    [Fact]
    public void Parse_WithWindowsLineEndings_ParsesCorrectly()
    {
        // Arrange
        var content = "---\r\ntitle: Test\r\nshowSubPages: true\r\n---\r\nContent";

        // Act
        var (frontMatter, parsedContent, rawContent) = WikiPageFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("Test", frontMatter.Title);
        Assert.True(frontMatter.ShowSubPages);
        Assert.Equal(content, rawContent);
    }

    [Fact]
    public void Parse_WithContentContainingTripleDashes_OnlyParsesFirstBlock()
    {
        // Arrange
        var content = @"---
title: Test
---
# Content

---
More content";

        // Act
        var (frontMatter, parsedContent, rawContent) = WikiPageFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("Test", frontMatter.Title);
        Assert.Contains("---", parsedContent);
        Assert.Contains("More content", parsedContent);
        Assert.Equal(content, rawContent);
    }

    [Fact]
    public void Parse_WithQuotedTitle_ParsesCorrectly()
    {
        // Arrange
        var content = @"---
title: ""My Page: With Special Characters""
---
Content";

        // Act
        var (frontMatter, parsedContent, rawContent) = WikiPageFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("My Page: With Special Characters", frontMatter.Title);
        Assert.Equal(content, rawContent);
    }

    [Fact]
    public void Parse_WithShowSubPagesFalse_DefaultsToFalse()
    {
        // Arrange
        var content = @"---
title: Test
showSubPages: false
---
Content";

        // Act
        var (frontMatter, parsedContent, rawContent) = WikiPageFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("Test", frontMatter.Title);
        Assert.False(frontMatter.ShowSubPages);
        Assert.False(frontMatter.SubPagesRecursive);
        Assert.Equal(content, rawContent);
    }

    [Fact]
    public void Parse_ReturnsWikiPageContent_WithCorrectRecordValues()
    {
        // Arrange
        var content = @"---
title: Test
---
Body";

        // Act
        var result = WikiPageFrontMatterParser.Parse(content);

        // Assert
        Assert.NotNull(result.FrontMatter);
        Assert.Equal("Test", result.FrontMatter.Title);
        Assert.Equal("Body", result.ContentWithoutFrontMatter);
        Assert.Equal(content, result.RawContent);
    }
}
