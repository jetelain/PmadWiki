using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Pmad.Wiki.Helpers;

public static partial class WikiInputValidator
{
    [GeneratedRegex("^[a-zA-Z0-9_/-]+$")]
    private static partial Regex PageNameRegex();

    [GeneratedRegex("^[a-z]{2}(-[A-Z]{2})?$")]
    private static partial Regex CultureRegex();

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

    public static void ValidateCulture(string culture)
    {
        if (!IsValidCulture(culture, out var errorMessage))
        {
            throw new ArgumentException(errorMessage, nameof(culture));
        }
    }
}
