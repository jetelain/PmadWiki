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
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

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
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

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
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

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
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

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
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

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
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

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
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

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
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

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
    public void Build_WithRequestedCulturePage_UsesCulturePage()
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
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

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
    public void Build_WithMultipleCulturesSamePageName_PrefersRequestedCulture()
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
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

        // Assert
        Assert.Single(result);
        var node = result[0];
        Assert.Equal("Home Page English", node.DisplayName);
        Assert.Equal("en", node.Culture);
        Assert.Equal(DateTimeOffset.Parse("2024-01-01T10:00:00Z"), node.LastModified);
        Assert.Equal("user1", node.LastModifiedBy);
    }

    [Fact]
    public void Build_WithRequestedCultureAndNullCulture_PrefersRequestedCulture()
    {
        // Arrange: both null and "en" cultures exist; "en" is requested so it should win
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
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

        // Assert
        Assert.Single(result);
        var node = result[0];
        Assert.Equal("Home Page English", node.DisplayName);
        Assert.Equal("en", node.Culture);
        Assert.Equal(DateTimeOffset.Parse("2024-01-02T11:00:00Z"), node.LastModified);
        Assert.Equal("user2", node.LastModifiedBy);
    }

    [Fact]
    public void Build_WithNoCultureMatchAndNullCultureFallback_UsesNullCulture()
    {
        // Arrange: no "en" page exists, but a null-culture page does; null culture is the second fallback
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
                Title = "Page d'accueil", 
                Culture = "fr",
                LastModified = DateTimeOffset.Parse("2024-01-02T11:00:00Z"),
                LastModifiedBy = "user2"
            }
        };
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

        // Assert
        Assert.Single(result);
        var node = result[0];
        Assert.Equal("Home Page Neutral", node.DisplayName);
        Assert.Null(node.Culture);
        Assert.Equal(DateTimeOffset.Parse("2024-01-01T10:00:00Z"), node.LastModified);
        Assert.Equal("user1", node.LastModifiedBy);
    }

    [Fact]
    public void Build_WithOnlyNonRequestedCulture_UsesFirstAlphabeticalCulture()
    {
        // Arrange: no "en" and no null-culture page; falls back to first culture alphabetically
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo 
            { 
                PageName = "Home", 
                Title = "Page d'accueil", 
                Culture = "fr",
                LastModified = DateTimeOffset.Parse("2024-01-01T10:00:00Z"),
                LastModifiedBy = "user1"
            },
            new WikiPageInfo 
            { 
                PageName = "Home", 
                Title = "Startseite", 
                Culture = "de",
                LastModified = DateTimeOffset.Parse("2024-01-02T11:00:00Z"),
                LastModifiedBy = "user2"
            }
        };
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

        // Assert: "de" comes before "fr" alphabetically
        Assert.Single(result);
        var node = result[0];
        Assert.Equal("Startseite", node.DisplayName);
        Assert.Equal("de", node.Culture);
        Assert.Equal(DateTimeOffset.Parse("2024-01-02T11:00:00Z"), node.LastModified);
        Assert.Equal("user2", node.LastModifiedBy);
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
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

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
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

        // Assert
        Assert.Single(result);
        var parent = result[0];
        Assert.Equal(3, parent.Children.Count);
        Assert.Equal("Docs/Apple", parent.Children[0].PageName);
        Assert.Equal("Docs/Mango", parent.Children[1].PageName);
        Assert.Equal("Docs/Zebra", parent.Children[2].PageName);
    }

    #endregion

    #region Build Tests - Sort Order

    [Fact]
    public void Build_WithSortOrder_SortsBeforeAlphabetical()
    {
        // Arrange: pages have reverse-alphabetical names but ascending sort orders
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Zebra", Title = "Zebra", Culture = null, SortOrder = 1 },
            new WikiPageInfo { PageName = "Mango", Title = "Mango", Culture = null, SortOrder = 2 },
            new WikiPageInfo { PageName = "Apple", Title = "Apple", Culture = null, SortOrder = 3 }
        };

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, "en");

        // Assert: sorted by SortOrder, not alphabetically by name
        Assert.Equal(3, result.Count);
        Assert.Equal("Zebra", result[0].PageName);
        Assert.Equal("Mango", result[1].PageName);
        Assert.Equal("Apple", result[2].PageName);
    }

    [Fact]
    public void Build_WithSameSortOrder_FallsBackToAlphabeticalByDisplayName()
    {
        // Arrange: all pages have the same non-zero sort order
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Zebra", Title = "Zebra", Culture = null, SortOrder = 5 },
            new WikiPageInfo { PageName = "Apple", Title = "Apple", Culture = null, SortOrder = 5 },
            new WikiPageInfo { PageName = "Mango", Title = "Mango", Culture = null, SortOrder = 5 }
        };

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, "en");

        // Assert: same SortOrder falls back to alphabetical by DisplayName (Title)
        Assert.Equal(3, result.Count);
        Assert.Equal("Apple", result[0].PageName);
        Assert.Equal("Mango", result[1].PageName);
        Assert.Equal("Zebra", result[2].PageName);
    }

    [Fact]
    public void Build_WithMixedDefaultAndExplicitSortOrder_SortsCorrectly()
    {
        // Arrange: some pages use default sort order (0), others use explicit values
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Zebra", Title = "Zebra", Culture = null, SortOrder = 0 },
            new WikiPageInfo { PageName = "Mango", Title = "Mango", Culture = null, SortOrder = 1 },
            new WikiPageInfo { PageName = "Apple", Title = "Apple", Culture = null, SortOrder = 0 }
        };

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, "en");

        // Assert: SortOrder=0 pages first sorted alphabetically, then SortOrder=1
        Assert.Equal(3, result.Count);
        Assert.Equal("Apple", result[0].PageName);
        Assert.Equal("Zebra", result[1].PageName);
        Assert.Equal("Mango", result[2].PageName);
    }

    [Fact]
    public void Build_WithNegativeSortOrder_AppearsBeforeDefaultSortOrder()
    {
        // Arrange: a page with a negative sort order should appear before pages with default (0)
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Apple", Title = "Apple", Culture = null, SortOrder = 0 },
            new WikiPageInfo { PageName = "Pinned", Title = "Pinned", Culture = null, SortOrder = -1 },
            new WikiPageInfo { PageName = "Zebra", Title = "Zebra", Culture = null, SortOrder = 0 }
        };

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, "en");

        // Assert: Pinned (SortOrder=-1) appears before the default-order pages
        Assert.Equal(3, result.Count);
        Assert.Equal("Pinned", result[0].PageName);
        Assert.Equal("Apple", result[1].PageName);
        Assert.Equal("Zebra", result[2].PageName);
    }

    [Fact]
    public void Build_WithSortOrderOnChildren_SortsChildrenByOrder()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Docs/Zebra", Title = "Zebra", Culture = null, SortOrder = 1 },
            new WikiPageInfo { PageName = "Docs/Apple", Title = "Apple", Culture = null, SortOrder = 3 },
            new WikiPageInfo { PageName = "Docs/Mango", Title = "Mango", Culture = null, SortOrder = 2 }
        };

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, "en");

        // Assert: children are sorted by SortOrder, not alphabetically
        Assert.Single(result);
        var children = result[0].Children;
        Assert.Equal(3, children.Count);
        Assert.Equal("Docs/Zebra", children[0].PageName);
        Assert.Equal("Docs/Mango", children[1].PageName);
        Assert.Equal("Docs/Apple", children[2].PageName);
    }

    [Fact]
    public void Build_WithSortOrderInComplexHierarchy_SortsAtEachLevelIndependently()
    {
        // Arrange: different sort orders at root and child levels
        var pages = new List<WikiPageInfo>
        {
            // Root level: B(1) should appear before A(2)
            new WikiPageInfo { PageName = "A", Title = "A", Culture = null, SortOrder = 2 },
            new WikiPageInfo { PageName = "B", Title = "B", Culture = null, SortOrder = 1 },
            // A's children: A/Y(1) should appear before A/X(2)
            new WikiPageInfo { PageName = "A/X", Title = "X", Culture = null, SortOrder = 2 },
            new WikiPageInfo { PageName = "A/Y", Title = "Y", Culture = null, SortOrder = 1 },
            // B's children: B/Q(0) should appear before B/P(1)
            new WikiPageInfo { PageName = "B/P", Title = "P", Culture = null, SortOrder = 1 },
            new WikiPageInfo { PageName = "B/Q", Title = "Q", Culture = null, SortOrder = 0 }
        };

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, "en");

        // Assert root level
        Assert.Equal(2, result.Count);
        Assert.Equal("B", result[0].PageName);
        Assert.Equal("A", result[1].PageName);

        // Assert A's children
        var aChildren = result[1].Children;
        Assert.Equal(2, aChildren.Count);
        Assert.Equal("A/Y", aChildren[0].PageName);
        Assert.Equal("A/X", aChildren[1].PageName);

        // Assert B's children
        var bChildren = result[0].Children;
        Assert.Equal(2, bChildren.Count);
        Assert.Equal("B/Q", bChildren[0].PageName);
        Assert.Equal("B/P", bChildren[1].PageName);
    }

    [Fact]
    public void Build_WithSortOrder_SortOrderIsExposedOnNode()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Home", Title = "Home", Culture = null, SortOrder = 42 }
        };

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, "en");

        // Assert: SortOrder value is propagated to the site map node
        Assert.Single(result);
        Assert.Equal(42, result[0].SortOrder);
    }

    [Fact]
    public void Build_WithGhostNodeParent_GhostNodeHasDefaultSortOrder()
    {
        // Arrange: only a child page exists, so the parent is created as a ghost node
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Docs/Guide", Title = "Guide", Culture = null, SortOrder = 5 }
        };

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, "en");

        // Assert: ghost "Docs" node defaults to SortOrder=0; real child node carries SortOrder=5
        Assert.Single(result);
        Assert.False(result[0].HasPage);
        Assert.Equal(0, result[0].SortOrder);
        Assert.Single(result[0].Children);
        Assert.Equal(5, result[0].Children[0].SortOrder);
    }

    [Fact]
    public void Build_WithSortOrderSortsByDisplayNameNotPageName()
    {
        // Arrange: page names are alphabetical but titles (used as DisplayName) are not
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "page-a", Title = "Zebra", Culture = null, SortOrder = 1 },
            new WikiPageInfo { PageName = "page-b", Title = "Apple", Culture = null, SortOrder = 1 },
            new WikiPageInfo { PageName = "page-c", Title = "Mango", Culture = null, SortOrder = 1 }
        };

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, "en");

        // Assert: same SortOrder falls back to DisplayName (Title) order, not PageName order
        Assert.Equal(3, result.Count);
        Assert.Equal("page-b", result[0].PageName); // Title "Apple"
        Assert.Equal("page-c", result[1].PageName); // Title "Mango"
        Assert.Equal("page-a", result[2].PageName); // Title "Zebra"
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
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

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
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

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
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

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
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

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
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

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
        var requestedCulture = "en";

        // Act
        var result = WikiSiteMapNodeHelper.Build(pages, requestedCulture);

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

    #region BuildSubPages Tests

    [Fact]
    public void BuildSubPages_WithFlatSubPages_ReturnsDirectChildrenOnly()
    {
        // Arrange: page "Foo" has sub-pages "Foo/Bar" and "Foo/Baz"
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Foo/Bar", Title = "Bar", Culture = null },
            new WikiPageInfo { PageName = "Foo/Baz", Title = "Baz", Culture = null }
        };

        // Act
        var result = WikiSiteMapNodeHelper.BuildSubPages(pages, "en", "Foo");

        // Assert: root nodes are Bar and Baz, not Foo
        Assert.Equal(2, result.Count);
        Assert.Equal("Foo/Bar", result[0].PageName);
        Assert.Equal("Bar", result[0].DisplayName);
        Assert.Equal(0, result[0].Level);
        Assert.Equal("Foo/Baz", result[1].PageName);
        Assert.Equal("Baz", result[1].DisplayName);
        Assert.Equal(0, result[1].Level);
    }

    [Fact]
    public void BuildSubPages_WithNestedPage_ReturnsCorrectHierarchy()
    {
        // Arrange: page "Foo/Bar" has sub-pages "Foo/Bar/Child1" and "Foo/Bar/Child1/Grand"
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Foo/Bar/Child1", Title = "Child1", Culture = null },
            new WikiPageInfo { PageName = "Foo/Bar/Child1/Grand", Title = "Grand", Culture = null }
        };

        // Act
        var result = WikiSiteMapNodeHelper.BuildSubPages(pages, "en", "Foo/Bar");

        // Assert: root is Child1 (level 0), not Foo or Bar
        Assert.Single(result);
        var child1 = result[0];
        Assert.Equal("Foo/Bar/Child1", child1.PageName);
        Assert.Equal("Child1", child1.DisplayName);
        Assert.Equal(0, child1.Level);
        Assert.True(child1.HasPage);

        Assert.Single(child1.Children);
        var grand = child1.Children[0];
        Assert.Equal("Foo/Bar/Child1/Grand", grand.PageName);
        Assert.Equal("Grand", grand.DisplayName);
        Assert.Equal(1, grand.Level);
        Assert.True(grand.HasPage);
    }

    [Fact]
    public void BuildSubPages_WithMissingIntermediateFolder_CreatesFolderNode()
    {
        // Arrange: page "Section" has sub-page "Section/Topic/Detail" with no "Section/Topic" page
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Section/Topic/Detail", Title = "Detail", Culture = null }
        };

        // Act
        var result = WikiSiteMapNodeHelper.BuildSubPages(pages, "en", "Section");

        // Assert: root is a folder node "Topic" (level 0), Detail is a child (level 1)
        Assert.Single(result);
        var folder = result[0];
        Assert.Equal("Section/Topic", folder.PageName);
        Assert.Equal("Topic", folder.DisplayName);
        Assert.False(folder.HasPage);
        Assert.Equal(0, folder.Level);

        Assert.Single(folder.Children);
        var detail = folder.Children[0];
        Assert.Equal("Section/Topic/Detail", detail.PageName);
        Assert.Equal("Detail", detail.DisplayName);
        Assert.True(detail.HasPage);
        Assert.Equal(1, detail.Level);
    }

    [Fact]
    public void BuildSubPages_WithEmptyList_ReturnsEmptyList()
    {
        var result = WikiSiteMapNodeHelper.BuildSubPages(new List<WikiPageInfo>(), "en", "Foo");
        Assert.Empty(result);
    }

    [Fact]
    public void BuildSubPages_WithRequestedCulture_UsesRequestedCulturePage()
    {
        // Arrange: sub-pages exist in both "en" and "fr"; "en" is requested
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Foo/Bar", Title = "Bar EN", Culture = "en" },
            new WikiPageInfo { PageName = "Foo/Bar", Title = "Bar FR", Culture = "fr" }
        };

        // Act
        var result = WikiSiteMapNodeHelper.BuildSubPages(pages, "en", "Foo");

        // Assert
        Assert.Single(result);
        Assert.Equal("Bar EN", result[0].DisplayName);
        Assert.Equal("en", result[0].Culture);
    }

    [Fact]
    public void BuildSubPages_WithNoRequestedCultureButNullCultureExists_FallsBackToNullCulture()
    {
        // Arrange: no "en" page, but a neutral (null) culture page exists
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Foo/Bar", Title = "Bar Neutral", Culture = null },
            new WikiPageInfo { PageName = "Foo/Bar", Title = "Bar FR", Culture = "fr" }
        };

        // Act
        var result = WikiSiteMapNodeHelper.BuildSubPages(pages, "en", "Foo");

        // Assert
        Assert.Single(result);
        Assert.Equal("Bar Neutral", result[0].DisplayName);
        Assert.Null(result[0].Culture);
    }

    [Fact]
    public void BuildSubPages_WithOnlyNonRequestedCulture_FallsBackToFirstAlphabeticalCulture()
    {
        // Arrange: neither "en" nor null-culture exists; falls back alphabetically
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Foo/Bar", Title = "Bar FR", Culture = "fr" },
            new WikiPageInfo { PageName = "Foo/Bar", Title = "Bar DE", Culture = "de" }
        };

        // Act
        var result = WikiSiteMapNodeHelper.BuildSubPages(pages, "en", "Foo");

        // Assert: "de" comes before "fr" alphabetically
        Assert.Single(result);
        Assert.Equal("Bar DE", result[0].DisplayName);
        Assert.Equal("de", result[0].Culture);
    }

    #endregion

    #region BuildSubPages Tests - Sort Order

    [Fact]
    public void BuildSubPages_WithSortOrder_SortsSubPagesByOrder()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            new WikiPageInfo { PageName = "Foo/Zebra", Title = "Zebra", Culture = null, SortOrder = 1 },
            new WikiPageInfo { PageName = "Foo/Apple", Title = "Apple", Culture = null, SortOrder = 3 },
            new WikiPageInfo { PageName = "Foo/Mango", Title = "Mango", Culture = null, SortOrder = 2 }
        };

        // Act
        var result = WikiSiteMapNodeHelper.BuildSubPages(pages, "en", "Foo");

        // Assert: sorted by SortOrder, not alphabetically
        Assert.Equal(3, result.Count);
        Assert.Equal("Foo/Zebra", result[0].PageName);
        Assert.Equal("Foo/Mango", result[1].PageName);
        Assert.Equal("Foo/Apple", result[2].PageName);
    }

    [Fact]
    public void BuildSubPages_WithSortOrderOnNestedChildren_SortsChildrenAtEachLevel()
    {
        // Arrange
        var pages = new List<WikiPageInfo>
        {
            // Root sub-pages of "Section": B(1) before A(2)
            new WikiPageInfo { PageName = "Section/A", Title = "A", Culture = null, SortOrder = 2 },
            new WikiPageInfo { PageName = "Section/B", Title = "B", Culture = null, SortOrder = 1 },
            // Children of Section/A: A/Y(1) before A/X(2)
            new WikiPageInfo { PageName = "Section/A/X", Title = "X", Culture = null, SortOrder = 2 },
            new WikiPageInfo { PageName = "Section/A/Y", Title = "Y", Culture = null, SortOrder = 1 }
        };

        // Act
        var result = WikiSiteMapNodeHelper.BuildSubPages(pages, "en", "Section");

        // Assert root of sub-pages
        Assert.Equal(2, result.Count);
        Assert.Equal("Section/B", result[0].PageName);
        Assert.Equal("Section/A", result[1].PageName);

        // Assert nested children
        var aChildren = result[1].Children;
        Assert.Equal(2, aChildren.Count);
        Assert.Equal("Section/A/Y", aChildren[0].PageName);
        Assert.Equal("Section/A/X", aChildren[1].PageName);
    }

    #endregion
}



