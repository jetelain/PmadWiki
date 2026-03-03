using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Pmad.Wiki.Helpers;

/// <summary>
/// Helper class for sanitizing template parameter values to valid page name components.
/// </summary>
public static partial class WikiInputSanitizer
{
    [GeneratedRegex(@"[^a-zA-Z0-9_-]+", RegexOptions.CultureInvariant)]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"[\s\.]+", RegexOptions.CultureInvariant)]
    private static partial Regex SpaceAndDotsCharsRegex();

    /// <summary>
    /// Sanitizes a filename by removing or replacing invalid characters.
    /// Only keeps alphanumeric characters, underscores, and hyphens.
    /// Whitespace and dots are converted to hyphens.
    /// </summary>
    /// <param name="fileName">The filename to sanitize.</param>
    /// <returns>A sanitized filename safe for use in file paths.</returns>
    public static string SanitizeMediaFileName(string fileName)
    {
        var sanitized = Sanitize(fileName);

        // Ensure the result is not empty and not too long
        if (string.IsNullOrEmpty(sanitized))
        {
            return "file";
        }

        // Limit length to avoid excessively long filenames
        if (sanitized.Length > 100)
        {
            sanitized = sanitized.Substring(0, 100);
        }

        return sanitized;
    }

    /// <summary>
    /// Sanitizes a parameter value to be safe for use in page names and locations.
    /// Converts to ASCII, removes invalid characters, and ensures valid format.
    /// </summary>
    public static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // Normalize to remove accents and convert to ASCII
        var normalized = RemoveDiacritics(value);

        // Replace spaces with hyphens
        normalized = SpaceAndDotsCharsRegex().Replace(normalized, "-");

        // Remove any characters that are not allowed in page names
        normalized = InvalidCharsRegex().Replace(normalized, string.Empty);

        // Remove leading/trailing hyphens and underscores
        normalized = normalized.Trim('-', '_');

        // Collapse multiple hyphens into one
        while (normalized.Contains("--", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
        }

        // Collapse multiple underscores into one
        while (normalized.Contains("__", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("__", "_", StringComparison.Ordinal);
        }

        return normalized;
    }

    /// <summary>
    /// Sanitizes a value intended for use as a directory/location component.
    /// Preserves forward slashes for directory separators.
    /// </summary>
    public static string SanitizeLocation(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // Split by directory separators
        var parts = value.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Sanitize each part
        var sanitizedParts = parts.Select(Sanitize).Where(p => !string.IsNullOrEmpty(p));

        return string.Join('/', sanitizedParts);
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }
}
