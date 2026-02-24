using System.Text.RegularExpressions;
using Pmad.Wiki.Models;

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

        return ExtractFirstTitle(WikiPageContentParser.Parse(markdownContent), pageName);
    }

    public static string ExtractFirstTitle(WikiPageContent content, string pageName)
    {
        if (!string.IsNullOrEmpty(content.FrontMatter.Title))
        {
            return content.FrontMatter.Title;
        }

        var match = FirstH1Regex().Match(content.ContentWithoutFrontMatter);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        return GetLastPart(pageName);
    }
}
