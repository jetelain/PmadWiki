using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Pmad.Wiki.Helpers;

public static partial class WikiInputValidator
{
    /// <summary>
    /// Links like they appear in the wiki URL structure
    /// </summary>
    /// <remarks>
    /// Not suitable for validating page names in markdown files, as it doesn't allow relative paths, anchors and does not use md extension. Use <see cref="PagePathMarkdownRegex"/> for that.
    /// </remarks>
    /// <returns></returns>
    [GeneratedRegex("^[a-zA-Z0-9_/-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex PageNameRegex();

    /// <summary>
    /// Links like they appear in the markdown files
    /// </summary>
    /// <remarks>
    /// Not suitable for security validation of page names, as it allows relative paths. Use <see cref="PageNameRegex"/> for that.
    /// </remarks>
    /// <returns></returns>
    [GeneratedRegex("^([a-zA-Z0-9_/\\.-]+)\\.md(#.*)?$", RegexOptions.CultureInvariant)]
    internal static partial Regex PagePathMarkdownRegex();

    /// <summary>
    /// Culture identifiers like "en" or "en-US"
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex("^[a-z]{2}(-[A-Z]{2})?$", RegexOptions.CultureInvariant)]
    private static partial Regex CultureRegex();

    /// <summary>
    /// Media paths like they appear in the wiki URL structure
    /// </summary>
    /// <remarks>
    /// Not suitable for validating media paths in markdown files, as it doesn't allow relative paths. Use <see cref="MediaPathMarkdownRegex"/> for that.
    /// </remarks>
    /// <returns></returns>
    [GeneratedRegex("^([a-zA-Z0-9_-]+/)*[a-zA-Z0-9_-]+(\\.[a-zA-Z0-9]+)+$", RegexOptions.CultureInvariant)]
    private static partial Regex MediaPathRegex();

    /// <summary>
    /// Media paths like they appear in the markdown files.
    /// </summary>
    /// <remarks>
    /// Not suitable for security validation of media paths, as it allows relative paths and doesn't enforce file extensions. Use <see cref="MediaPathRegex"/> for that.
    /// </remarks>
    /// <returns></returns>
    [GeneratedRegex("^([a-zA-Z0-9_/\\.-]+)$", RegexOptions.CultureInvariant)]
    internal static partial Regex MediaPathMarkdownRegex();

    public static bool IsValidPageName(string pageName, [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(pageName))
        {
            errorMessage = "Page name cannot be null or empty.";
            return false;
        }

        if (!PageNameRegex().IsMatch(pageName))
        {
            errorMessage = "Invalid name.";
            return false;
        }

        // Security checks to prevent directory traversal and malformed paths
        if (pageName.Contains("..", StringComparison.Ordinal) 
            || pageName.Contains("//", StringComparison.Ordinal)
            || pageName.StartsWith("/", StringComparison.Ordinal)
            || pageName.EndsWith("/", StringComparison.Ordinal))
        {
            errorMessage = "Invalid name.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    public static bool IsValidMediaPath(string mediaPath, [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(mediaPath))
        {
            errorMessage = "Media path cannot be null or empty.";
            return false;
        }

        if (!MediaPathRegex().IsMatch(mediaPath))
        {
            errorMessage = "Invalid path.";
            return false;
        }

        // Security checks to prevent directory traversal and malformed paths
        if (mediaPath.Contains("..", StringComparison.Ordinal)
            || mediaPath.Contains("//", StringComparison.Ordinal)
            || mediaPath.StartsWith("/", StringComparison.Ordinal)
            || mediaPath.EndsWith("/", StringComparison.Ordinal))
        {
            errorMessage = "Invalid path.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    public static bool IsValidCulture(string culture, [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            errorMessage = "Culture cannot be null or empty.";
            return false;
        }

        if (!CultureRegex().IsMatch(culture))
        {
            errorMessage = "Invalid culture identifier.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    public static void ValidatePageName(string pageName)
    {
        if (!IsValidPageName(pageName, out var errorMessage))
        {
            throw new ArgumentException(errorMessage, nameof(pageName));
        }
    }

    public static void ValidateMediaPath(string mediaPath)
    {
        if (!IsValidMediaPath(mediaPath, out var errorMessage))
        {
            throw new ArgumentException(errorMessage, nameof(mediaPath));
        }
    }

    public static void ValidateCulture(string culture)
    {
        if (!IsValidCulture(culture, out var errorMessage))
        {
            throw new ArgumentException(errorMessage, nameof(culture));
        }
    }
}
