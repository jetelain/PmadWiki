using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Services;

public class PageAccessRuleTest
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var rule = new PageAccessRule("admin/**", ["admin"], ["admin"], 0);

        // Assert
        Assert.Equal("admin/**", rule.Pattern);
        Assert.Equal(["admin"], rule.ReadGroups);
        Assert.Equal(["admin"], rule.WriteGroups);
        Assert.Equal(0, rule.Order);
        Assert.NotNull(rule.CompiledPattern);
    }

    [Fact]
    public void Constructor_WithEmptyGroups_CreatesInstance()
    {
        // Arrange & Act
        var rule = new PageAccessRule("public/**", [], [], 0);

        // Assert
        Assert.Empty(rule.ReadGroups);
        Assert.Empty(rule.WriteGroups);
    }

    [Fact]
    public void Constructor_WithMultipleGroups_CreatesInstance()
    {
        // Arrange & Act
        var rule = new PageAccessRule("docs/**", ["readers", "editors", "admins"], ["editors", "admins"], 1);

        // Assert
        Assert.Equal(3, rule.ReadGroups.Length);
        Assert.Equal(2, rule.WriteGroups.Length);
        Assert.Contains("readers", rule.ReadGroups);
        Assert.Contains("editors", rule.ReadGroups);
        Assert.Contains("admins", rule.ReadGroups);
    }

    #endregion

    #region Pattern Matching - Double Wildcard Tests

    [Theory]
    [InlineData("admin/**", "admin/settings", true)]
    [InlineData("admin/**", "admin/users/list", true)]
    [InlineData("admin/**", "admin/users/roles/edit", true)]
    [InlineData("admin/**", "admin", false)]
    [InlineData("admin/**", "administrator", false)]
    [InlineData("admin/**", "docs/admin", false)]
    [InlineData("admin/**", "public/page", false)]
    public void Matches_WithDoubleWildcard_MatchesNestedPaths(string pattern, string pageName, bool expected)
    {
        // Arrange
        var rule = new PageAccessRule(pattern, [], [], 0);

        // Act
        var result = rule.Matches(pageName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("docs/**", "docs/readme", true)]
    [InlineData("docs/**", "docs/guides/intro", true)]
    [InlineData("docs/**", "docs/guides/advanced/topics", true)]
    [InlineData("**/internal", "admin/internal", true)]
    [InlineData("**/internal", "docs/guides/internal", true)]
    [InlineData("prefix/**/suffix", "prefix/middle/suffix", true)]
    public void Matches_WithDoubleWildcard_MatchesAnyDepth(string pattern, string pageName, bool expected)
    {
        // Arrange
        var rule = new PageAccessRule(pattern, [], [], 0);

        // Act
        var result = rule.Matches(pageName);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Pattern Matching - Single Wildcard Tests

    [Theory]
    [InlineData("docs/*", "docs/readme", true)]
    [InlineData("docs/*", "docs/intro", true)]
    [InlineData("docs/*", "docs/guides/intro", false)]
    [InlineData("docs/*", "docs/guides/advanced/topics", false)]
    [InlineData("docs/*", "docs", false)]
    [InlineData("admin/*/settings", "admin/users/settings", true)]
    [InlineData("admin/*/settings", "admin/roles/settings", true)]
    [InlineData("admin/*/settings", "admin/users/roles/settings", false)]
    public void Matches_WithSingleWildcard_DoesNotMatchSlash(string pattern, string pageName, bool expected)
    {
        // Arrange
        var rule = new PageAccessRule(pattern, [], [], 0);

        // Act
        var result = rule.Matches(pageName);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Pattern Matching - Simple Wildcard Tests

    [Theory]
    [InlineData("*", "homepage", true)]
    [InlineData("*", "readme", true)]
    [InlineData("*", "path/to/page", false)]
    [InlineData("*", "admin/settings", false)]
    [InlineData("**", "homepage", true)]
    [InlineData("**", "path/to/page", true)]
    [InlineData("**", "admin/settings", true)]
    [InlineData("**", "deeply/nested/path/to/page", true)]
    public void Matches_WithWildcardOnly_MatchesCorrectly(string pattern, string pageName, bool expected)
    {
        // Arrange
        var rule = new PageAccessRule(pattern, [], [], 0);

        // Act
        var result = rule.Matches(pageName);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Pattern Matching - Exact Match Tests

    [Theory]
    [InlineData("Home", "Home", true)]
    [InlineData("Home", "home", true)] // Case insensitive
    [InlineData("Home", "HOME", true)] // Case insensitive
    [InlineData("Home", "Homepage", false)]
    [InlineData("admin/settings", "admin/settings", true)]
    [InlineData("admin/settings", "admin/setting", false)]
    [InlineData("admin/settings", "admin/settings/page", false)]
    public void Matches_WithExactPattern_MatchesExactly(string pattern, string pageName, bool expected)
    {
        // Arrange
        var rule = new PageAccessRule(pattern, [], [], 0);

        // Act
        var result = rule.Matches(pageName);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Pattern Matching - Mixed Wildcard Tests

    [Theory]
    [InlineData("docs/*/readme", "docs/guides/readme", true)]
    [InlineData("docs/*/readme", "docs/tutorials/readme", true)]
    [InlineData("docs/*/readme", "docs/guides/advanced/readme", false)]
    [InlineData("docs/**/readme", "docs/guides/readme", true)]
    [InlineData("docs/**/readme", "docs/guides/advanced/readme", true)]
    [InlineData("admin/*/config/**", "admin/users/config/settings", true)]
    [InlineData("admin/*/config/**", "admin/users/config/advanced/settings", true)]
    [InlineData("admin/*/config/**", "admin/config/settings", false)]
    public void Matches_WithMixedWildcards_MatchesCorrectly(string pattern, string pageName, bool expected)
    {
        // Arrange
        var rule = new PageAccessRule(pattern, [], [], 0);

        // Act
        var result = rule.Matches(pageName);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Pattern Matching - Edge Cases

    [Theory]
    [InlineData("test*page", "testpage", true)]
    [InlineData("test*page", "test123page", true)]
    [InlineData("test*page", "test/page", false)] // * doesn't match /
    [InlineData("test**page", "testpage", true)]
    [InlineData("test**page", "test/page", true)] // ** matches /
    [InlineData("test**page", "test/sub/page", true)]
    [InlineData("*test", "mytest", true)]
    [InlineData("*test", "test", true)]
    [InlineData("test*", "test", true)]
    [InlineData("test*", "testing", true)]
    public void Matches_WithWildcardInMiddle_MatchesCorrectly(string pattern, string pageName, bool expected)
    {
        // Arrange
        var rule = new PageAccessRule(pattern, [], [], 0);

        // Act
        var result = rule.Matches(pageName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("docs-*/page", "docs-v1/page", true)]
    [InlineData("docs_*/page", "docs_draft/page", true)]
    [InlineData("docs.*/page", "docs.2024/page", true)]
    public void Matches_WithSpecialCharactersAndWildcard_MatchesCorrectly(string pattern, string pageName, bool expected)
    {
        // Arrange
        var rule = new PageAccessRule(pattern, [], [], 0);

        // Act
        var result = rule.Matches(pageName);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Pattern Matching - Case Insensitivity Tests

    [Theory]
    [InlineData("Admin/**", "admin/settings", true)]
    [InlineData("admin/**", "Admin/Settings", true)]
    [InlineData("ADMIN/**", "admin/settings", true)]
    [InlineData("Docs/Guides/**", "docs/guides/intro", true)]
    [InlineData("docs/guides/**", "DOCS/GUIDES/INTRO", true)]
    public void Matches_IsCaseInsensitive(string pattern, string pageName, bool expected)
    {
        // Arrange
        var rule = new PageAccessRule(pattern, [], [], 0);

        // Act
        var result = rule.Matches(pageName);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public void Matches_AdminSection_RestrictsCorrectly()
    {
        // Arrange
        var rule = new PageAccessRule("admin/**", ["admin"], ["admin"], 0);

        // Assert - Should match admin pages
        Assert.True(rule.Matches("admin/dashboard"));
        Assert.True(rule.Matches("admin/users"));
        Assert.True(rule.Matches("admin/users/create"));
        Assert.True(rule.Matches("admin/settings/security"));

        // Should NOT match non-admin pages
        Assert.False(rule.Matches("homepage"));
        Assert.False(rule.Matches("docs/readme"));
        Assert.False(rule.Matches("administrator"));
    }

    [Fact]
    public void Matches_PublicDocumentation_AllowsCorrectly()
    {
        // Arrange
        var rule = new PageAccessRule("docs/public/**", [], [], 0);

        // Assert - Should match public docs
        Assert.True(rule.Matches("docs/public/readme"));
        Assert.True(rule.Matches("docs/public/guides/intro"));
        Assert.True(rule.Matches("docs/public/api/reference"));

        // Should NOT match private or other docs
        Assert.False(rule.Matches("docs/private/internal"));
        Assert.False(rule.Matches("docs/readme"));
    }

    [Fact]
    public void Matches_RestrictedFiles_WithDoubleWildcard()
    {
        // Arrange
        var rule = new PageAccessRule("**/secrets", ["admin"], ["admin"], 0);

        // Assert - Should match 'secrets' page in subdirectories
        Assert.True(rule.Matches("admin/secrets"));
        Assert.True(rule.Matches("docs/internal/secrets"));
        Assert.True(rule.Matches("projects/alpha/secrets"));

        // Should NOT match similar but different names
        Assert.False(rule.Matches("secret"));
        Assert.False(rule.Matches("secretspage"));
        Assert.False(rule.Matches("docs/secrets-guide"));
        
        // Note: Pattern **/secrets doesn't match "secrets" at root level
        // Use pattern "secrets" or "**/secrets" for root, or "**" for everything
    }

    [Fact]
    public void Matches_DepartmentSpecificPages()
    {
        // Arrange
        var hrRule = new PageAccessRule("departments/hr/**", ["hr", "admin"], ["hr"], 0);
        var financeRule = new PageAccessRule("departments/finance/**", ["finance", "admin"], ["finance"], 0);

        // Assert - HR pages
        Assert.True(hrRule.Matches("departments/hr/policies"));
        Assert.True(hrRule.Matches("departments/hr/employees/list"));
        Assert.False(hrRule.Matches("departments/finance/budget"));

        // Assert - Finance pages
        Assert.True(financeRule.Matches("departments/finance/budget"));
        Assert.True(financeRule.Matches("departments/finance/reports/q1"));
        Assert.False(financeRule.Matches("departments/hr/policies"));
    }

    [Fact]
    public void Matches_VersionedDocumentation()
    {
        // Arrange
        var v1Rule = new PageAccessRule("docs/v1/**", [], ["legacy-maintainers"], 0);
        var v2Rule = new PageAccessRule("docs/v2/**", [], ["editors"], 0);
        var anyVersionRule = new PageAccessRule("docs/v*/**", ["users"], [], 0);

        // Assert - Specific versions
        Assert.True(v1Rule.Matches("docs/v1/api"));
        Assert.False(v1Rule.Matches("docs/v2/api"));

        Assert.True(v2Rule.Matches("docs/v2/guide"));
        Assert.False(v2Rule.Matches("docs/v1/guide"));

        // Any version pattern
        Assert.True(anyVersionRule.Matches("docs/v1/readme"));
        Assert.True(anyVersionRule.Matches("docs/v2/readme"));
        Assert.True(anyVersionRule.Matches("docs/v3/readme"));
        Assert.False(anyVersionRule.Matches("docs/latest/readme"));
    }

    #endregion

    #region Order and Priority Tests

    [Fact]
    public void Order_DeterminesPriority()
    {
        // Arrange
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("*", [], ["users"], 10),
            new PageAccessRule("admin/**", ["admin"], ["admin"], 1),
            new PageAccessRule("docs/**", ["users"], ["editors"], 5)
        };

        // Act
        var sortedRules = rules.OrderBy(r => r.Order).ToList();

        // Assert
        Assert.Equal("admin/**", sortedRules[0].Pattern);
        Assert.Equal(1, sortedRules[0].Order);

        Assert.Equal("docs/**", sortedRules[1].Pattern);
        Assert.Equal(5, sortedRules[1].Order);

        Assert.Equal("*", sortedRules[2].Pattern);
        Assert.Equal(10, sortedRules[2].Order);
    }

    #endregion

    #region Group Access Tests

    [Fact]
    public void ReadGroups_StoresMultipleGroups()
    {
        // Arrange & Act
        var rule = new PageAccessRule("docs/**", ["readers", "editors", "admins"], ["editors"], 0);

        // Assert
        Assert.Equal(3, rule.ReadGroups.Length);
        Assert.Contains("readers", rule.ReadGroups);
        Assert.Contains("editors", rule.ReadGroups);
        Assert.Contains("admins", rule.ReadGroups);
    }

    [Fact]
    public void WriteGroups_StoresMultipleGroups()
    {
        // Arrange & Act
        var rule = new PageAccessRule("docs/**", ["readers"], ["editors", "admins"], 0);

        // Assert
        Assert.Equal(2, rule.WriteGroups.Length);
        Assert.Contains("editors", rule.WriteGroups);
        Assert.Contains("admins", rule.WriteGroups);
    }

    [Fact]
    public void EmptyGroups_AllowsAllUsers()
    {
        // Arrange & Act
        var rule = new PageAccessRule("public/**", [], [], 0);

        // Assert
        Assert.Empty(rule.ReadGroups);
        Assert.Empty(rule.WriteGroups);
        // Empty groups typically mean "allow all users" in the service logic
    }

    #endregion

    #region Pattern Compilation Tests

    [Fact]
    public void CompiledPattern_IsCreatedDuringConstruction()
    {
        // Arrange & Act
        var rule = new PageAccessRule("admin/**", ["admin"], ["admin"], 0);

        // Assert
        Assert.NotNull(rule.CompiledPattern);
    }

    [Theory]
    [InlineData("test", "test")]
    [InlineData("admin/**", "admin/page")]
    [InlineData("docs/*", "docs/readme")]
    [InlineData("**/internal", "path/to/internal")]
    public void CompiledPattern_PerformsCorrectMatching(string pattern, string matchingPage)
    {
        // Arrange
        var rule = new PageAccessRule(pattern, [], [], 0);

        // Act
        var matches = rule.CompiledPattern.IsMatch(matchingPage);

        // Assert
        Assert.True(matches);
    }

    #endregion
}
