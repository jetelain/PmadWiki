using Pmad.Wiki.Helpers;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Helpers;

public class WikiSiteMapNodeHelperTest
{
    #region Build Tests - Single Level

    [Fact]
    public void Build_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var pages = new List<WikiPageInfo>();
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Build_WithSinglePage_ReturnsSingleRootNode()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo
            {
                PageName = "Home",
                Title = "Home Page",
                Culture = null,
                LastModified = DateTimeOffset.Parse("2024-01-01T10:00:00Z"),
                LastModifiedBy = "user1"
            }
        };
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        Assert.Single(result);
        var node = result[0];
        Assert.Equal("Home", node.PageName);
        Assert.Equal("Home Page", node.DisplayName);
        Assert.Equal("Home Page", node.Title);
        Assert.True(node.HasPage);
        Assert.Null(node.Culture);
        Assert.Equal(DateTimeOffset.Parse("2024-01-01T10:00:00Z"), node.LastModified);
        Assert.Equal("user1", node.LastModifiedBy);
        Assert.Equal(0, node.Level);
        Assert.Empty(node.Children);
    }

    [Fact]
    public void Build_WithMultipleSingleLevelPages_ReturnsMultipleRootNodes()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Home", Title = "Home Page", Culture = null },
            new WikiPageInfo { PageName = "About", Title = "About Us", Culture = null },
            new WikiPageInfo { PageName = "Contact", Title = "Contact Info", Culture = null }
        };
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("About", result[0].PageName);
        Assert.Equal("Contact", result[1].PageName);
        Assert.Equal("Home", result[2].PageName);
        Assert.All(result, node => Assert.True(node.HasPage));
        Assert.All(result, node => Assert.Equal(0, node.Level));
    }

    #endregion

    #region Build Tests - Hierarchical Structure

    [Fact]
    public void Build_WithTwoLevelHierarchy_CreatesParentChildRelationship()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Docs/Guide", Title = "User Guide", Culture = null }
        };
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        Assert.Single(result);
        var parent = result[0];
        Assert.Equal("Docs", parent.PageName);
        Assert.Equal("Docs", parent.DisplayName);
        Assert.False(parent.HasPage);
        Assert.Equal(0, parent.Level);
        Assert.Single(parent.Children);

        var child = parent.Children[0];
        Assert.Equal("Docs/Guide", child.PageName);
        Assert.Equal("User Guide", child.DisplayName);
        Assert.True(child.HasPage);
        Assert.Equal(1, child.Level);
    }

    [Fact]
    public void Build_WithThreeLevelHierarchy_CreatesDeepNesting()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Docs/API/Getting-Started", Title = "Getting Started", Culture = null }
        };
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        Assert.Single(result);
        
        var level0 = result[0];
        Assert.Equal("Docs", level0.PageName);
        Assert.False(level0.HasPage);
        Assert.Equal(0, level0.Level);
        
        var level1 = level0.Children[0];
        Assert.Equal("Docs/API", level1.PageName);
        Assert.False(level1.HasPage);
        Assert.Equal(1, level1.Level);
        
        var level2 = level1.Children[0];
        Assert.Equal("Docs/API/Getting-Started", level2.PageName);
        Assert.Equal("Getting Started", level2.DisplayName);
        Assert.True(level2.HasPage);
        Assert.Equal(2, level2.Level);
    }

    [Fact]
    public void Build_WithBothParentAndChildPages_CreatesBothNodes()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Docs", Title = "Documentation", Culture = null },
            new WikiPageInfo { PageName = "Docs/Guide", Title = "User Guide", Culture = null }
        };
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        Assert.Single(result);
        var parent = result[0];
        Assert.Equal("Docs", parent.PageName);
        Assert.Equal("Documentation", parent.DisplayName);
        Assert.True(parent.HasPage);
        Assert.Single(parent.Children);

        var child = parent.Children[0];
        Assert.Equal("Docs/Guide", child.PageName);
        Assert.Equal("User Guide", child.DisplayName);
        Assert.True(child.HasPage);
    }

    [Fact]
    public void Build_WithMultipleChildrenUnderOneParent_CreatesMultipleChildren()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Docs/Guide", Title = "User Guide", Culture = null },
            new WikiPageInfo { PageName = "Docs/API", Title = "API Reference", Culture = null },
            new WikiPageInfo { PageName = "Docs/FAQ", Title = "FAQ", Culture = null }
        };
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        Assert.Single(result);
        var parent = result[0];
        Assert.Equal("Docs", parent.PageName);
        Assert.Equal(3, parent.Children.Count);
        Assert.Equal("Docs/API", parent.Children[0].PageName);
        Assert.Equal("Docs/FAQ", parent.Children[1].PageName);
        Assert.Equal("Docs/Guide", parent.Children[2].PageName);
    }

    [Fact]
    public void Build_WithComplexHierarchy_CreatesCorrectStructure()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Home", Title = "Home", Culture = null },
            new WikiPageInfo { PageName = "Docs", Title = "Documentation", Culture = null },
            new WikiPageInfo { PageName = "Docs/Guide", Title = "User Guide", Culture = null },
            new WikiPageInfo { PageName = "Docs/Guide/Install", Title = "Installation", Culture = null },
            new WikiPageInfo { PageName = "Docs/Guide/Config", Title = "Configuration", Culture = null },
            new WikiPageInfo { PageName = "Docs/API", Title = "API Reference", Culture = null },
            new WikiPageInfo { PageName = "About", Title = "About", Culture = null }
        };
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("About", result[0].PageName);
        Assert.Equal("Docs", result[1].PageName);
        Assert.Equal("Home", result[2].PageName);

        var docs = result[1];
        Assert.Equal(2, docs.Children.Count);
        Assert.Equal("Docs/API", docs.Children[0].PageName);
        Assert.Equal("Docs/Guide", docs.Children[1].PageName);

        var guide = docs.Children[1];
        Assert.Equal(2, guide.Children.Count);
        Assert.Equal("Docs/Guide/Config", guide.Children[0].PageName);
        Assert.Equal("Docs/Guide/Install", guide.Children[1].PageName);
    }

    #endregion

    #region Build Tests - Culture Support

    [Fact]
    public void Build_WithNeutralCulturePage_UsesCulturePage()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo 
            { 
                PageName = "Home", 
                Title = "Home Page", 
                Culture = "en",
                LastModified = DateTimeOffset.Parse("2024-01-01T10:00:00Z"),
                LastModifiedBy = "user1"
            }
        };
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        Assert.Single(result);
        var node = result[0];
        Assert.Equal("Home", node.PageName);
        Assert.Equal("Home Page", node.DisplayName);
        Assert.Equal("en", node.Culture);
        Assert.Equal(DateTimeOffset.Parse("2024-01-01T10:00:00Z"), node.LastModified);
        Assert.Equal("user1", node.LastModifiedBy);
    }

    [Fact]
    public void Build_WithMultipleCulturesSamePageName_PrefersNeutralCulture()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo 
            { 
                PageName = "Home", 
                Title = "Home Page English", 
                Culture = "en",
                LastModified = DateTimeOffset.Parse("2024-01-01T10:00:00Z"),
                LastModifiedBy = "user1"
            },
            new WikiPageInfo 
            { 
                PageName = "Home", 
                Title = "Page d'accueil", 
                Culture = "fr",
                LastModified = DateTimeOffset.Parse("2024-01-02T11:00:00Z"),
                LastModifiedBy = "user2"
            }
        };
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        Assert.Single(result);
        var node = result[0];
        Assert.Equal("Home Page English", node.DisplayName);
        Assert.Equal("en", node.Culture);
        Assert.Equal(DateTimeOffset.Parse("2024-01-01T10:00:00Z"), node.LastModified);
        Assert.Equal("user1", node.LastModifiedBy);
    }

    [Fact]
    public void Build_WithNullCultureAndNeutralCulture_PrefersNullCulture()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo 
            { 
                PageName = "Home", 
                Title = "Home Page Neutral", 
                Culture = null,
                LastModified = DateTimeOffset.Parse("2024-01-01T10:00:00Z"),
                LastModifiedBy = "user1"
            },
            new WikiPageInfo 
            { 
                PageName = "Home", 
                Title = "Home Page English", 
                Culture = "en",
                LastModified = DateTimeOffset.Parse("2024-01-02T11:00:00Z"),
                LastModifiedBy = "user2"
            }
        };
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        Assert.Single(result);
        var node = result[0];
        Assert.Equal("Home Page Neutral", node.DisplayName);
        Assert.Null(node.Culture);
        Assert.Equal(DateTimeOffset.Parse("2024-01-01T10:00:00Z"), node.LastModified);
        Assert.Equal("user1", node.LastModifiedBy);
    }

    [Fact]
    public void Build_WithOnlyNonNeutralCulture_UsesFirstAvailableCulture()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo 
            { 
                PageName = "Home", 
                Title = "Page d'accueil", 
                Culture = "fr",
                LastModified = DateTimeOffset.Parse("2024-01-01T10:00:00Z"),
                LastModifiedBy = "user1"
            }
        };
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        Assert.Single(result);
        var node = result[0];
        Assert.Equal("Home", node.PageName);
        Assert.Equal("Home", node.DisplayName);
        Assert.Null(node.Title);
        Assert.Null(node.Culture);
        Assert.Null(node.LastModified);
        Assert.Null(node.LastModifiedBy);
    }

    #endregion

    #region Build Tests - Ordering

    [Fact]
    public void Build_WithUnorderedPages_ReturnsAlphabeticallySorted()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Zebra", Title = "Zebra", Culture = null },
            new WikiPageInfo { PageName = "Apple", Title = "Apple", Culture = null },
            new WikiPageInfo { PageName = "Mango", Title = "Mango", Culture = null }
        };
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Apple", result[0].PageName);
        Assert.Equal("Mango", result[1].PageName);
        Assert.Equal("Zebra", result[2].PageName);
    }

    [Fact]
    public void Build_WithUnorderedChildPages_ReturnsSortedChildren()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Docs/Zebra", Title = "Zebra", Culture = null },
            new WikiPageInfo { PageName = "Docs/Apple", Title = "Apple", Culture = null },
            new WikiPageInfo { PageName = "Docs/Mango", Title = "Mango", Culture = null }
        };
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        Assert.Single(result);
        var parent = result[0];
        Assert.Equal(3, parent.Children.Count);
        Assert.Equal("Docs/Apple", parent.Children[0].PageName);
        Assert.Equal("Docs/Mango", parent.Children[1].PageName);
        Assert.Equal("Docs/Zebra", parent.Children[2].PageName);
    }

    #endregion

    #region Build Tests - Missing Title Fallback

    [Fact]
    public void Build_WithoutTitle_UsesPageNameAsDisplayName()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo 
            { 
                PageName = "Home", 
                Title = null,
                Culture = null 
            }
        };
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        Assert.Single(result);
        var node = result[0];
        Assert.Equal("Home", node.PageName);
        Assert.Equal("Home", node.DisplayName);
        Assert.Null(node.Title);
    }

    [Fact]
    public void Build_WithHierarchyWithoutTitle_UsesLastPartOfPathAsDisplayName()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo 
            { 
                PageName = "Docs/API/Reference", 
                Title = null,
                Culture = null 
            }
        };
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        var level0 = result[0];
        Assert.Equal("Docs", level0.DisplayName);
        
        var level1 = level0.Children[0];
        Assert.Equal("API", level1.DisplayName);
        
        var level2 = level1.Children[0];
        Assert.Equal("Reference", level2.DisplayName);
    }

    #endregion

    #region Build Tests - Edge Cases

    [Fact]
    public void Build_WithSingleCharacterPageNames_WorksCorrectly()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "A", Title = "A Title", Culture = null },
            new WikiPageInfo { PageName = "B", Title = "B Title", Culture = null },
            new WikiPageInfo { PageName = "A/X", Title = "X Title", Culture = null }
        };
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].PageName);
        Assert.Equal("B", result[1].PageName);
        Assert.Single(result[0].Children);
        Assert.Equal("A/X", result[0].Children[0].PageName);
    }

    [Fact]
    public void Build_WithHyphenatedAndUnderscoredNames_WorksCorrectly()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "my-page", Title = "My Page", Culture = null },
            new WikiPageInfo { PageName = "my_other_page", Title = "My Other Page", Culture = null },
            new WikiPageInfo { PageName = "category/sub-category/my-page", Title = "Nested Page", Culture = null }
        };
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("category", result[0].PageName);
        Assert.Equal("my_other_page", result[1].PageName);
        Assert.Equal("my-page", result[2].PageName);
    }

    [Fact]
    public void Build_WithDeepNesting_HandlesCorrectly()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo 
            { 
                PageName = "A/B/C/D/E/F/G/H/I/J", 
                Title = "Deep Page", 
                Culture = null 
            }
        };
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        Assert.Single(result);
        var current = result[0];
        for (int i = 0; i < 9; i++)
        {
            Assert.False(current.HasPage);
            Assert.Equal(i, current.Level);
            Assert.Single(current.Children);
            current = current.Children[0];
        }
        Assert.True(current.HasPage);
        Assert.Equal(9, current.Level);
        Assert.Equal("Deep Page", current.DisplayName);
    }

    [Fact]
    public void Build_WithMultipleCulturesInHierarchy_HandlesCorrectly()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Docs", Title = "Documentation EN", Culture = "en" },
            new WikiPageInfo { PageName = "Docs", Title = "Documentation FR", Culture = "fr" },
            new WikiPageInfo { PageName = "Docs/Guide", Title = "Guide EN", Culture = "en" },
            new WikiPageInfo { PageName = "Docs/Guide", Title = "Guide FR", Culture = "fr" }
        };
        var neutralCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, neutralCulture);

        // Assert
        Assert.Single(result);
        var docs = result[0];
        Assert.Equal("Documentation EN", docs.DisplayName);
        Assert.Equal("en", docs.Culture);
        Assert.Single(docs.Children);
        
        var guide = docs.Children[0];
        Assert.Equal("Guide EN", guide.DisplayName);
        Assert.Equal("en", guide.Culture);
    }

    #endregion
}
