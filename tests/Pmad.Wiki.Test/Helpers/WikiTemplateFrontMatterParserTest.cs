using Pmad.Wiki.Helpers;

namespace Pmad.Wiki.Test.Helpers;

public class WikiTemplateFrontMatterParserTest
{
    [Fact]
    public void Parse_WithNoFrontMatter_ReturnsEmptyObjectAndFullContent()
    {
        // Arrange
        var content = "# Test Page\n\nThis is the content.";

        // Act
        var (frontMatter, parsedContent) = WikiTemplateFrontMatterParser.Parse(content);

        // Assert
        Assert.Null(frontMatter.Title);
        Assert.Null(frontMatter.Description);
        Assert.Null(frontMatter.Location);
        Assert.Null(frontMatter.Pattern);
        Assert.Equal(content, parsedContent);
    }

    [Fact]
    public void Parse_WithSimpleFrontMatter_ParsesProperties()
    {
        // Arrange
        var content = @"---
title: My Page
description: A test page
---
# Content";

        // Act
        var (frontMatter, parsedContent) = WikiTemplateFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("My Page", frontMatter.Title);
        Assert.Equal("A test page", frontMatter.Description);
        Assert.Equal("# Content", parsedContent);
    }

    [Fact]
    public void Parse_WithQuotedValues_ParsesCorrectly()
    {
        // Arrange
        var content = @"---
title: ""My Page: With Special Characters""
description: 'Single quoted value'
---
Content";

        // Act
        var (frontMatter, parsedContent) = WikiTemplateFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("My Page: With Special Characters", frontMatter.Title);
        Assert.Equal("Single quoted value", frontMatter.Description);
    }

    [Fact]
    public void Parse_WithMultilineValues_ParsesCorrectly()
    {
        // Arrange
        var content = @"---
title: Page Title
description: |
  This is a multiline
  description with
  multiple lines
---
Content";

        // Act
        var (frontMatter, parsedContent) = WikiTemplateFrontMatterParser.Parse(content);

        // Assert
        Assert.NotNull(frontMatter.Description);
        Assert.Contains("multiline", frontMatter.Description);
        Assert.Contains("multiple lines", frontMatter.Description);
    }

    [Fact]
    public void Parse_WithEmptyValue_ParsesAsNull()
    {
        // Arrange
        var content = @"---
title: 
description: Test
---
Content";

        // Act
        var (frontMatter, parsedContent) = WikiTemplateFrontMatterParser.Parse(content);

        // Assert
        Assert.Null(frontMatter.Title);
        Assert.Equal("Test", frontMatter.Description);
    }

    [Fact]
    public void Parse_WithSpecialCharactersInValues_ParsesCorrectly()
    {
        // Arrange
        var content = @"---
title: Test Page
location: /docs
pattern: page-{date}
---
Content";

        // Act
        var (frontMatter, parsedContent) = WikiTemplateFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("Test Page", frontMatter.Title);
        Assert.Equal("/docs", frontMatter.Location);
        Assert.Equal("page-{date}", frontMatter.Pattern);
    }

    [Fact]
    public void Parse_WithColonsInValues_ParsesCorrectly()
    {
        // Arrange
        var content = @"---
pattern: ""{year}:{month}:{day}""
---
Content";

        // Act
        var (frontMatter, parsedContent) = WikiTemplateFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("{year}:{month}:{day}", frontMatter.Pattern);
    }

    [Fact]
    public void Parse_WithMalformedYAML_ReturnsEmptyFrontMatterAndPreservesContent()
    {
        // Arrange
        var content = @"---
this is not: valid: yaml: at: all
---
# Content";

        // Act
        var (frontMatter, parsedContent) = WikiTemplateFrontMatterParser.Parse(content);

        // Assert
        Assert.Null(frontMatter.Title);
        Assert.Null(frontMatter.Description);
        Assert.Equal("# Content", parsedContent);
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
        var (frontMatter, parsedContent) = WikiTemplateFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("Test", frontMatter.Title);
        Assert.Equal(string.Empty, parsedContent);
    }

    [Fact]
    public void Parse_WithContentContainingTripleDashes_OnlyParsesFirstBlock()
    {
        // Arrange
        var content = @"---
title: Test
---
# Content

Some text with separator
---
More content";

        // Act
        var (frontMatter, parsedContent) = WikiTemplateFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("Test", frontMatter.Title);
        Assert.Contains("---", parsedContent);
        Assert.Contains("More content", parsedContent);
    }

    [Fact]
    public void Parse_WithWindowsLineEndings_ParsesCorrectly()
    {
        // Arrange
        var content = "---\r\ntitle: Test\r\ndescription: Value\r\n---\r\nContent";

        // Act
        var (frontMatter, parsedContent) = WikiTemplateFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("Test", frontMatter.Title);
        Assert.Equal("Value", frontMatter.Description);
    }

    [Fact]
    public void Parse_WithUnixLineEndings_ParsesCorrectly()
    {
        // Arrange
        var content = "---\ntitle: Test\ndescription: Value\n---\nContent";

        // Act
        var (frontMatter, parsedContent) = WikiTemplateFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("Test", frontMatter.Title);
        Assert.Equal("Value", frontMatter.Description);
    }

    [Fact]
    public void Parse_WithWhitespaceAfterDelimiters_ParsesCorrectly()
    {
        // Arrange
        var content = "---  \ntitle: Test\n---  \nContent";

        // Act
        var (frontMatter, parsedContent) = WikiTemplateFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("Test", frontMatter.Title);
    }

    [Fact]
    public void Parse_RealWorldTemplateExample_ParsesCorrectly()
    {
        // Arrange
        var content = @"---
title: Blog Post Template
description: Template for creating new blog posts
pattern: blog/{year}/{month}/{day}/{title}
location: blog/posts
tags:
  - blog
  - template
  - article
author: Wiki System
version: 1.0
---
# {title}

**Published:** {date}
**Author:** [Your Name]

## Summary

Write a brief summary here.

## Content

Start writing your blog post content here...

## Tags

- Technology
- Tutorial";

        // Act
        var (frontMatter, parsedContent) = WikiTemplateFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("Blog Post Template", frontMatter.Title);
        Assert.Equal("Template for creating new blog posts", frontMatter.Description);
        Assert.Equal("blog/{year}/{month}/{day}/{title}", frontMatter.Pattern);
        Assert.Equal("blog/posts", frontMatter.Location);
        Assert.Contains("# {title}", parsedContent);
        Assert.Contains("Start writing your blog post", parsedContent);
    }

    [Fact]
    public void Parse_WithDateInFrontMatter_ParsesAsString()
    {
        // Arrange
        var content = @"---
title: Test
---
Content";

        // Act
        var (frontMatter, parsedContent) = WikiTemplateFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("Test", frontMatter.Title);
    }

    [Fact]
    public void Parse_WithNullValues_ConvertsToNull()
    {
        // Arrange
        var content = @"---
title: Test
author: null
description: null
---
Content";

        // Act
        var (frontMatter, parsedContent) = WikiTemplateFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("Test", frontMatter.Title);
        // null value should be converted to null in the object
        Assert.Null(frontMatter.Description);
    }

    [Fact]
    public void Parse_WithAllProperties_ParsesCorrectly()
    {
        // Arrange
        var content = @"---
title: Complete Template
description: Full example
location: templates
pattern: '{year}/{month}/{title}'
---
Content";

        // Act
        var (frontMatter, parsedContent) = WikiTemplateFrontMatterParser.Parse(content);

        // Assert
        Assert.Equal("Complete Template", frontMatter.Title);
        Assert.Equal("Full example", frontMatter.Description);
        Assert.Equal("templates", frontMatter.Location);
        Assert.Equal("{year}/{month}/{title}", frontMatter.Pattern);
    }
}
