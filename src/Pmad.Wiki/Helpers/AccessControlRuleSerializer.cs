using System.Text;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Helpers;

/// <summary>
/// Helper class for serializing and parsing access control rules.
/// </summary>
public static class AccessControlRuleSerializer
{
    /// <summary>
    /// Serializes access control rules to the .wikipermissions file format.
    /// </summary>
    /// <param name="rules">The rules to serialize.</param>
    /// <param name="includeExamples">Whether to include example rules when the list is empty.</param>
    /// <returns>The serialized rules as a string.</returns>
    public static string SerializeRules(List<PageAccessRule> rules, bool includeExamples = false)
    {
        if (rules.Count == 0 && includeExamples)
        {
            return @"# Wiki Page Access Control Rules
# Format: Pattern | ReadGroups | WriteGroups
# Patterns support wildcards: * (any chars except /) and ** (any chars including /)
# Groups are comma-separated. Empty means all users.
# Rules are evaluated in order - first match wins.
#
# Examples:
# admin/** | admin | admin
# private/* | users, editors | editors
# * | | users
";
        }

        var sb = new StringBuilder();
        sb.AppendLine("# Wiki Page Access Control Rules");
        sb.AppendLine("# Format: Pattern | ReadGroups | WriteGroups");
        sb.AppendLine("# Patterns support wildcards: * (any chars except /) and ** (any chars including /)");
        sb.AppendLine("# Groups are comma-separated. Empty means all users.");
        sb.AppendLine("# Rules are evaluated in order - first match wins.");
        sb.AppendLine();

        var sortedRules = rules.OrderBy(r => r.Order).ToList();
        
        foreach (var rule in sortedRules)
        {
            var readGroups = string.Join(", ", rule.ReadGroups);
            var writeGroups = string.Join(", ", rule.WriteGroups);
            sb.AppendLine($"{rule.Pattern} | {readGroups} | {writeGroups}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parses access control rules from the .wikipermissions file format.
    /// </summary>
    /// <param name="content">The content to parse.</param>
    /// <returns>The parsed rules.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a rule has invalid format.</exception>
    public static List<PageAccessRule> ParseRules(string content)
    {
        var rules = new List<PageAccessRule>();
        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        int order = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            var parts = trimmed.Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length < 3)
            {
                throw new InvalidOperationException($"Invalid rule format: {line}");
            }

            var pattern = parts[0];
            var readGroups = parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var writeGroups = parts[2].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            rules.Add(new PageAccessRule(pattern, readGroups, writeGroups, order));
            order++;
        }

        return rules;
    }
}
