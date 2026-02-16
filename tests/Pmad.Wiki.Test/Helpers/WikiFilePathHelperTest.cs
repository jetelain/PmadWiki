using Pmad.Wiki.Helpers;

namespace Pmad.Wiki.Test.Helpers;

public class WikiFilePathHelperTest
{
    [Fact]
    public void GetFilePath_WithNullCulture_ReturnsBaseFile()
    {
        // Act
        var path = WikiFilePathHelper.GetFilePath("test", null, "en");

        // Assert
        Assert.Equal("test.md", path);
    }

    [Fact]
    public void GetFilePath_WithNeutralCulture_ReturnsBaseFile()
    {
        // Act
        var path = WikiFilePathHelper.GetFilePath("test", "en", "en");

        // Assert
        Assert.Equal("test.md", path);
    }

    [Fact]
    public void GetFilePath_WithDifferentCulture_ReturnsLocalizedFile()
    {
        // Act
        var path = WikiFilePathHelper.GetFilePath("test", "fr", "en");

        // Assert
        Assert.Equal("test.fr.md", path);
    }

    [Fact]
    public void GetFilePath_WithNestedPage_ReturnsCorrectPath()
    {
        // Act
        var path = WikiFilePathHelper.GetFilePath("admin/settings", null, "en");

        // Assert
        Assert.Equal("admin/settings.md", path);
    }

    [Fact]
    public void GetFilePath_WithNestedPageAndCulture_ReturnsCorrectPath()
    {
        // Act
        var path = WikiFilePathHelper.GetFilePath("admin/settings", "fr", "en");

        // Assert
        Assert.Equal("admin/settings.fr.md", path);
    }

    [Fact]
    public void GetBaseFileName_RemovesLeadingSlash()
    {
        // Act
        var result = WikiFilePathHelper.GetBaseFileName("/test/page");

        // Assert
        Assert.Equal("test/page", result);
    }

    [Fact]
    public void GetBaseFileName_RemovesTrailingSlash()
    {
        // Act
        var result = WikiFilePathHelper.GetBaseFileName("test/page/");

        // Assert
        Assert.Equal("test/page", result);
    }

    [Fact]
    public void GetBaseFileName_NormalizesBackslashToForwardSlash()
    {
        // Act
        var result = WikiFilePathHelper.GetBaseFileName("test\\page");

        // Assert
        Assert.Equal("test/page", result);
    }

    [Fact]
    public void ParsePagePath_WithNeutralCulture_ReturnsPageNameAndNullCulture()
    {
        // Act
        var (pageName, culture) = WikiFilePathHelper.ParsePagePath("test.md");

        // Assert
        Assert.Equal("test", pageName);
        Assert.Null(culture);
    }

    [Fact]
    public void ParsePagePath_WithCultureSuffix_ReturnsPageNameAndCulture()
    {
        // Act
        var (pageName, culture) = WikiFilePathHelper.ParsePagePath("test.fr.md");

        // Assert
        Assert.Equal("test", pageName);
        Assert.Equal("fr", culture);
    }

    [Fact]
    public void ParsePagePath_WithNestedPath_ReturnsCorrectPageName()
    {
        // Act
        var (pageName, culture) = WikiFilePathHelper.ParsePagePath("admin/settings.md");

        // Assert
        Assert.Equal("admin/settings", pageName);
        Assert.Null(culture);
    }

    [Fact]
    public void ParsePagePath_WithNestedPathAndCulture_ReturnsCorrectPageNameAndCulture()
    {
        // Act
        var (pageName, culture) = WikiFilePathHelper.ParsePagePath("admin/settings.fr.md");

        // Assert
        Assert.Equal("admin/settings", pageName);
        Assert.Equal("fr", culture);
    }

    [Fact]
    public void ParsePagePath_WithInvalidCultureSuffix_TreatsAsPartOfName()
    {
        // Act
        var (pageName, culture) = WikiFilePathHelper.ParsePagePath("test.invalid.md");

        // Assert
        Assert.Equal("test.invalid", pageName);
        Assert.Null(culture);
    }

    [Fact]
    public void IsLocalizedVersionOfPage_WithNeutralCulture_ReturnsTrue()
    {
        // Act
        var result = WikiFilePathHelper.IsLocalizedVersionOfPage("test.md", "test", "en", out var culture);

        // Assert
        Assert.True(result);
        Assert.Equal("en", culture);
    }

    [Fact]
    public void IsLocalizedVersionOfPage_WithMatchingCulture_ReturnsTrue()
    {
        // Act
        var result = WikiFilePathHelper.IsLocalizedVersionOfPage("test.fr.md", "test", "en", out var culture);

        // Assert
        Assert.True(result);
        Assert.Equal("fr", culture);
    }

    [Fact]
    public void IsLocalizedVersionOfPage_WithDifferentPage_ReturnsFalse()
    {
        // Act
        var result = WikiFilePathHelper.IsLocalizedVersionOfPage("other.md", "test", "en", out var culture);

        // Assert
        Assert.False(result);
        Assert.Null(culture);
    }

    [Fact]
    public void IsLocalizedVersionOfPage_WithInvalidCulture_ReturnsFalse()
    {
        // Act
        var result = WikiFilePathHelper.IsLocalizedVersionOfPage("test.invalid.md", "test", "en", out var culture);

        // Assert
        Assert.False(result);
        Assert.Null(culture);
    }

    [Fact]
    public void GetFilePath_WithInvalidPageName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            WikiFilePathHelper.GetFilePath("invalid name", null, "en"));
    }

    [Fact]
    public void GetFilePath_WithInvalidCulture_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            WikiFilePathHelper.GetFilePath("test", "INVALID", "en"));
    }

    [Fact]
    public void GetRelativePath_SameDirectory_ReturnsPageName()
    {
        // Act
        var result = WikiFilePathHelper.GetRelativePath("admin/page1", "admin/page2");

        // Assert
        Assert.Equal("page2", result);
    }

    [Fact]
    public void GetRelativePath_SamePage_ReturnsPageName()
    {
        // Act
        var result = WikiFilePathHelper.GetRelativePath("home", "home");

        // Assert
        Assert.Equal("home", result);
    }

    [Fact]
    public void GetRelativePath_FromRootToNested_ReturnsFullPath()
    {
        // Act
        var result = WikiFilePathHelper.GetRelativePath("home", "admin/settings");

        // Assert
        Assert.Equal("admin/settings", result);
    }

    [Fact]
    public void GetRelativePath_FromNestedToRoot_ReturnsParentPath()
    {
        // Act
        var result = WikiFilePathHelper.GetRelativePath("admin/settings", "home");

        // Assert
        Assert.Equal("../home", result);
    }

    [Fact]
    public void GetRelativePath_BetweenDifferentNestedPaths_ReturnsCorrectPath()
    {
        // Act
        var result = WikiFilePathHelper.GetRelativePath("admin/settings", "docs/help");

        // Assert
        Assert.Equal("../docs/help", result);
    }

    [Fact]
    public void GetRelativePath_DeeplyNestedToRoot_ReturnsMultipleParents()
    {
        // Act
        var result = WikiFilePathHelper.GetRelativePath("admin/users/settings", "home");

        // Assert
        Assert.Equal("../../home", result);
    }

    [Fact]
    public void GetRelativePath_BetweenDeeplyNestedPaths_ReturnsCorrectPath()
    {
        // Act
        var result = WikiFilePathHelper.GetRelativePath("admin/users/permissions", "docs/api/reference");

        // Assert
        Assert.Equal("../../docs/api/reference", result);
    }

    [Fact]
    public void GetRelativePath_WithCommonPrefix_ReturnsCorrectPath()
    {
        // Act
        var result = WikiFilePathHelper.GetRelativePath("admin/users/list", "admin/groups/list");

        // Assert
        Assert.Equal("../groups/list", result);
    }

    [Fact]
    public void GetRelativePath_ParentPage_ReturnsCorrectPath()
    {
        // Act
        var result = WikiFilePathHelper.GetRelativePath("home/subpage", "home");

        // Assert
        Assert.Equal("../home", result);
    }

    [Fact]
    public void GetDirectoryName_WithNoSlash_ReturnsEmptyString()
    {
        // Act
        var result = WikiFilePathHelper.GetDirectoryName("home");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetDirectoryName_WithSingleLevel_ReturnsDirectory()
    {
        // Act
        var result = WikiFilePathHelper.GetDirectoryName("admin/settings");

        // Assert
        Assert.Equal("admin", result);
    }

    [Fact]
    public void GetDirectoryName_WithMultipleLevels_ReturnsFullDirectoryPath()
    {
        // Act
        var result = WikiFilePathHelper.GetDirectoryName("admin/users/permissions");

        // Assert
        Assert.Equal("admin/users", result);
    }

    [Fact]
    public void GetDirectoryName_WithDeeplyNested_ReturnsFullDirectoryPath()
    {
        // Act
        var result = WikiFilePathHelper.GetDirectoryName("a/b/c/d/page");

        // Assert
        Assert.Equal("a/b/c/d", result);
    }

    [Fact]
    public void GetDirectoryName_WithEmptyString_ReturnsEmptyString()
    {
        // Act
        var result = WikiFilePathHelper.GetDirectoryName("");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetDirectoryName_WithNull_ReturnsEmptyString()
    {
        // Act
        var result = WikiFilePathHelper.GetDirectoryName(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("_templates/simple")]
    [InlineData("_templates/blog/post")]
    [InlineData("_templates/docs/api/reference")]
    [InlineData("_TEMPLATES/article")]
    [InlineData("_TeMpLaTeS/mixed")]
    [InlineData("docs/_template")]
    [InlineData("admin/settings/_template")]
    [InlineData("a/b/c/_template")]
    [InlineData("docs/_TEMPLATE")]
    [InlineData("admin/_TeMpLaTe")]
    public void IsTemplatePageName_WithValidNames_ReturnsTrue(string pageName)
    {
        // Act
        var result = WikiFilePathHelper.IsTemplatePageName(pageName);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("Home")]
    [InlineData("About")]
    [InlineData("docs/readme")]
    [InlineData("admin/settings")]
    [InlineData("_template-page")]
    [InlineData("page-_template")]
    [InlineData("_templates")]
    [InlineData("docs/_templates")]
    [InlineData("_templ")]
    [InlineData("template")]
    [InlineData("templates/page")]
    public void IsTemplatePageName_WithNonTemplatePages_ReturnsFalse(string pageName)
    {
        // Act
        var result = WikiFilePathHelper.IsTemplatePageName(pageName);

        // Assert
        Assert.False(result);
    }
}

