using System.Text.RegularExpressions;

namespace Pmad.Wiki.Helpers;

public static partial class WikiTemplateFrontMatterParser
{
    [GeneratedRegex(@"^---\s*\r?\n(.*?)\r?\n---\s*\r?\n", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex FrontMatterRegex();

    [GeneratedRegex(@"^\s*([a-zA-Z0-9_-]+)\s*:\s*(.+?)\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex FrontMatterLineRegex();

    public static (Dictionary<string, string> FrontMatter, string Content) Parse(string rawContent)
    {
        var frontMatter = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var content = rawContent;

        var match = FrontMatterRegex().Match(rawContent);
        if (match.Success)
        {
            var frontMatterText = match.Groups[1].Value;
            content = rawContent[match.Length..];

            var lineMatches = FrontMatterLineRegex().Matches(frontMatterText);
            foreach (Match lineMatch in lineMatches)
            {
                var key = lineMatch.Groups[1].Value.Trim();
                var value = lineMatch.Groups[2].Value.Trim();
                frontMatter[key] = value;
            }
        }

        return (frontMatter, content);
    }

    public static string GetValue(Dictionary<string, string> frontMatter, string key, string defaultValue = "")
    {
        return frontMatter.TryGetValue(key, out var value) ? value : defaultValue;
    }
}
