using System.Text.RegularExpressions;

namespace Pmad.Wiki.Services;

/// <summary>
/// Represents an access control rule for wiki pages.
/// </summary>
public class PageAccessRule
{
    /// <summary>
    /// Gets the file pattern (supports wildcards * and **).
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Gets the groups that have read access.
    /// </summary>
    public string[] ReadGroups { get; }

    /// <summary>
    /// Gets the groups that have write access.
    /// </summary>
    public string[] WriteGroups { get; }

    /// <summary>
    /// Gets the order/priority of this rule (lower numbers take precedence).
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Gets the pre-compiled regex pattern for matching page names.
    /// </summary>
    internal Regex CompiledPattern { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PageAccessRule"/> class.
    /// </summary>
    /// <param name="pattern">The file pattern (supports wildcards * and **).</param>
    /// <param name="readGroups">The groups that have read access.</param>
    /// <param name="writeGroups">The groups that have write access.</param>
    /// <param name="order">The order/priority of this rule (lower numbers take precedence).</param>
    public PageAccessRule(string pattern, string[] readGroups, string[] writeGroups, int order)
    {
        Pattern = pattern;
        ReadGroups = readGroups;
        WriteGroups = writeGroups;
        Order = order;
        CompiledPattern = CompilePattern(pattern);
    }

    private static Regex CompilePattern(string pattern)
    {
        // Convert glob pattern to regex
        // ** matches any character including /
        // * matches any character except /

        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")      // ** -> .*
            .Replace("\\*", "[^/]*")      // * -> [^/]*
            + "$";

        return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }

    /// <summary>
    /// Checks if a page name matches this rule's pattern.
    /// </summary>
    /// <param name="pageName">The page name to check.</param>
    /// <returns>True if the page name matches the pattern; otherwise, false.</returns>
    public bool Matches(string pageName)
    {
        return CompiledPattern.IsMatch(pageName);
    }
}

