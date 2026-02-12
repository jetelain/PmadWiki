using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pmad.Wiki.Models;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Controllers;

public class WikiController_PreviewMarkdownTests : WikiControllerTestBase
{
    #region PreviewMarkdown Action Tests

    [Fact]
    public async Task PreviewMarkdown_WithValidMarkdown_ReturnsRenderedHtml()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var request = new PreviewMarkdownRequest
        {
            Markdown = "# Test Heading\n\nSome content here.",
            PageName = "TestPage",
            Culture = null
        };

        var expectedHtml = "<h1>Test Heading</h1>\n<p>Some content here.</p>";

        _mockMarkdownRenderService
            .Setup(x => x.ToHtml("# Test Heading\n\nSome content here.", null, "TestPage"))
            .Returns(expectedHtml);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.PreviewMarkdown(request, CancellationToken.None);

        // Assert
        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal(expectedHtml, contentResult.Content);

        _mockMarkdownRenderService.Verify(
            x => x.ToHtml("# Test Heading\n\nSome content here.", null, "TestPage"),
            Times.Once);
    }

    [Fact]
    public async Task PreviewMarkdown_WithCulture_PassesCultureToRenderer()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var request = new PreviewMarkdownRequest
        {
            Markdown = "# Titre\n\nContenu en français.",
            PageName = "TestPage",
            Culture = "fr"
        };

        var expectedHtml = "<h1>Titre</h1>\n<p>Contenu en français.</p>";

        _mockMarkdownRenderService
            .Setup(x => x.ToHtml("# Titre\n\nContenu en français.", "fr", "TestPage"))
            .Returns(expectedHtml);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.PreviewMarkdown(request, CancellationToken.None);

        // Assert
        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal(expectedHtml, contentResult.Content);

        _mockMarkdownRenderService.Verify(
            x => x.ToHtml("# Titre\n\nContenu en français.", "fr", "TestPage"),
            Times.Once);
    }

    [Fact]
    public async Task PreviewMarkdown_WithEmptyMarkdown_ReturnsEmptyContent()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var request = new PreviewMarkdownRequest
        {
            Markdown = "",
            PageName = "TestPage",
            Culture = null
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.PreviewMarkdown(request, CancellationToken.None);

        // Assert
        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal(string.Empty, contentResult.Content);

        _mockMarkdownRenderService.Verify(
            x => x.ToHtml(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task PreviewMarkdown_WithNullMarkdown_ReturnsEmptyContent()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var request = new PreviewMarkdownRequest
        {
            Markdown = null!,
            PageName = "TestPage",
            Culture = null
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.PreviewMarkdown(request, CancellationToken.None);

        // Assert
        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal(string.Empty, contentResult.Content);

        _mockMarkdownRenderService.Verify(
            x => x.ToHtml(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task PreviewMarkdown_WithNullRequest_ReturnsEmptyContent()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.PreviewMarkdown(null!, CancellationToken.None);

        // Assert
        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal(string.Empty, contentResult.Content);

        _mockMarkdownRenderService.Verify(
            x => x.ToHtml(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task PreviewMarkdown_WhenUserNotAuthenticated_ReturnsForbid()
    {
        // Arrange
        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IWikiUserWithPermissions?)null);

        var request = new PreviewMarkdownRequest
        {
            Markdown = "# Test",
            PageName = "TestPage",
            Culture = null
        };

        // Act
        var result = await _controller.PreviewMarkdown(request, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);

        _mockMarkdownRenderService.Verify(
            x => x.ToHtml(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task PreviewMarkdown_WhenUserCannotEdit_ReturnsForbid()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(false);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var request = new PreviewMarkdownRequest
        {
            Markdown = "# Test",
            PageName = "TestPage",
            Culture = null
        };

        SetupUserContext("testuser");

        // Act
        var result = await _controller.PreviewMarkdown(request, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);

        _mockMarkdownRenderService.Verify(
            x => x.ToHtml(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task PreviewMarkdown_WithComplexMarkdown_ReturnsRenderedHtml()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var markdown = @"# Heading 1

## Heading 2

This is a paragraph with **bold** and *italic* text.

- List item 1
- List item 2
- List item 3

```csharp
var code = ""example"";
```

[Link text](https://example.com)";

        var expectedHtml = "<h1>Heading 1</h1>\n<h2>Heading 2</h2>\n<p>This is a paragraph with <strong>bold</strong> and <em>italic</em> text.</p>";

        var request = new PreviewMarkdownRequest
        {
            Markdown = markdown,
            PageName = "TestPage",
            Culture = null
        };

        _mockMarkdownRenderService
            .Setup(x => x.ToHtml(markdown, null, "TestPage"))
            .Returns(expectedHtml);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.PreviewMarkdown(request, CancellationToken.None);

        // Assert
        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal(expectedHtml, contentResult.Content);
    }

    [Fact]
    public async Task PreviewMarkdown_WithPageNameAndCulture_PassesBothToRenderer()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var request = new PreviewMarkdownRequest
        {
            Markdown = "# Test",
            PageName = "docs/guide",
            Culture = "de"
        };

        var expectedHtml = "<h1>Test</h1>";

        _mockMarkdownRenderService
            .Setup(x => x.ToHtml("# Test", "de", "docs/guide"))
            .Returns(expectedHtml);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.PreviewMarkdown(request, CancellationToken.None);

        // Assert
        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal(expectedHtml, contentResult.Content);

        _mockMarkdownRenderService.Verify(
            x => x.ToHtml("# Test", "de", "docs/guide"),
            Times.Once);
    }

    [Fact]
    public async Task PreviewMarkdown_WithWhitespaceOnlyMarkdown_ReturnsEmptyContent()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var request = new PreviewMarkdownRequest
        {
            Markdown = "   \n\n  \t  ",
            PageName = "TestPage",
            Culture = null
        };

        var expectedHtml = "";

        _mockMarkdownRenderService
            .Setup(x => x.ToHtml("   \n\n  \t  ", null, "TestPage"))
            .Returns(expectedHtml);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.PreviewMarkdown(request, CancellationToken.None);

        // Assert
        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal(expectedHtml, contentResult.Content);
    }

    [Fact]
    public async Task PreviewMarkdown_WithMarkdownContainingLinks_RendersCorrectly()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var markdown = "[Internal Link](./OtherPage.md)\n\n[External Link](https://example.com)";
        var expectedHtml = "<p><a href=\"/Wiki/View/OtherPage\">Internal Link</a></p>\n<p><a href=\"https://example.com\">External Link</a></p>";

        var request = new PreviewMarkdownRequest
        {
            Markdown = markdown,
            PageName = "TestPage",
            Culture = null
        };

        _mockMarkdownRenderService
            .Setup(x => x.ToHtml(markdown, null, "TestPage"))
            .Returns(expectedHtml);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.PreviewMarkdown(request, CancellationToken.None);

        // Assert
        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal(expectedHtml, contentResult.Content);
    }

    [Fact]
    public async Task PreviewMarkdown_WithMarkdownContainingImages_RendersCorrectly()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var markdown = "![Alt text](medias/image.png)";
        var expectedHtml = "<p><img src=\"/Wiki/Media/medias/image.png\" alt=\"Alt text\" /></p>";

        var request = new PreviewMarkdownRequest
        {
            Markdown = markdown,
            PageName = "TestPage",
            Culture = null
        };

        _mockMarkdownRenderService
            .Setup(x => x.ToHtml(markdown, null, "TestPage"))
            .Returns(expectedHtml);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.PreviewMarkdown(request, CancellationToken.None);

        // Assert
        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal(expectedHtml, contentResult.Content);
    }

    [Fact]
    public async Task PreviewMarkdown_WithNestedPageName_PassesCorrectPageName()
    {
        // Arrange
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanEdit).Returns(true);

        _mockUserService
            .Setup(x => x.GetWikiUser(It.IsAny<ClaimsPrincipal>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser.Object);

        var request = new PreviewMarkdownRequest
        {
            Markdown = "# Test",
            PageName = "docs/api/reference",
            Culture = null
        };

        var expectedHtml = "<h1>Test</h1>";

        _mockMarkdownRenderService
            .Setup(x => x.ToHtml("# Test", null, "docs/api/reference"))
            .Returns(expectedHtml);

        SetupUserContext("testuser");

        // Act
        var result = await _controller.PreviewMarkdown(request, CancellationToken.None);

        // Assert
        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal(expectedHtml, contentResult.Content);

        _mockMarkdownRenderService.Verify(
            x => x.ToHtml("# Test", null, "docs/api/reference"),
            Times.Once);
    }

    #endregion

}
