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
    [GeneratedRegex("^([a-zA-Z0-9_-]+/)*[a-zA-Z0-9_-]+(\\.excalidraw\\.svg|\\.[a-zA-Z0-9]+)+$", RegexOptions.CultureInvariant)]
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

    /// <summary>
    /// Template parameter names: alphanumeric, dash and underscores only.
    /// </summary>
    /// <remarks>
    /// Needs to be safe to use as HTML input names, query string parameters, and in template placeholder syntax.
    /// Hyphens are valid and widely supported in HTML attribute values (including the <c>name</c> attribute); the restricted character set here is chosen to avoid special characters that may cause issues in URLs, model binding, or template processing.
    /// </remarks>
    [GeneratedRegex("^[a-zA-Z0-9_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex TemplateParameterNameRegex();

    /// <summary>
    /// Slide show theme names: alphanumeric, hyphens and underscores only, safe for use as a CSS file name and URL path segment.
    /// </summary>
    [GeneratedRegex("^[a-zA-Z0-9_-]+$", RegexOptions.CultureInvariant)]
    internal static partial Regex ThemeNameRegex();

    /// <summary>
    /// Temporary media identifiers, which are a lowercase GUID without dashes, like "a3f1e2b4c5d67890e1f2a3b4c5d67890"
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex("^([a-f0-9]+)$", RegexOptions.CultureInvariant)]
    internal static partial Regex TempMediaIdRegex();

    public static bool IsValidPageName(string pageName)
    {
        if (string.IsNullOrWhiteSpace(pageName))
        {
            return false;
        }

        if (!PageNameRegex().IsMatch(pageName))
        {
            return false;
        }

        // Security checks to prevent directory traversal and malformed paths
        if (pageName.Contains("..", StringComparison.Ordinal) 
            || pageName.Contains("//", StringComparison.Ordinal)
            || pageName.StartsWith("/", StringComparison.Ordinal)
            || pageName.EndsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    public static bool IsValidMediaPath(string mediaPath)
    {
        if (string.IsNullOrWhiteSpace(mediaPath))
        {
            return false;
        }

        if (!MediaPathRegex().IsMatch(mediaPath))
        {
            return false;
        }

        // Security checks to prevent directory traversal and malformed paths
        if (mediaPath.Contains("..", StringComparison.Ordinal)
            || mediaPath.Contains("//", StringComparison.Ordinal)
            || mediaPath.StartsWith("/", StringComparison.Ordinal)
            || mediaPath.EndsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    public static bool IsValidCulture(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return false;
        }

        if (!CultureRegex().IsMatch(culture))
        {
            return false;
        }

        return true;
    }

    public static bool IsValidTempMediaId(string tempMediaId)
    {
        if (string.IsNullOrWhiteSpace(tempMediaId))
        {
            return false;
        }
        if (!TempMediaIdRegex().IsMatch(tempMediaId))
        {
            return false;
        }
        return true;
    }

    public static bool IsValidTemplateParameterName(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }
        return TemplateParameterNameRegex().IsMatch(parameterName);
    }

    public static void ValidatePageName(string pageName)
    {
        if (!IsValidPageName(pageName))
        {
            throw new ArgumentException("Invalid page name.", nameof(pageName));
        }
    }

    public static void ValidateMediaPath(string mediaPath)
    {
        if (!IsValidMediaPath(mediaPath))
        {
            throw new ArgumentException("Invalid media path.", nameof(mediaPath));
        }
    }

    public static void ValidateCulture(string culture)
    {
        if (!IsValidCulture(culture))
        {
            throw new ArgumentException("Invalid culture identifier.", nameof(culture));
        }
    }

    public static void ValidateTempMediaId(string tempMediaId)
    {
        if (!IsValidTempMediaId(tempMediaId))
        {
            throw new ArgumentException("Invalid temporary media ID.", nameof(tempMediaId));
        }
    }

    internal static bool IsValidThemeName(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName))
        {
            return false;
        }
        return ThemeNameRegex().IsMatch(themeName);
    }

    public static bool IsValidEditableMedia(string mediaPath)
    {
        return IsValidMediaPath(mediaPath) && WikiPathHelper.IsEditableMedia(mediaPath);
    }

    public static void ValidateEditableMedia(string mediaPath)
    {
        if (!IsValidEditableMedia(mediaPath))
        {
            throw new ArgumentException("Invalid media path.", nameof(mediaPath));
        }
    }
}
