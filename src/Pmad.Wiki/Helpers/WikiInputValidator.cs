using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Pmad.Wiki.Helpers;

public static partial class WikiInputValidator
{
    // Links like they appear in the wiki URL structure
    [GeneratedRegex("^[a-zA-Z0-9_/-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex PageNameRegex();

    // Links like they appear in the markdown files
    [GeneratedRegex("^([a-zA-Z0-9_/\\.-]+)\\.md(#.*)?$", RegexOptions.CultureInvariant)]
    internal static partial Regex PageNativePathRegex();

    // Culture identifiers like "en" or "en-US"
    [GeneratedRegex("^[a-z]{2}(-[A-Z]{2})?$", RegexOptions.CultureInvariant)]
    private static partial Regex CultureRegex();

    // Media paths like they appear in the markdown files and in the wiki URL structure
    [GeneratedRegex("^[a-zA-Z0-9_/\\.-]+$", RegexOptions.CultureInvariant)]
    internal static partial Regex MediaPathRegex();

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

    public static void ValidateMediaPath(string pageName)
    {
        if (!IsValidMediaPath(pageName, out var errorMessage))
        {
            throw new ArgumentException(errorMessage, nameof(pageName));
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
