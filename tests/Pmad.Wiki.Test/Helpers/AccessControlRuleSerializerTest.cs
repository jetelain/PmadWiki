using Pmad.Wiki.Helpers;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Helpers;

public class AccessControlRuleSerializerTest
{
    [Fact]
    public void SerializeRules_WithEmptyList_IncludeExamples_ReturnsExampleContent()
    {
        // Arrange
        var rules = new List<PageAccessRule>();

        // Act
        var result = AccessControlRuleSerializer.SerializeRules(rules, includeExamples: true);

        // Assert
        Assert.Contains("# Wiki Page Access Control Rules", result);
        Assert.Contains("# Format: Pattern | ReadGroups | WriteGroups", result);
        Assert.Contains("# Examples:", result);
        Assert.Contains("# admin/** | admin | admin", result);
        Assert.Contains("# private/* | users, editors | editors", result);
        Assert.Contains("# * | | users", result);
    }

    [Fact]
    public void SerializeRules_WithEmptyList_NoExamples_ReturnsHeaderOnly()
    {
        // Arrange
        var rules = new List<PageAccessRule>();

        // Act
        var result = AccessControlRuleSerializer.SerializeRules(rules, includeExamples: false);

        // Assert
        Assert.Contains("# Wiki Page Access Control Rules", result);
        Assert.Contains("# Format: Pattern | ReadGroups | WriteGroups", result);
        Assert.DoesNotContain("# Examples:", result);
        Assert.DoesNotContain("# admin/** | admin | admin", result);
    }

    [Fact]
    public void SerializeRules_WithSingleRule_ReturnsFormattedRule()
    {
        // Arrange
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("admin/**", ["admin"], ["admin"], 0)
        };

        // Act
        var result = AccessControlRuleSerializer.SerializeRules(rules);

        // Assert
        Assert.Contains("admin/** | admin | admin", result);
    }

    [Fact]
    public void SerializeRules_WithMultipleRules_ReturnsOrderedRules()
    {
        // Arrange
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("*", [], ["users"], 2),
            new PageAccessRule("admin/**", ["admin"], ["admin"], 0),
            new PageAccessRule("private/*", ["users", "editors"], ["editors"], 1)
        };

        // Act
        var result = AccessControlRuleSerializer.SerializeRules(rules);

        // Assert
        var lines = result.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var ruleLines = lines.Where(l => !l.TrimStart().StartsWith('#') && !string.IsNullOrWhiteSpace(l)).ToList();
        
        Assert.Equal(3, ruleLines.Count);
        Assert.Contains("admin/** | admin | admin", ruleLines[0]);
        Assert.Contains("private/* | users, editors | editors", ruleLines[1]);
        Assert.Contains("* |  | users", ruleLines[2]);
    }

    [Fact]
    public void SerializeRules_WithEmptyGroups_ReturnsEmptyGroupsInFormat()
    {
        // Arrange
        var rules = new List<PageAccessRule>
        {
            new PageAccessRule("public/*", [], [], 0)
        };

        // Act
        var result = AccessControlRuleSerializer.SerializeRules(rules);

        // Assert
        Assert.Contains("public/* |  | ", result);
    }

    [Fact]
    public void ParseRules_WithValidContent_ReturnsRules()
    {
        // Arrange
        var content = @"# Wiki Page Access Control Rules
# Format: Pattern | ReadGroups | WriteGroups
admin/** | admin | admin
private/* | users, editors | editors
* |  | users";

        // Act
        var rules = AccessControlRuleSerializer.ParseRules(content);

        // Assert
        Assert.Equal(3, rules.Count);
        
        Assert.Equal("admin/**", rules[0].Pattern);
        Assert.Equal(["admin"], rules[0].ReadGroups);
        Assert.Equal(["admin"], rules[0].WriteGroups);
        Assert.Equal(0, rules[0].Order);

        Assert.Equal("private/*", rules[1].Pattern);
        Assert.Equal(["users", "editors"], rules[1].ReadGroups);
        Assert.Equal(["editors"], rules[1].WriteGroups);
        Assert.Equal(1, rules[1].Order);

        Assert.Equal("*", rules[2].Pattern);
        Assert.Empty(rules[2].ReadGroups);
        Assert.Equal(["users"], rules[2].WriteGroups);
        Assert.Equal(2, rules[2].Order);
    }

    [Fact]
    public void ParseRules_WithCommentsAndEmptyLines_IgnoresThemCorrectly()
    {
        // Arrange
        var content = @"# Comment line
# Another comment

admin/** | admin | admin

# More comments
private/* | users | editors";

        // Act
        var rules = AccessControlRuleSerializer.ParseRules(content);

        // Assert
        Assert.Equal(2, rules.Count);
        Assert.Equal("admin/**", rules[0].Pattern);
        Assert.Equal("private/*", rules[1].Pattern);
    }

    [Fact]
    public void ParseRules_WithEmptyContent_ReturnsEmptyList()
    {
        // Arrange
        var content = "";

        // Act
        var rules = AccessControlRuleSerializer.ParseRules(content);

        // Assert
        Assert.Empty(rules);
    }

    [Fact]
    public void ParseRules_WithOnlyComments_ReturnsEmptyList()
    {
        // Arrange
        var content = @"# Comment 1
# Comment 2
# Comment 3";

        // Act
        var rules = AccessControlRuleSerializer.ParseRules(content);

        // Assert
        Assert.Empty(rules);
    }

    [Fact]
    public void ParseRules_WithInvalidFormat_ThrowsInvalidOperationException()
    {
        // Arrange
        var content = "invalid line without pipes";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            AccessControlRuleSerializer.ParseRules(content));
        Assert.Contains("Invalid rule format", exception.Message);
    }

    [Fact]
    public void ParseRules_WithMissingPart_ThrowsInvalidOperationException()
    {
        // Arrange
        var content = "pattern | readgroups";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            AccessControlRuleSerializer.ParseRules(content));
        Assert.Contains("Invalid rule format", exception.Message);
    }

    [Fact]
    public void ParseRules_WithWhitespaceInGroups_TrimsCorrectly()
    {
        // Arrange
        var content = "pattern | group1 , group2 , group3 | group4 , group5";

        // Act
        var rules = AccessControlRuleSerializer.ParseRules(content);

        // Assert
        Assert.Single(rules);
        Assert.Equal(["group1", "group2", "group3"], rules[0].ReadGroups);
        Assert.Equal(["group4", "group5"], rules[0].WriteGroups);
    }

    [Fact]
    public void ParseRules_WithExtraSpaces_HandlesCorrectly()
    {
        // Arrange
        var content = "  pattern  |  group1  |  group2  ";

        // Act
        var rules = AccessControlRuleSerializer.ParseRules(content);

        // Assert
        Assert.Single(rules);
        Assert.Equal("pattern", rules[0].Pattern);
        Assert.Equal(["group1"], rules[0].ReadGroups);
        Assert.Equal(["group2"], rules[0].WriteGroups);
    }

    [Fact]
    public void SerializeAndParse_RoundTrip_PreservesData()
    {
        // Arrange
        var originalRules = new List<PageAccessRule>
        {
            new PageAccessRule("admin/**", ["admin", "superadmin"], ["admin"], 0),
            new PageAccessRule("docs/*", ["users", "editors", "admin"], ["editors", "admin"], 1),
            new PageAccessRule("*", [], ["users"], 2)
        };

        // Act
        var serialized = AccessControlRuleSerializer.SerializeRules(originalRules);
        var parsedRules = AccessControlRuleSerializer.ParseRules(serialized);

        // Assert
        Assert.Equal(originalRules.Count, parsedRules.Count);
        
        for (int i = 0; i < originalRules.Count; i++)
        {
            Assert.Equal(originalRules[i].Pattern, parsedRules[i].Pattern);
            Assert.Equal(originalRules[i].ReadGroups, parsedRules[i].ReadGroups);
            Assert.Equal(originalRules[i].WriteGroups, parsedRules[i].WriteGroups);
            Assert.Equal(originalRules[i].Order, parsedRules[i].Order);
        }
    }
}
