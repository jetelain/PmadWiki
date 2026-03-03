using Moq;
using Pmad.Wiki.Helpers;
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
            .Setup(x => x.GetAllAccessiblePagesAsync(mockUser, It.IsAny<CancellationToken>()))
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
            .Setup(x => x.GetAllAccessiblePagesAsync(mockUser, It.IsAny<CancellationToken>()))
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
            .Setup(x => x.GetAllAccessiblePagesAsync(mockUser, It.IsAny<CancellationToken>()))
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
                Content = WikiPageContentParser.Parse(pageContent),
                ContentHash = "hash",
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
            .Setup(x => x.GetAllAccessiblePagesAsync(mockUser, It.IsAny<CancellationToken>()))
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
            .Setup(x => x.GetAllAccessiblePagesAsync(mockUser, It.IsAny<CancellationToken>()))
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
            .Setup(x => x.GetAllAccessiblePagesAsync(mockUser, It.IsAny<CancellationToken>()))
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
            .Setup(x => x.GetAllAccessiblePagesAsync(mockUser, It.IsAny<CancellationToken>()))
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
            .Setup(x => x.GetAllAccessiblePagesAsync(mockUser, It.IsAny<CancellationToken>()))
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
            .Setup(x => x.GetAllAccessiblePagesAsync(mockUser, It.IsAny<CancellationToken>()))
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
                Content = WikiPageContentParser.Parse(pageContent),
                ContentHash = "hash",
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
            .Setup(x => x.GetAllAccessiblePagesAsync(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        var pageContent = @"# Simple Template

This is a simple template without front matter.";

        _mockPageService
            .Setup(x => x.GetPageAsync("_templates/simple", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WikiPage
            {
                PageName = "_templates/simple",
                Content = WikiPageContentParser.Parse(pageContent),
                ContentHash = "hash",
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
        Assert.Null(template.Description);
        Assert.Null(template.DefaultLocation);
        Assert.Null(template.NamePattern);
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
            .Setup(x => x.GetAllAccessiblePagesAsync(mockUser, It.IsAny<CancellationToken>()))
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
                Content = WikiPageContentParser.Parse(pageContent),
                ContentHash = "hash",
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
        Assert.Null(template.DefaultLocation);
        Assert.Null(template.NamePattern);
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
            .Setup(x => x.GetAllAccessiblePagesAsync(mockUser, It.IsAny<CancellationToken>()))
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
                Content = WikiPageContentParser.Parse(pageContent),
                ContentHash = "hash",
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
            .Setup(x => x.GetAllAccessiblePagesAsync(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        var pageContent = "# Content without front matter";

        _mockPageService
            .Setup(x => x.GetPageAsync("_templates/noTitle", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WikiPage
            {
                PageName = "_templates/noTitle",
                Content = WikiPageContentParser.Parse(pageContent),
                ContentHash = "hash",
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
            .Setup(x => x.GetAllAccessiblePagesAsync(mockUser, It.IsAny<CancellationToken>()))
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
            .Setup(x => x.GetAllAccessiblePagesAsync(mockUser, It.IsAny<CancellationToken>()))
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
            .Setup(x => x.GetAllAccessiblePagesAsync(mockUser, It.IsAny<CancellationToken>()))
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
            .Setup(x => x.GetAllAccessiblePagesAsync(mockUser, cancellationToken))
            .ReturnsAsync(allPages);

        SetupTemplatePage("_templates/test", "Test Template", "# Test");

        // Act
        await _service.GetAllTemplatesAsync(mockUser, cancellationToken);

        // Assert
        _mockPermissionHelper.Verify(
            x => x.GetAllAccessiblePagesAsync(mockUser, cancellationToken),
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
                Content = WikiPageContentParser.Parse(pageContent),
                ContentHash = "hash",
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
        var result = _service.ResolvePlaceholders(pattern);

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
        var result = _service.ResolvePlaceholders(pattern);

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
        var result = _service.ResolvePlaceholders(pattern);

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
        var result = _service.ResolvePlaceholders(pattern);

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
        var result = _service.ResolvePlaceholders(pattern);

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
        var result = _service.ResolvePlaceholders(pattern);

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
        var result = _service.ResolvePlaceholders(pattern);

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
        var result = _service.ResolvePlaceholders(pattern);

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
        var result = _service.ResolvePlaceholders(pattern);

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
        var result = _service.ResolvePlaceholders(pattern);

        // Assert
        Assert.Equal($"{expectedYear}/{expectedMonth}/{expectedDay}", result);
    }

    #endregion

    #region ResolvePlaceHolders Tests - Edge Cases

    [Fact]
    public void ResolvePlaceHolders_WithNullPattern_ReturnsEmptyString()
    {
        // Act
        var result = _service.ResolvePlaceholders(null!);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithEmptyPattern_ReturnsEmptyString()
    {
        // Act
        var result = _service.ResolvePlaceholders("");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithWhitespacePattern_ReturnsEmptyString()
    {
        // Act
        var result = _service.ResolvePlaceholders("   ");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithNoPlaceholders_ReturnsOriginalPattern()
    {
        // Arrange
        var pattern = "article-without-placeholders";

        // Act
        var result = _service.ResolvePlaceholders(pattern);

        // Assert
        Assert.Equal(pattern, result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithUnknownPlaceholder_LeavesUnchanged()
    {
        // Arrange
        var pattern = "article-{unknown}";

        // Act
        var result = _service.ResolvePlaceholders(pattern);

        // Assert
        Assert.Equal("article-{unknown}", result);
    }

    [Fact]
    public void ResolvePlaceHolders_WithPartialPlaceholderMatch_LeavesUnchanged()
    {
        // Arrange
        var pattern = "article-{dates}"; // Note the 's'

        // Act
        var result = _service.ResolvePlaceholders(pattern);

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
        var result = _service.ResolvePlaceholders(pattern);

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
        var result = _service.ResolvePlaceholders(pattern);

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
        var result = _service.ResolvePlaceholders(pattern);

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
            .Setup(x => x.GetAllAccessiblePagesAsync(mockUser, It.IsAny<CancellationToken>()))
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
                Content = WikiPageContentParser.Parse(pageContent),
                ContentHash = "hash",
                Title = "Article Template",
                Culture = null
            });

        // Act
        var templates = await _service.GetAllTemplatesAsync(mockUser, CancellationToken.None);
        var template = templates.First();
        var resolvedPattern = _service.ResolvePlaceholders(template.NamePattern!);

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
                Content = WikiPageContentParser.Parse(pageContent),
                ContentHash = "hash",
                Title = "Daily Log Template",
                Culture = null
            });

        // Act
        var template = await _service.GetTemplateAsync(mockUser, "_templates/daily-log", CancellationToken.None);
        Assert.NotNull(template);

        var resolvedPattern = _service.ResolvePlaceholders(template.NamePattern!);
        var resolvedContent = _service.ResolvePlaceholders(template.Content);

        // Assert
        Assert.NotNull(template.NamePattern);
        Assert.Contains("{date}", template.NamePattern);

        Assert.DoesNotContain("{date}", resolvedPattern);
        Assert.DoesNotContain("{date}", resolvedContent);
        Assert.StartsWith("logs/", resolvedPattern);
    }

    #endregion

    #region Custom Parameters Tests

    [Fact]
    public async Task GetTemplateAsync_WithCustomParameters_ParsesParametersCorrectly()
    {
        // Arrange
        var mockUser = CreateMockUser();

        _mockPermissionHelper
            .Setup(x => x.CanView(mockUser, "_templates/feature", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var pageContent = @"---
title: Feature Template
pattern: ""{ticket_id}-{name}""
location: ""Features/{category}""
parameters:
  - name: category
    type: text
    label: Category
    default: core
    required: true
    help: Feature category
  - name: ticket_id
    type: text
    label: Ticket ID
    required: true
  - name: name
    type: text
    label: Feature Name
    required: true
---

# Feature: {name}

Ticket: {ticket_id}
Category: {category}";

        _mockPageService
            .Setup(x => x.GetPageAsync("_templates/feature", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WikiPage
            {
                PageName = "_templates/feature",
                Content = WikiPageContentParser.Parse(pageContent),
                ContentHash = "hash",
                Title = "Feature Template",
                Culture = null
            });

        // Act
        var template = await _service.GetTemplateAsync(mockUser, "_templates/feature", CancellationToken.None);

        // Assert
        Assert.NotNull(template);
        Assert.Equal(3, template.Parameters.Count);
        Assert.Empty(template.InvalidParameterNames);

        var categoryParam = template.Parameters.FirstOrDefault(p => p.Name == "category");
        Assert.NotNull(categoryParam);
        Assert.Equal("Category", categoryParam.Label);
        Assert.Equal("core", categoryParam.DefaultValue);
        Assert.True(categoryParam.Required);
        Assert.Equal("Feature category", categoryParam.HelpText);

        var ticketParam = template.Parameters.FirstOrDefault(p => p.Name == "ticket_id");
        Assert.NotNull(ticketParam);
        Assert.True(ticketParam.Required);

        var nameParam = template.Parameters.FirstOrDefault(p => p.Name == "name");
        Assert.NotNull(nameParam);
        Assert.Equal("Feature Name", nameParam.Label);
    }

    [Fact]
    public void ResolvePlaceholders_WithCustomParameters_ReplacesCorrectly()
    {
        // Arrange
        var pattern = "Features/{category}/{ticket-id}-{name}";
        var parameterValues = new Dictionary<string, string>
        {
            ["category"] = "ui",
            ["ticket-id"] = "JIRA-123",
            ["name"] = "Dark-Mode"
        };

        // Act
        var result = _service.ResolvePlaceholders(pattern, parameterValues);

        // Assert
        Assert.Equal("Features/ui/JIRA-123-Dark-Mode", result);
    }

    [Fact]
    public void ResolvePlaceholders_WithMixedPlaceholders_ResolvesBothCustomAndBuiltin()
    {
        // Arrange
        var pattern = "{category}/{year}/{month}/{name}";
        var parameterValues = new Dictionary<string, string>
        {
            ["category"] = "blog",
            ["name"] = "my-post"
        };

        // Act
        var result = _service.ResolvePlaceholders(pattern, parameterValues);

        // Assert
        Assert.Contains("blog", result);
        Assert.Contains("my-post", result);
        Assert.Matches(@"^\w+/\d{4}/\d{2}/\w+-\w+$", result);
    }

    [Fact]
    public void ResolvePlaceholders_WithCaseInsensitivePlaceholders_ReplacesCorrectly()
    {
        // Arrange
        var pattern = "{CATEGORY}/{Name}/{TICKET-ID}";
        var parameterValues = new Dictionary<string, string>
        {
            ["category"] = "docs",
            ["name"] = "guide",
            ["ticket-id"] = "DOC-001"
        };

        // Act
        var result = _service.ResolvePlaceholders(pattern, parameterValues);

        // Assert
        Assert.Equal("docs/guide/DOC-001", result);
    }

    [Fact]
    public async Task GetTemplateAsync_WithDifferentParameterTypes_ParsesCorrectly()
    {
        // Arrange
        var mockUser = CreateMockUser();

        _mockPermissionHelper
            .Setup(x => x.CanView(mockUser, "_templates/mixed", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var pageContent = @"---
title: Mixed Parameters
parameters:
  - name: text_param
    type: text
  - name: number_param
    type: number
  - name: date_param
    type: date
  - name: datetime_param
    type: datetime
---

Content";

        _mockPageService
            .Setup(x => x.GetPageAsync("_templates/mixed", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WikiPage
            {
                PageName = "_templates/mixed",
                Content = WikiPageContentParser.Parse(pageContent),
                ContentHash = "hash",
                Title = "Mixed Parameters",
                Culture = null
            });

        // Act
        var template = await _service.GetTemplateAsync(mockUser, "_templates/mixed", CancellationToken.None);

        // Assert
        Assert.NotNull(template);
        Assert.Equal(4, template.Parameters.Count);
        Assert.Empty(template.InvalidParameterNames);

        Assert.Equal(Models.WikiTemplateParameterType.Text, 
            template.Parameters.First(p => p.Name == "text_param").Type);
        Assert.Equal(Models.WikiTemplateParameterType.Number, 
            template.Parameters.First(p => p.Name == "number_param").Type);
        Assert.Equal(Models.WikiTemplateParameterType.Date, 
            template.Parameters.First(p => p.Name == "date_param").Type);
        Assert.Equal(Models.WikiTemplateParameterType.DateTime, 
            template.Parameters.First(p => p.Name == "datetime_param").Type);
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
                Content = WikiPageContentParser.Parse(pageContent),
                ContentHash = "hash",
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
                Content = WikiPageContentParser.Parse(content),
                ContentHash = "hash",
                Title = null,
                Culture = null
            });

        _mockPageService
            .Setup(x => x.GetPageTitleAsync(pageName, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
    }

    #endregion

    #region Unsafe Parameter Filtering Tests

    [Fact]
    public async Task GetTemplateAsync_WithUnsafeParameterName_FiltersItOut()
    {
        // Arrange
        var mockUser = CreateMockUser();

        _mockPermissionHelper
            .Setup(x => x.CanView(mockUser, "_templates/unsafe", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var pageContent = @"---
title: Unsafe Template
parameters:
  - name: valid_param
    type: text
  - name: ../etc/passwd
    type: text
---

Content";

        _mockPageService
            .Setup(x => x.GetPageAsync("_templates/unsafe", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WikiPage
            {
                PageName = "_templates/unsafe",
                Content = WikiPageContentParser.Parse(pageContent),
                ContentHash = "hash",
                Title = "Unsafe Template",
                Culture = null
            });

        // Act
        var template = await _service.GetTemplateAsync(mockUser, "_templates/unsafe", CancellationToken.None);

        // Assert
        Assert.NotNull(template);
        Assert.Single(template.Parameters);
        Assert.Equal("valid_param", template.Parameters[0].Name);
        Assert.Single(template.InvalidParameterNames);
        Assert.Contains("../etc/passwd", template.InvalidParameterNames);
    }

    [Fact]
    public async Task GetTemplateAsync_WithAllUnsafeParameterNames_ReturnsEmptyParameterList()
    {
        // Arrange
        var mockUser = CreateMockUser();

        _mockPermissionHelper
            .Setup(x => x.CanView(mockUser, "_templates/all-unsafe", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var pageContent = @"---
title: All Unsafe Template
parameters:
  - name: my-param
    type: text
  - name: my param
    type: text
  - name: my.param
    type: text
---

Content";

        _mockPageService
            .Setup(x => x.GetPageAsync("_templates/all-unsafe", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WikiPage
            {
                PageName = "_templates/all-unsafe",
                Content = WikiPageContentParser.Parse(pageContent),
                ContentHash = "hash",
                Title = "All Unsafe Template",
                Culture = null
            });

        // Act
        var template = await _service.GetTemplateAsync(mockUser, "_templates/all-unsafe", CancellationToken.None);

        // Assert
        Assert.NotNull(template);
        Assert.Empty(template.Parameters);
        Assert.Equal(3, template.InvalidParameterNames.Count);
        Assert.Contains("my-param", template.InvalidParameterNames);
        Assert.Contains("my param", template.InvalidParameterNames);
        Assert.Contains("my.param", template.InvalidParameterNames);
    }

    [Fact]
    public async Task GetTemplateAsync_WithAllSafeParameterNames_ReturnsAllParameters()
    {
        // Arrange
        var mockUser = CreateMockUser();

        _mockPermissionHelper
            .Setup(x => x.CanView(mockUser, "_templates/all-safe", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var pageContent = @"---
title: All Safe Template
parameters:
  - name: alpha
    type: text
  - name: Beta2
    type: text
  - name: gamma_3
    type: text
---

Content";

        _mockPageService
            .Setup(x => x.GetPageAsync("_templates/all-safe", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WikiPage
            {
                PageName = "_templates/all-safe",
                Content = WikiPageContentParser.Parse(pageContent),
                ContentHash = "hash",
                Title = "All Safe Template",
                Culture = null
            });

        // Act
        var template = await _service.GetTemplateAsync(mockUser, "_templates/all-safe", CancellationToken.None);

        // Assert
        Assert.NotNull(template);
        Assert.Equal(3, template.Parameters.Count);
        Assert.Contains(template.Parameters, p => p.Name == "alpha");
        Assert.Contains(template.Parameters, p => p.Name == "Beta2");
        Assert.Contains(template.Parameters, p => p.Name == "gamma_3");
        Assert.Empty(template.InvalidParameterNames);
    }

    [Fact]
    public async Task GetAllTemplatesAsync_WithUnsafeParameterName_FiltersItOut()
    {
        // Arrange
        var mockUser = CreateMockUser();

        var allPages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "_templates/mixed-params", Title = "Mixed", Culture = null }
        };

        _mockPermissionHelper
            .Setup(x => x.GetAllAccessiblePagesAsync(mockUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPages);

        var pageContent = @"---
title: Mixed Params Template
parameters:
  - name: safe_param
    type: text
  - name: unsafe-param
    type: text
  - name: also_safe
    type: text
---

Content";

        _mockPageService
            .Setup(x => x.GetPageAsync("_templates/mixed-params", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WikiPage
            {
                PageName = "_templates/mixed-params",
                Content = WikiPageContentParser.Parse(pageContent),
                ContentHash = "hash",
                Title = "Mixed Params Template",
                Culture = null
            });

        // Act
        var templates = await _service.GetAllTemplatesAsync(mockUser, CancellationToken.None);

        // Assert
        Assert.Single(templates);
        var template = templates[0];
        Assert.Equal(2, template.Parameters.Count);
        Assert.Contains(template.Parameters, p => p.Name == "safe_param");
        Assert.Contains(template.Parameters, p => p.Name == "also_safe");
        Assert.DoesNotContain(template.Parameters, p => p.Name == "unsafe-param");
        Assert.Single(template.InvalidParameterNames);
        Assert.Contains("unsafe-param", template.InvalidParameterNames);
    }

    #endregion
}
