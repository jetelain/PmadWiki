using Pmad.Wiki.Models;

namespace Pmad.Wiki.Helpers;

public static class WikiPageContentParser
{
    /// <summary>
    /// Parses the front matter and markdown content from raw page content.
    /// </summary>
    /// <param name="rawContent">The raw content containing potential front matter.</param>
    /// <returns>A <see cref="WikiPageContent"/> object containing the parsed front matter, the remaining content without the front matter block and the raw content.</returns>
    public static WikiPageContent Parse(string rawContent)
    {
        var (frontMatter, content) = WikiFrontMatterParser.Parse<WikiPageFrontMatter>(rawContent);
        if (string.IsNullOrEmpty(frontMatter.Title)) frontMatter.Title = null;
        return new WikiPageContent(frontMatter, content, rawContent);
    }
}
