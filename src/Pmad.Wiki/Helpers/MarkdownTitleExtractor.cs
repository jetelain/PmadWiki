using System.Text.RegularExpressions;

namespace Pmad.Wiki.Helpers;

public static partial class MarkdownTitleExtractor
{
    [GeneratedRegex(@"^#\s+(.+)$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex FirstH1Regex();

    public static string GetLastPart(string pageName)
    {
        var idx = pageName.LastIndexOf('/');
        if (idx != -1)
        {
            return pageName[(idx + 1)..];
        }
        return pageName;
    }

    public static string ExtractFirstTitle(string markdownContent, string pageName)
    {
        if (string.IsNullOrWhiteSpace(markdownContent))
        {
            return GetLastPart(pageName);
        }

        var match = FirstH1Regex().Match(markdownContent);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        return GetLastPart(pageName);
    }
}
