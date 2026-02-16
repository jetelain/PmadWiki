using Moq;
using Pmad.Wiki.Models;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Services;

public class WikiTemplateServiceTest
{
    private readonly Mock<IWikiPageService> _mockPageService;
    private readonly Mock<IWikiPagePermissionHelper> _mockPermissionHelper;
    private readonly WikiTemplateService _service;

    public WikiTemplateServiceTest()
    {
        _mockPageService = new Mock<IWikiPageService>();
        _mockPermissionHelper = new Mock<IWikiPagePermissionHelper>();

        _service = new WikiTemplateService(
            _mockPageService.Object,
            _mockPermissionHelper.Object);
    }

    #region GetAllTemplatesAsync Tests - Basic Scenarios

    [Fact]
    public async Task GetAllTemplatesAsync_WithNoAccessiblePages_ReturnsEmptyList()
    {
        // Arrange
        var mockUser = CreateMockUser();

        _mockPermissionHelper
            .Setup(x => x.GetAllAccessiblePages(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WikiPageInfo>());

        // Act
        var result = await _service.GetAllTemplatesAsync(mockUser, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllTemplatesAsync_WithNoTemplatePages_ReturnsEmptyList()
    {
        // Arrange
        var mockUser = CreateMockUser();

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Home", Title = "Home Page", Culture = null },
            new WikiPageInfo { PageName = "About", Title = "About Page", Culture = null }
        };

        _mockPermissionHelper
            .Setup(x => x.GetAllAccessiblePages(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        // Act
        var result = await _service.GetAllTemplatesAsync(mockUser, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllTemplatesAsync_WithSingleTemplate_ReturnsOneTemplate()
    {
        // Arrange
        var mockUser = CreateMockUser();

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "_templates/article", Title = "Article Template", Culture = null }
        };

        _mockPermissionHelper
            .Setup(x => x.GetAllAccessiblePages(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        var pageContent = @"---
title: Article Template
description: Template for articles
location: docs
pattern: article-{date}
---

# Article Title

Content here";

        _mockPageService
            .Setup(x => x.GetPageAsync("_templates/article", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WikiPage
            {
                PageName = "_templates/article",
                Content = pageContent,
                ContentHash = "hash",
                HtmlContent = "<p>Content</p>",
                Title = "Article Template",
                Culture = null
            });

        // Act
        var result = await _service.GetAllTemplatesAsync(mockUser, CancellationToken.None);

        // Assert
        Assert.Single(result);
        var template = result[0];
        Assert.Equal("_templates/article", template.TemplateName);
        Assert.Equal("Article Template", template.DisplayName);
        Assert.Equal("Template for articles", template.Description);
        Assert.Equal("docs", template.DefaultLocation);
        Assert.Equal("article-{date}", template.NamePattern);
        Assert.Contains("# Article Title", template.Content);
    }

    [Fact]
    public async Task GetAllTemplatesAsync_WithMultipleTemplates_ReturnsAllTemplates()
    {
        // Arrange
        var mockUser = CreateMockUser();

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "_templates/article", Title = "Article", Culture = null },
            new WikiPageInfo { PageName = "_templates/blog-post", Title = "Blog Post", Culture = null },
            new WikiPageInfo { PageName = "_templates/tutorial", Title = "Tutorial", Culture = null }
        };

        _mockPermissionHelper
            .Setup(x => x.GetAllAccessiblePages(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        SetupTemplatePage("_templates/article", "Article Template", "# Article");
        SetupTemplatePage("_templates/blog-post", "Blog Post Template", "# Blog");
        SetupTemplatePage("_templates/tutorial", "Tutorial Template", "# Tutorial");

        // Act
        var result = await _service.GetAllTemplatesAsync(mockUser, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, t => t.TemplateName == "_templates/article");
        Assert.Contains(result, t => t.TemplateName == "_templates/blog-post");
        Assert.Contains(result, t => t.TemplateName == "_templates/tutorial");
    }

    #endregion

    #region GetAllTemplatesAsync Tests - Template Page Name Detection

    [Fact]
    public async Task GetAllTemplatesAsync_WithTemplatesDirectory_ReturnsTemplates()
    {
        // Arrange
        var mockUser = CreateMockUser();

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "_templates/simple", Title = "Simple", Culture = null },
            new WikiPageInfo { PageName = "_templates/nested/advanced", Title = "Advanced", Culture = null }
        };

        _mockPermissionHelper
            .Setup(x => x.GetAllAccessiblePages(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        SetupTemplatePage("_templates/simple", "Simple Template", "# Simple");
        SetupTemplatePage("_templates/nested/advanced", "Advanced Template", "# Advanced");

        // Act
        var result = await _service.GetAllTemplatesAsync(mockUser, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllTemplatesAsync_WithTemplateNamedPage_ReturnsTemplate()
    {
        // Arrange
        var mockUser = CreateMockUser();

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "_template", Title = "Template", Culture = null }
        };

        _mockPermissionHelper
            .Setup(x => x.GetAllAccessiblePages(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        SetupTemplatePage("_template", "Root Template", "# Root");

        // Act
        var result = await _service.GetAllTemplatesAsync(mockUser, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("_template", result[0].TemplateName);
    }

    [Fact]
    public async Task GetAllTemplatesAsync_WithTemplateEndingPage_ReturnsTemplate()
    {
        // Arrange
        var mockUser = CreateMockUser();

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "docs/_template", Title = "Docs Template", Culture = null }
        };

        _mockPermissionHelper
            .Setup(x => x.GetAllAccessiblePages(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        SetupTemplatePage("docs/_template", "Docs Template", "# Docs");

        // Act
        var result = await _service.GetAllTemplatesAsync(mockUser, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("docs/_template", result[0].TemplateName);
    }

    [Fact]
    public async Task GetAllTemplatesAsync_MixedTemplatePages_ReturnsAllTemplates()
    {
        // Arrange
        var mockUser = CreateMockUser();

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "_templates/article", Title = "Article", Culture = null },
            new WikiPageInfo { PageName = "_template", Title = "Root Template", Culture = null },
            new WikiPageInfo { PageName = "docs/_template", Title = "Docs Template", Culture = null },
            new WikiPageInfo { PageName = "Home", Title = "Home", Culture = null } // Non-template
        };

        _mockPermissionHelper
            .Setup(x => x.GetAllAccessiblePages(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        SetupTemplatePage("_templates/article", "Article", "# Article");
        SetupTemplatePage("_template", "Root Template", "# Root");
        SetupTemplatePage("docs/_template", "Docs Template", "# Docs");

        // Act
        var result = await _service.GetAllTemplatesAsync(mockUser, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(result, t => t.TemplateName == "Home");
    }

    #endregion

    #region GetAllTemplatesAsync Tests - Front Matter Parsing

    [Fact]
    public async Task GetAllTemplatesAsync_WithCompleteFrontMatter_ParsesAllFields()
    {
        // Arrange
        var mockUser = CreateMockUser();

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "_templates/complete", Title = "Complete", Culture = null }
        };

        _mockPermissionHelper
            .Setup(x => x.GetAllAccessiblePages(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        var pageContent = @"---
title: Complete Template
description: A complete template with all fields
location: docs/articles
pattern: article-{year}-{month}-{day}
---

# Template Content

This is the template content.";

        _mockPageService
            .Setup(x => x.GetPageAsync("_templates/complete", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WikiPage
            {
                PageName = "_templates/complete",
                Content = pageContent,
                ContentHash = "hash",
                HtmlContent = "<p>Content</p>",
                Title = "Complete",
                Culture = null
            });

        // Act
        var result = await _service.GetAllTemplatesAsync(mockUser, CancellationToken.None);

        // Assert
        Assert.Single(result);
        var template = result[0];
        Assert.Equal("Complete Template", template.DisplayName);
        Assert.Equal("A complete template with all fields", template.Description);
        Assert.Equal("docs/articles", template.DefaultLocation);
        Assert.Equal("article-{year}-{month}-{day}", template.NamePattern);
        Assert.Contains("# Template Content", template.Content);
        Assert.DoesNotContain("---", template.Content);
    }

    [Fact]
    public async Task GetAllTemplatesAsync_WithNoFrontMatter_UsesPageTitle()
    {
        // Arrange
        var mockUser = CreateMockUser();

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "_templates/simple", Title = "Simple Template", Culture = null }
        };

        _mockPermissionHelper
            .Setup(x => x.GetAllAccessiblePages(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        var pageContent = @"# Simple Template

This is a simple template without front matter.";

        _mockPageService
            .Setup(x => x.GetPageAsync("_templates/simple", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WikiPage
            {
                PageName = "_templates/simple",
                Content = pageContent,
                ContentHash = "hash",
                HtmlContent = "<p>Content</p>",
                Title = "Simple Template",
                Culture = null
            });

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("_templates/simple", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Simple Template");

        // Act
        var result = await _service.GetAllTemplatesAsync(mockUser, CancellationToken.None);

        // Assert
        Assert.Single(result);
        var template = result[0];
        Assert.Equal("Simple Template", template.DisplayName);
        Assert.Equal("", template.Description);
        Assert.Equal("", template.DefaultLocation);
        Assert.Equal("", template.NamePattern);
        Assert.Equal(pageContent, template.Content);
    }

    [Fact]
    public async Task GetAllTemplatesAsync_WithPartialFrontMatter_ParsesAvailableFields()
    {
        // Arrange
        var mockUser = CreateMockUser();

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "_templates/partial", Title = "Partial", Culture = null }
        };

        _mockPermissionHelper
            .Setup(x => x.GetAllAccessiblePages(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        var pageContent = @"---
title: Partial Template
description: Has only some fields
---

# Content";

        _mockPageService
            .Setup(x => x.GetPageAsync("_templates/partial", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WikiPage
            {
                PageName = "_templates/partial",
                Content = pageContent,
                ContentHash = "hash",
                HtmlContent = "<p>Content</p>",
                Title = "Partial",
                Culture = null
            });

        // Act
        var result = await _service.GetAllTemplatesAsync(mockUser, CancellationToken.None);

        // Assert
        Assert.Single(result);
        var template = result[0];
        Assert.Equal("Partial Template", template.DisplayName);
        Assert.Equal("Has only some fields", template.Description);
        Assert.Equal("", template.DefaultLocation);
        Assert.Equal("", template.NamePattern);
    }

    [Fact]
    public async Task GetAllTemplatesAsync_WithNoTitleInFrontMatter_FallsBackToPageTitle()
    {
        // Arrange
        var mockUser = CreateMockUser();

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "_templates/fallback", Title = "Fallback Title", Culture = null }
        };

        _mockPermissionHelper
            .Setup(x => x.GetAllAccessiblePages(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        var pageContent = @"---
description: Template without title in front matter
---

# Content";

        _mockPageService
            .Setup(x => x.GetPageAsync("_templates/fallback", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WikiPage
            {
                PageName = "_templates/fallback",
                Content = pageContent,
                ContentHash = "hash",
                HtmlContent = "<p>Content</p>",
                Title = "Fallback Title",
                Culture = null
            });

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("_templates/fallback", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Fallback Title");

        // Act
        var result = await _service.GetAllTemplatesAsync(mockUser, CancellationToken.None);

        // Assert
        Assert.Single(result);
        var template = result[0];
        Assert.Equal("Fallback Title", template.DisplayName);
    }

    [Fact]
    public async Task GetAllTemplatesAsync_WithNoTitleAnywhere_UsesPageName()
    {
        // Arrange
        var mockUser = CreateMockUser();

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "_templates/noTitle", Title = null, Culture = null }
        };

        _mockPermissionHelper
            .Setup(x => x.GetAllAccessiblePages(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        var pageContent = "# Content without front matter";

        _mockPageService
            .Setup(x => x.GetPageAsync("_templates/noTitle", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WikiPage
            {
                PageName = "_templates/noTitle",
                Content = pageContent,
                ContentHash = "hash",
                HtmlContent = "<p>Content</p>",
                Title = null,
                Culture = null
            });

        _mockPageService
            .Setup(x => x.GetPageTitleAsync("_templates/noTitle", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.GetAllTemplatesAsync(mockUser, CancellationToken.None);

        // Assert
        Assert.Single(result);
        var template = result[0];
        Assert.Equal("_templates/noTitle", template.DisplayName);
    }

    #endregion

    #region GetAllTemplatesAsync Tests - Sorting

    [Fact]
    public async Task GetAllTemplatesAsync_SortsByDisplayName()
    {
        // Arrange
        var mockUser = CreateMockUser();

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "_templates/zebra", Title = "Zebra", Culture = null },
            new WikiPageInfo { PageName = "_templates/apple", Title = "Apple", Culture = null },
            new WikiPageInfo { PageName = "_templates/mango", Title = "Mango", Culture = null }
        };

        _mockPermissionHelper
            .Setup(x => x.GetAllAccessiblePages(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        SetupTemplatePage("_templates/zebra", "Zebra Template", "# Zebra");
        SetupTemplatePage("_templates/apple", "Apple Template", "# Apple");
        SetupTemplatePage("_templates/mango", "Mango Template", "# Mango");

        // Act
        var result = await _service.GetAllTemplatesAsync(mockUser, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Apple Template", result[0].DisplayName);
        Assert.Equal("Mango Template", result[1].DisplayName);
        Assert.Equal("Zebra Template", result[2].DisplayName);
    }

    [Fact]
    public async Task GetAllTemplatesAsync_SortsByTemplateNameWhenNoDisplayName()
    {
        // Arrange
        var mockUser = CreateMockUser();

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "_templates/z-template", Title = null, Culture = null },
            new WikiPageInfo { PageName = "_templates/a-template", Title = null, Culture = null },
            new WikiPageInfo { PageName = "_templates/m-template", Title = null, Culture = null }
        };

        _mockPermissionHelper
            .Setup(x => x.GetAllAccessiblePages(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        SetupTemplatePageWithoutTitle("_templates/z-template", "# Z");
        SetupTemplatePageWithoutTitle("_templates/a-template", "# A");
        SetupTemplatePageWithoutTitle("_templates/m-template", "# M");

        // Act
        var result = await _service.GetAllTemplatesAsync(mockUser, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("_templates/a-template", result[0].DisplayName);
        Assert.Equal("_templates/m-template", result[1].DisplayName);
        Assert.Equal("_templates/z-template", result[2].DisplayName);
    }

    #endregion

    #region GetAllTemplatesAsync Tests - Error Handling

    [Fact]
    public async Task GetAllTemplatesAsync_WhenPageLoadFails_SkipsThatTemplate()
    {
        // Arrange
        var mockUser = CreateMockUser();

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "_templates/good", Title = "Good", Culture = null },
            new WikiPageInfo { PageName = "_templates/missing", Title = "Missing", Culture = null },
            new WikiPageInfo { PageName = "_templates/another", Title = "Another", Culture = null }
        };

        _mockPermissionHelper
            .Setup(x => x.GetAllAccessiblePages(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        SetupTemplatePage("_templates/good", "Good Template", "# Good");
        SetupTemplatePage("_templates/another", "Another Template", "# Another");

        _mockPageService
            .Setup(x => x.GetPageAsync("_templates/missing", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WikiPage?)null);

        // Act
        var result = await _service.GetAllTemplatesAsync(mockUser, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.TemplateName == "_templates/good");
        Assert.Contains(result, t => t.TemplateName == "_templates/another");
        Assert.DoesNotContain(result, t => t.TemplateName == "_templates/missing");
    }

    #endregion

    #region GetAllTemplatesAsync Tests - CancellationToken

    [Fact]
    public async Task GetAllTemplatesAsync_PassesCancellationToken()
    {
        // Arrange
        var mockUser = CreateMockUser();
        var cancellationToken = new CancellationToken();

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "_templates/test", Title = "Test", Culture = null }
        };

        _mockPermissionHelper
            .Setup(x => x.GetAllAccessiblePages(mockUser, cancellationToken))
            .ReturnsAsync(allPages);

        SetupTemplatePage("_templates/test", "Test Template", "# Test");

        // Act
        await _service.GetAllTemplatesAsync(mockUser, cancellationToken);

        // Assert
        _mockPermissionHelper.Verify(
            x => x.GetAllAccessiblePages(mockUser, cancellationToken),
            Times.Once);

        _mockPageService.Verify(
            x => x.GetPageAsync("_templates/test", null, cancellationToken),
            Times.Once);
    }

    #endregion

    #region GetTemplateAsync Tests - Basic Scenarios

    [Fact]
    public async Task GetTemplateAsync_WithNullTemplateId_ReturnsNull()
    {
        // Arrange
        var mockUser = CreateMockUser();

        // Act
        var result = await _service.GetTemplateAsync(mockUser, null!, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTemplateAsync_WithEmptyTemplateId_ReturnsNull()
    {
        // Arrange
        var mockUser = CreateMockUser();

        // Act
        var result = await _service.GetTemplateAsync(mockUser, "", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTemplateAsync_WithValidTemplateId_ReturnsTemplate()
    {
        // Arrange
        var mockUser = CreateMockUser();

        _mockPermissionHelper
            .Setup(x => x.CanView(mockUser, "_templates/article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var pageContent = @"---
title: Article Template
description: Template for articles
---

# Article";

        _mockPageService
            .Setup(x => x.GetPageAsync("_templates/article", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WikiPage
            {
                PageName = "_templates/article",
                Content = pageContent,
                ContentHash = "hash",
                HtmlContent = "<p>Content</p>",
                Title = "Article Template",
                Culture = null
            });

        // Act
        var result = await _service.GetTemplateAsync(mockUser, "_templates/article", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("_templates/article", result.TemplateName);
        Assert.Equal("Article Template", result.DisplayName);
        Assert.Equal("Template for articles", result.Description);
    }

    [Fact]
    public async Task GetTemplateAsync_WhenPageDoesNotExist_ReturnsNull()
    {
        // Arrange
        var mockUser = CreateMockUser();

        _mockPermissionHelper
            .Setup(x => x.CanView(mockUser, "_templates/nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockPageService
            .Setup(x => x.GetPageAsync("_templates/nonexistent", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WikiPage?)null);

        // Act
        var result = await _service.GetTemplateAsync(mockUser, "_templates/nonexistent", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetTemplateAsync Tests - Validation

    [Fact]
    public async Task GetTemplateAsync_WithInvalidPageName_ThrowsArgumentException()
    {
        // Arrange
        var mockUser = CreateMockUser();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetTemplateAsync(mockUser, "invalid name", CancellationToken.None));
    }

    [Fact]
    public async Task GetTemplateAsync_WithNonTemplatePageName_ThrowsArgumentException()
    {
        // Arrange
        var mockUser = CreateMockUser();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetTemplateAsync(mockUser, "regular-page", CancellationToken.None));

        Assert.Equal("templateId", exception.ParamName);
        Assert.Contains("Invalid template ID.", exception.Message);
    }

    [Fact]
    public async Task GetTemplateAsync_WithTemplateInTemplatesDirectory_Works()
    {
        // Arrange
        var mockUser = CreateMockUser();

        _mockPermissionHelper
            .Setup(x => x.CanView(mockUser, "_templates/valid", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        SetupTemplatePage("_templates/valid", "Valid Template", "# Valid");

        // Act
        var result = await _service.GetTemplateAsync(mockUser, "_templates/valid", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTemplateAsync_WithTemplateNamedPage_Works()
    {
        // Arrange
        var mockUser = CreateMockUser();

        _mockPermissionHelper
            .Setup(x => x.CanView(mockUser, "_template", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        SetupTemplatePage("_template", "Root Template", "# Root");

        // Act
        var result = await _service.GetTemplateAsync(mockUser, "_template", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTemplateAsync_WithTemplateEndingPage_Works()
    {
        // Arrange
        var mockUser = CreateMockUser();

        _mockPermissionHelper
            .Setup(x => x.CanView(mockUser, "docs/_template", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        SetupTemplatePage("docs/_template", "Docs Template", "# Docs");

        // Act
        var result = await _service.GetTemplateAsync(mockUser, "docs/_template", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region GetTemplateAsync Tests - Permissions

    [Fact]
    public async Task GetTemplateAsync_WhenUserCannotView_ReturnsNull()
    {
        // Arrange
        var mockUser = CreateMockUser();

        _mockPermissionHelper
            .Setup(x => x.CanView(mockUser, "_templates/restricted", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.GetTemplateAsync(mockUser, "_templates/restricted", CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockPageService.Verify(
            x => x.GetPageAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetTemplateAsync_ChecksPermissionsBeforeLoadingPage()
    {
        // Arrange
        var mockUser = CreateMockUser();

        _mockPermissionHelper
            .Setup(x => x.CanView(mockUser, "_templates/test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _service.GetTemplateAsync(mockUser, "_templates/test", CancellationToken.None);

        // Assert
        _mockPermissionHelper.Verify(
            x => x.CanView(mockUser, "_templates/test", It.IsAny<CancellationToken>()),
            Times.Once);

        _mockPageService.Verify(
            x => x.GetPageAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region GetTemplateAsync Tests - CancellationToken

    [Fact]
    public async Task GetTemplateAsync_PassesCancellationToken()
    {
        // Arrange
        var mockUser = CreateMockUser();
        var cancellationToken = new CancellationToken();

        _mockPermissionHelper
            .Setup(x => x.CanView(mockUser, "_templates/test", cancellationToken))
            .ReturnsAsync(true);

        SetupTemplatePage("_templates/test", "Test", "# Test");

        // Act
        await _service.GetTemplateAsync(mockUser, "_templates/test", cancellationToken);

        // Assert
        _mockPermissionHelper.Verify(
            x => x.CanView(mockUser, "_templates/test", cancellationToken),
            Times.Once);

        _mockPageService.Verify(
            x => x.GetPageAsync("_templates/test", null, cancellationToken),
            Times.Once);
    }

    #endregion

    #region ResolvePlaceHolders Tests - Date Placeholders

    [Fact]
    public void ResolvePlaceHolders_WithDatePlaceholder_ReplacesWithCurrentDate()
    {
        // Arrange
        var pattern = "article-{date}";
        var expectedDate = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var result = _service.ResolvePlaceHolders(pattern);

        // Assert
        Assert.Equal($"article-{expectedDate}", result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithDateTimePlaceholder_ReplacesWithCurrentDateTime()
    {
        // Arrange
        var pattern = "log-{datetime}";
        var expectedDateTime = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd-HHmmss");

        // Act
        var result = _service.ResolvePlaceHolders(pattern);

        // Assert
        Assert.StartsWith("log-", result);
        Assert.Contains(DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"), result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithYearPlaceholder_ReplacesWithCurrentYear()
    {
        // Arrange
        var pattern = "report-{year}";
        var expectedYear = DateTimeOffset.UtcNow.Year.ToString();

        // Act
        var result = _service.ResolvePlaceHolders(pattern);

        // Assert
        Assert.Equal($"report-{expectedYear}", result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithMonthPlaceholder_ReplacesWithCurrentMonth()
    {
        // Arrange
        var pattern = "monthly-{month}";
        var expectedMonth = DateTimeOffset.UtcNow.Month.ToString("D2");

        // Act
        var result = _service.ResolvePlaceHolders(pattern);

        // Assert
        Assert.Equal($"monthly-{expectedMonth}", result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithDayPlaceholder_ReplacesWithCurrentDay()
    {
        // Arrange
        var pattern = "daily-{day}";
        var expectedDay = DateTimeOffset.UtcNow.Day.ToString("D2");

        // Act
        var result = _service.ResolvePlaceHolders(pattern);

        // Assert
        Assert.Equal($"daily-{expectedDay}", result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithMultiplePlaceholders_ReplacesAll()
    {
        // Arrange
        var pattern = "article-{year}-{month}-{day}";
        var now = DateTimeOffset.UtcNow;
        var expectedYear = now.Year.ToString();
        var expectedMonth = now.Month.ToString("D2");
        var expectedDay = now.Day.ToString("D2");

        // Act
        var result = _service.ResolvePlaceHolders(pattern);

        // Assert
        Assert.Equal($"article-{expectedYear}-{expectedMonth}-{expectedDay}", result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithMixedPlaceholders_ReplacesAll()
    {
        // Arrange
        var pattern = "{year}/{month}/report-{date}-{day}";
        var now = DateTimeOffset.UtcNow;
        var expectedDate = now.ToString("yyyy-MM-dd");
        var expectedYear = now.Year.ToString();
        var expectedMonth = now.Month.ToString("D2");
        var expectedDay = now.Day.ToString("D2");

        // Act
        var result = _service.ResolvePlaceHolders(pattern);

        // Assert
        Assert.Equal($"{expectedYear}/{expectedMonth}/report-{expectedDate}-{expectedDay}", result);
    }

    #endregion

    #region ResolvePlaceHolders Tests - Case Insensitivity

    [Fact]
    public void ResolvePlaceHolders_WithUppercasePlaceholder_Replaces()
    {
        // Arrange
        var pattern = "article-{DATE}";
        var expectedDate = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var result = _service.ResolvePlaceHolders(pattern);

        // Assert
        Assert.Equal($"article-{expectedDate}", result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithMixedCasePlaceholder_Replaces()
    {
        // Arrange
        var pattern = "article-{DaTe}";
        var expectedDate = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var result = _service.ResolvePlaceHolders(pattern);

        // Assert
        Assert.Equal($"article-{expectedDate}", result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithMixedCasePlaceholders_ReplacesAll()
    {
        // Arrange
        var pattern = "{YEAR}/{Month}/{dAy}";
        var now = DateTimeOffset.UtcNow;
        var expectedYear = now.Year.ToString();
        var expectedMonth = now.Month.ToString("D2");
        var expectedDay = now.Day.ToString("D2");

        // Act
        var result = _service.ResolvePlaceHolders(pattern);

        // Assert
        Assert.Equal($"{expectedYear}/{expectedMonth}/{expectedDay}", result);
    }

    #endregion

    #region ResolvePlaceHolders Tests - Edge Cases

    [Fact]
    public void ResolvePlaceHolders_WithNullPattern_ReturnsEmptyString()
    {
        // Act
        var result = _service.ResolvePlaceHolders(null!);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithEmptyPattern_ReturnsEmptyString()
    {
        // Act
        var result = _service.ResolvePlaceHolders("");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithWhitespacePattern_ReturnsEmptyString()
    {
        // Act
        var result = _service.ResolvePlaceHolders("   ");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithNoPlaceholders_ReturnsOriginalPattern()
    {
        // Arrange
        var pattern = "article-without-placeholders";

        // Act
        var result = _service.ResolvePlaceHolders(pattern);

        // Assert
        Assert.Equal(pattern, result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithUnknownPlaceholder_LeavesUnchanged()
    {
        // Arrange
        var pattern = "article-{unknown}";

        // Act
        var result = _service.ResolvePlaceHolders(pattern);

        // Assert
        Assert.Equal("article-{unknown}", result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithPartialPlaceholderMatch_LeavesUnchanged()
    {
        // Arrange
        var pattern = "article-{dates}"; // Note the 's'

        // Act
        var result = _service.ResolvePlaceHolders(pattern);

        // Assert
        Assert.Equal("article-{dates}", result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithPlaceholderInMiddleOfWord_Replaces()
    {
        // Arrange
        var pattern = "prefix{date}suffix";
        var expectedDate = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var result = _service.ResolvePlaceHolders(pattern);

        // Assert
        Assert.Equal($"prefix{expectedDate}suffix", result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithDuplicatePlaceholders_ReplacesAll()
    {
        // Arrange
        var pattern = "{date}-{date}";
        var expectedDate = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var result = _service.ResolvePlaceHolders(pattern);

        // Assert
        Assert.Equal($"{expectedDate}-{expectedDate}", result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithComplexPattern_ReplacesCorrectly()
    {
        // Arrange
        var pattern = "docs/{year}/reports/{month}/report-{date}-final";
        var now = DateTimeOffset.UtcNow;
        var expectedYear = now.Year.ToString();
        var expectedMonth = now.Month.ToString("D2");
        var expectedDate = now.ToString("yyyy-MM-dd");

        // Act
        var result = _service.ResolvePlaceHolders(pattern);

        // Assert
        Assert.Equal($"docs/{expectedYear}/reports/{expectedMonth}/report-{expectedDate}-final", result);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CompleteWorkflow_GetAllTemplatesAndResolvePattern()
    {
        // Arrange
        var mockUser = CreateMockUser();

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "_templates/article", Title = "Article", Culture = null }
        };

        _mockPermissionHelper
            .Setup(x => x.GetAllAccessiblePages(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        var pageContent = @"---
title: Article Template
pattern: articles/{year}/{month}/article-{date}
---

# Article Content";

        _mockPageService
            .Setup(x => x.GetPageAsync("_templates/article", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WikiPage
            {
                PageName = "_templates/article",
                Content = pageContent,
                ContentHash = "hash",
                HtmlContent = "<p>Content</p>",
                Title = "Article Template",
                Culture = null
            });

        // Act
        var templates = await _service.GetAllTemplatesAsync(mockUser, CancellationToken.None);
        var template = templates.First();
        var resolvedPattern = _service.ResolvePlaceHolders(template.NamePattern!);

        // Assert
        Assert.Single(templates);
        Assert.NotNull(template.NamePattern);
        Assert.Contains("{year}", template.NamePattern);
        Assert.Contains("{month}", template.NamePattern);
        Assert.Contains("{date}", template.NamePattern);

        Assert.DoesNotContain("{year}", resolvedPattern);
        Assert.DoesNotContain("{month}", resolvedPattern);
        Assert.DoesNotContain("{date}", resolvedPattern);
        Assert.StartsWith("articles/", resolvedPattern);
    }

    [Fact]
    public async Task CompleteWorkflow_GetSpecificTemplateAndResolvePattern()
    {
        // Arrange
        var mockUser = CreateMockUser();

        _mockPermissionHelper
            .Setup(x => x.CanView(mockUser, "_templates/daily-log", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var pageContent = @"---
title: Daily Log Template
pattern: logs/{year}/{month}/log-{date}
location: daily-logs
---

# Daily Log

Date: {date}";

        _mockPageService
            .Setup(x => x.GetPageAsync("_templates/daily-log", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WikiPage
            {
                PageName = "_templates/daily-log",
                Content = pageContent,
                ContentHash = "hash",
                HtmlContent = "<p>Content</p>",
                Title = "Daily Log Template",
                Culture = null
            });

        // Act
        var template = await _service.GetTemplateAsync(mockUser, "_templates/daily-log", CancellationToken.None);
        Assert.NotNull(template);

        var resolvedPattern = _service.ResolvePlaceHolders(template.NamePattern!);
        var resolvedContent = _service.ResolvePlaceHolders(template.Content);

        // Assert
        Assert.NotNull(template.NamePattern);
        Assert.Contains("{date}", template.NamePattern);

        Assert.DoesNotContain("{date}", resolvedPattern);
        Assert.DoesNotContain("{date}", resolvedContent);
        Assert.StartsWith("logs/", resolvedPattern);
    }

    #endregion

    #region Helper Methods

    private static IWikiUserWithPermissions CreateMockUser()
    {
        var mockUser = new Mock<IWikiUserWithPermissions>();
        mockUser.Setup(x => x.CanView).Returns(true);
        mockUser.Setup(x => x.CanEdit).Returns(true);
        mockUser.Setup(x => x.Groups).Returns(new[] { "users" });
        return mockUser.Object;
    }

    private void SetupTemplatePage(string pageName, string title, string content)
    {
        var pageContent = $@"---
title: {title}
---

{content}";

        _mockPageService
            .Setup(x => x.GetPageAsync(pageName, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WikiPage
            {
                PageName = pageName,
                Content = pageContent,
                ContentHash = "hash",
                HtmlContent = "<p>Content</p>",
                Title = title,
                Culture = null
            });
    }

    private void SetupTemplatePageWithoutTitle(string pageName, string content)
    {
        _mockPageService
            .Setup(x => x.GetPageAsync(pageName, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WikiPage
            {
                PageName = pageName,
                Content = content,
                ContentHash = "hash",
                HtmlContent = "<p>Content</p>",
                Title = null,
                Culture = null
            });

        _mockPageService
            .Setup(x => x.GetPageTitleAsync(pageName, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
    }

    #endregion
}
