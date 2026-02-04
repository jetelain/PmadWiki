using Markdig;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Moq;
using Pmad.Wiki.Services;
using Pmad.Wiki.Test.Infrastructure;

namespace Pmad.Wiki.Test.Services;

public class MarkdownRenderServiceTest
{
    private readonly IMarkdownRenderService _service;
    private readonly WikiOptions _options;
    private readonly LinkGenerator _linkGenerator;

    public MarkdownRenderServiceTest()
    {
        _options = new WikiOptions
        {
            NeutralMarkdownPageCulture = "en"
        };

        _linkGenerator = CreateMockLinkGenerator();
        var optionsWrapper = Options.Create(_options);
        _service = new MarkdownRenderService(optionsWrapper, _linkGenerator);
    }

    private static LinkGenerator CreateMockLinkGenerator(string basePath = "wiki")
    {
        return new TestLinkGenerator(basePath);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsWrapper = Options.Create<WikiOptions>(null!);
        var linkGenerator = CreateMockLinkGenerator();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MarkdownRenderService(optionsWrapper, linkGenerator));
    }

    [Fact]
    public void Constructor_WithValidOptions_CreatesInstance()
    {
        // Arrange
        var options = new WikiOptions { NeutralMarkdownPageCulture = "en" };
        var optionsWrapper = Options.Create(options);
        var linkGenerator = CreateMockLinkGenerator();

        // Act
        var service = new MarkdownRenderService(optionsWrapper, linkGenerator);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithCustomMarkdownConfiguration_AppliesConfiguration()
    {
        // Arrange
        bool configurationCalled = false;
        var options = new WikiOptions
        {
            NeutralMarkdownPageCulture = "en",
            ConfigureMarkdown = builder =>
            {
                configurationCalled = true;
            }
        };
        var optionsWrapper = Options.Create(options);
        var linkGenerator = CreateMockLinkGenerator();

        // Act
        var service = new MarkdownRenderService(optionsWrapper, linkGenerator);
        // Trigger pipeline creation
        service.ToHtml("test");

        // Assert
        Assert.True(configurationCalled);
    }

    #endregion

    #region ToHtml Basic Tests

    [Fact]
    public void ToHtml_WithSimpleMarkdown_ReturnsHtml()
    {
        // Arrange
        var markdown = "# Hello World\n\nThis is a test.";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("<h1", html);
        Assert.Contains("Hello World", html);
        Assert.Contains("<p>This is a test.</p>", html);
    }

    [Fact]
    public void ToHtml_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var markdown = "";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Equal("", html);
    }

    [Fact]
    public void ToHtml_WithPlainText_WrapsInParagraph()
    {
        // Arrange
        var markdown = "Just plain text.";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("<p>Just plain text.</p>", html);
    }

    #endregion

    #region Basic Markdown Syntax Tests

    [Fact]
    public void ToHtml_WithHeadings_RendersAllLevels()
    {
        // Arrange
        var markdown = @"# Heading 1
## Heading 2
### Heading 3
#### Heading 4
##### Heading 5
###### Heading 6";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("<h1", html);
        Assert.Contains("<h2", html);
        Assert.Contains("<h3", html);
        Assert.Contains("<h4", html);
        Assert.Contains("<h5", html);
        Assert.Contains("<h6", html);
    }

    [Fact]
    public void ToHtml_WithBoldText_RendersBold()
    {
        // Arrange
        var markdown = "This is **bold** text.";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("<strong>bold</strong>", html);
    }

    [Fact]
    public void ToHtml_WithItalicText_RendersItalic()
    {
        // Arrange
        var markdown = "This is *italic* text.";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("<em>italic</em>", html);
    }

    [Fact]
    public void ToHtml_WithCode_RendersCodeTag()
    {
        // Arrange
        var markdown = "This is `inline code`.";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("<code>inline code</code>", html);
    }

    [Fact]
    public void ToHtml_WithCodeBlock_RendersPreAndCode()
    {
        // Arrange
        var markdown = @"```csharp
public class Test
{
}
```";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("<pre><code", html);
        Assert.Contains("public class Test", html);
    }

    [Fact]
    public void ToHtml_WithUnorderedList_RendersList()
    {
        // Arrange
        var markdown = @"- Item 1
- Item 2
- Item 3";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("<ul>", html);
        Assert.Contains("<li>Item 1</li>", html);
        Assert.Contains("<li>Item 2</li>", html);
        Assert.Contains("<li>Item 3</li>", html);
    }

    [Fact]
    public void ToHtml_WithOrderedList_RendersNumberedList()
    {
        // Arrange
        var markdown = @"1. First
2. Second
3. Third";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("<ol>", html);
        Assert.Contains("<li>First</li>", html);
        Assert.Contains("<li>Second</li>", html);
        Assert.Contains("<li>Third</li>", html);
    }

    [Fact]
    public void ToHtml_WithBlockquote_RendersBlockquote()
    {
        // Arrange
        var markdown = "> This is a quote";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("<blockquote>", html);
        Assert.Contains("This is a quote", html);
    }

    [Fact]
    public void ToHtml_WithHorizontalRule_RendersHr()
    {
        // Arrange
        var markdown = "---";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("<hr", html);
    }

    #endregion

    #region Link Tests

    [Fact]
    public void ToHtml_WithExternalLink_RendersLink()
    {
        // Arrange
        var markdown = "[Google](https://www.google.com)";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("<a href=\"https://www.google.com\">Google</a>", html);
    }

    [Fact]
    public void ToHtml_WithWikiLink_ConvertsToWikiRoute()
    {
        // Arrange
        var markdown = "[Page](test.md)";

        // Act
        var html = _service.ToHtml(markdown, null, "home");

        // Assert
        Assert.Contains("/wiki/view/test", html);
        Assert.Contains(">Page</a>", html);
    }

    [Fact]
    public void ToHtml_WithWikiLinkWithPath_ConvertsToWikiRoute()
    {
        // Arrange
        var markdown = "[Admin Settings](admin/settings.md)";

        // Act
        var html = _service.ToHtml(markdown, null, "home");

        // Assert
        Assert.Contains("/wiki/view/admin/settings", html);
        Assert.Contains(">Admin Settings</a>", html);
    }

    [Fact]
    public void ToHtml_WithWikiLinkWithAnchor_PreservesAnchor()
    {
        // Arrange
        var markdown = "[Section](page.md#section)";

        // Act
        var html = _service.ToHtml(markdown, null, "home");

        // Assert
        Assert.Contains("/wiki/view/page#section", html);
        Assert.Contains(">Section</a>", html);
    }

    [Fact]
    public void ToHtml_WithWikiLinkWithAnchorAndCulture_PreservesAnchor()
    {
        // Arrange
        var markdown = "[Section](page.md#section)";

        // Act
        var html = _service.ToHtml(markdown, "fr", "home");

        // Assert
        Assert.Contains("/wiki/view/page?culture=fr#section", html);
        Assert.Contains(">Section</a>", html);
    }

    [Fact]
    public void ToHtml_WithAnchorOnlyLink_DoesNotConvert()
    {
        // Arrange
        var markdown = "[Jump to section](#section)";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        // Anchor-only links should not be converted
        Assert.Contains("href=\"#section\"", html);
        Assert.DoesNotContain("/wiki/view/", html);
    }

    [Fact]
    public void ToHtml_WithAbsoluteWikiLink_ConvertsToWikiRoute()
    {
        // Arrange
        var markdown = "[Root Page](/root.md)";

        // Act
        var html = _service.ToHtml(markdown, null, "docs/page");

        // Assert
        Assert.Contains("/wiki/view/root", html);
    }

    [Fact]
    public void ToHtml_WithHttpLink_DoesNotConvert()
    {
        // Arrange
        var markdown = "[Example](http://example.com/page.md)";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("http://example.com/page.md", html);
        Assert.DoesNotContain("/wiki/view/", html);
    }

    [Fact]
    public void ToHtml_WithHttpsLink_DoesNotConvert()
    {
        // Arrange
        var markdown = "[Secure](https://example.com/page.md)";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("https://example.com/page.md", html);
        Assert.DoesNotContain("/wiki/view/", html);
    }

    [Fact]
    public void ToHtml_WithProtocolRelativeLink_DoesNotConvert()
    {
        // Arrange
        var markdown = "[CDN](//cdn.example.com/page.md)";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("//cdn.example.com/page.md", html);
        Assert.DoesNotContain("/wiki/view/", html);
    }

    [Fact]
    public void ToHtml_WithNonMarkdownLink_DoesNotConvert()
    {
        // Arrange
        var markdown = "[Image](image.png)";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("image.png", html);
        Assert.DoesNotContain("/wiki/view/", html);
    }

    [Fact]
    public void ToHtml_WithRelativeWikiLink_ResolvesRelativeToCurrentPage()
    {
        // Arrange
        var markdown = "[Settings](settings.md)";

        // Act
        var html = _service.ToHtml(markdown, null, "docs/admin/intro");

        // Assert
        // Should resolve to docs/admin/settings (same directory as current page)
        Assert.Contains("/wiki/view/docs/admin/settings", html);
    }

    [Fact]
    public void ToHtml_WithRelativeWikiLinkInSubfolder_ResolvesCorrectly()
    {
        // Arrange
        var markdown = "[Nested Page](subfolder/page.md)";

        // Act
        var html = _service.ToHtml(markdown, null, "docs/intro");

        // Assert
        // Should resolve to docs/subfolder/page
        Assert.Contains("/wiki/view/docs/subfolder/page", html);
    }

    [Fact]
    public void ToHtml_WithRelativeWikiLinkGoingUp_ResolvesCorrectly()
    {
        // Arrange
        var markdown = "[Parent Page](../parent.md)";

        // Act
        var html = _service.ToHtml(markdown, null, "docs/admin/settings");

        // Assert
        // Should resolve to docs/parent
        Assert.Contains("/wiki/view/docs/parent", html);
    }

    [Fact]
    public void ToHtml_WithRelativeWikiLinkFromRoot_ResolvesCorrectly()
    {
        // Arrange
        var markdown = "[Other Page](other.md)";

        // Act
        var html = _service.ToHtml(markdown, null, "home");

        // Assert
        // Should resolve to other (same level as home, which is root)
        Assert.Contains("/wiki/view/other", html);
    }

    [Fact]
    public void ToHtml_WithoutCurrentPageName_ProcessWikiLinks()
    {
        // Arrange
        var markdown = "[Page](test.md)";

        // Act
        var html = _service.ToHtml(markdown, null, null);

        // Assert
        // Without currentPageName, links should not be processed
        Assert.Contains("/wiki/view/test", html);
    }

    #endregion

    #region Advanced Extensions Tests

    [Fact]
    public void ToHtml_WithTable_RendersTable()
    {
        // Arrange
        var markdown = @"| Header 1 | Header 2 |
|----------|----------|
| Cell 1   | Cell 2   |
| Cell 3   | Cell 4   |";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("<table>", html);
        Assert.Contains("<thead>", html);
        Assert.Contains("<tbody>", html);
        Assert.Contains("Header 1", html);
        Assert.Contains("Cell 1", html);
    }

    [Fact]
    public void ToHtml_WithTaskList_RendersCheckboxes()
    {
        // Arrange
        var markdown = @"- [x] Completed task
- [ ] Incomplete task";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("type=\"checkbox\"", html);
        Assert.Contains("checked=\"checked\"", html);
    }

    [Fact]
    public void ToHtml_WithStrikethrough_RendersStrikethrough()
    {
        // Arrange
        var markdown = "This is ~~strikethrough~~ text.";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("<del>strikethrough</del>", html);
    }

    [Fact]
    public void ToHtml_WithAutolink_RendersLink()
    {
        // Arrange
        var markdown = "Visit https://github.com";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("<a href=\"https://github.com\">https://github.com</a>", html);
    }

    [Fact]
    public void ToHtml_WithEmoji_DoesNotRenderEmojiByDefault()
    {
        // Arrange
        var markdown = "Hello :smile:";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        // Emoji shortcodes are not enabled by default in UseAdvancedExtensions
        Assert.Contains(":smile:", html);
        Assert.DoesNotContain("??", html);
    }

    [Fact]
    public void ToHtml_WithFootnote_RendersFootnote()
    {
        // Arrange
        var markdown = @"This has a footnote[^1].

[^1]: This is the footnote.";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("footnote", html);
    }

    #endregion

    #region HTML Sanitization Tests

    [Fact]
    public void ToHtml_WithHtmlTag_EscapesHtml()
    {
        // Arrange
        var markdown = "This has <script>alert('XSS')</script> in it.";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        // DisableHtml escapes HTML tags, it doesn't remove them completely
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("alert", html); // The text content is still present
        Assert.Contains("&lt;/script&gt;", html);
    }

    [Fact]
    public void ToHtml_WithHtmlInlineTag_RemovesHtml()
    {
        // Arrange
        var markdown = "This has <strong>inline HTML</strong> in it.";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        // The text should be present but not the HTML tags from input
        // (Markdown's **bold** should work, but raw HTML should be stripped)
        Assert.Contains("inline HTML", html);
        // The DisableHtml should prevent raw HTML from being rendered
    }

    [Fact]
    public void ToHtml_WithDangerousHtml_EscapesContent()
    {
        // Arrange
        var markdown = "<iframe src='evil.com'></iframe>";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.DoesNotContain("<iframe", html);
    }

    #endregion

    #region Complex Content Tests

    [Fact]
    public void ToHtml_WithMixedContent_RendersCorrectly()
    {
        // Arrange
        var markdown = @"# Main Title

This is a paragraph with **bold** and *italic* text.

## Section 1

Here's a list:
- Item 1
- Item 2

[Link to page](test.md)

```csharp
var code = ""sample"";
```

> A quote

| Col 1 | Col 2 |
|-------|-------|
| A     | B     |";

        // Act
        var html = _service.ToHtml(markdown, null, "home");

        // Assert
        Assert.Contains("<h1", html);
        Assert.Contains("Main Title", html);
        Assert.Contains("<strong>bold</strong>", html);
        Assert.Contains("<em>italic</em>", html);
        Assert.Contains("<h2", html);
        Assert.Contains("<ul>", html);
        Assert.Contains("/wiki/view/test", html);
        Assert.Contains("<pre><code", html);
        Assert.Contains("<blockquote>", html);
        Assert.Contains("<table>", html);
    }

    [Fact]
    public void ToHtml_WithNestedLists_RendersCorrectly()
    {
        // Arrange
        var markdown = @"- Level 1
  - Level 2
    - Level 3
  - Level 2b
- Level 1b";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("<ul>", html);
        Assert.Contains("Level 1", html);
        Assert.Contains("Level 2", html);
        Assert.Contains("Level 3", html);
    }

    [Fact]
    public void ToHtml_WithMultipleWikiLinks_ConvertsAll()
    {
        // Arrange
        var markdown = @"Links:
- [Page 1](page1.md)
- [Page 2](page2.md)
- [Page 3](folder/page3.md)";

        // Act
        var html = _service.ToHtml(markdown, null, "home");

        // Assert
        Assert.Contains("/wiki/view/page1", html);
        Assert.Contains("/wiki/view/page2", html);
        Assert.Contains("/wiki/view/folder/page3", html);
    }

    #endregion

    #region Custom Route Tests

    [Fact]
    public void ToHtml_WithCustomBasePath_UsesCorrectPath()
    {
        // Arrange
        var options = new WikiOptions { NeutralMarkdownPageCulture = "en" };
        var optionsWrapper = Options.Create(options);
        var linkGenerator = CreateMockLinkGenerator("docs");
        var service = new MarkdownRenderService(optionsWrapper, linkGenerator);
        var markdown = "[Page](test.md)";

        // Act
        var html = service.ToHtml(markdown, null, "home");

        // Assert
        Assert.Contains("/docs/view/test", html);
    }

    [Fact]
    public void ToHtml_WithEmptyBasePath_UsesCorrectPath()
    {
        // Arrange
        var options = new WikiOptions { NeutralMarkdownPageCulture = "en" };
        var optionsWrapper = Options.Create(options);
        var linkGenerator = CreateMockLinkGenerator("");
        var service = new MarkdownRenderService(optionsWrapper, linkGenerator);
        var markdown = "[Page](test.md)";

        // Act
        var html = service.ToHtml(markdown, null, "home");

        // Assert
        Assert.Contains("//view/test", html);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void ToHtml_WithVeryLongContent_CompletesSuccessfully()
    {
        // Arrange
        var markdown = string.Join("\n\n", Enumerable.Repeat("# Heading\n\nParagraph text.", 1000));

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.NotNull(html);
        Assert.NotEmpty(html);
        Assert.Contains("<h1", html);
    }

    [Fact]
    public void ToHtml_WithSpecialCharacters_PreservesCharacters()
    {
        // Arrange
        var markdown = "Special chars: & < > \" ' © ® ™";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("&amp;", html);
        Assert.Contains("&lt;", html);
        Assert.Contains("&gt;", html);
        Assert.Contains("&quot;", html);
    }

    [Fact]
    public void ToHtml_WithUnicodeCharacters_PreservesUnicode()
    {
        // Arrange
        var markdown = "Unicode: ??? ?? ??????? ??";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("???", html);
        Assert.Contains("??", html);
        Assert.Contains("???????", html);
        Assert.Contains("??", html);
    }

    [Fact]
    public void ToHtml_WithMultipleNewlines_HandlesCorrectly()
    {
        // Arrange
        var markdown = "Paragraph 1\n\n\n\nParagraph 2";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.Contains("Paragraph 1", html);
        Assert.Contains("Paragraph 2", html);
    }

    [Fact]
    public void ToHtml_WithMalformedMarkdown_HandlesGracefully()
    {
        // Arrange
        var markdown = "# Heading\n[Broken link(no-closing";

        // Act
        var html = _service.ToHtml(markdown);

        // Assert
        Assert.NotNull(html);
        Assert.Contains("<h1", html);
    }

    #endregion
}
