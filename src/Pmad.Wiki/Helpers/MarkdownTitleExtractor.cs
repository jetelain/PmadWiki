using System.Text.RegularExpressions;

namespace Pmad.Wiki.Helpers;

public static partial class MarkdownTitleExtractor
{
    [GeneratedRegex(@"^#\s+(.+)$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex FirstH1Regex();

    public static string ExtractFirstTitle(string markdownContent, string fallbackTitle)
    {
        if (string.IsNullOrWhiteSpace(markdownContent))
        {
            return fallbackTitle;
        }

        var match = FirstH1Regex().Match(markdownContent);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        return fallbackTitle;
    }
}
