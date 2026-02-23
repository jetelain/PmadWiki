using Pmad.Wiki.Models;

namespace Pmad.Wiki.Helpers;

public static class WikiPageFrontMatterParser
{
    /// <summary>
    /// Parses the front matter from raw page content.
    /// </summary>
    /// <param name="rawContent">The raw content containing potential front matter.</param>
    /// <returns>A tuple containing the parsed front matter and the remaining content without the front matter block.</returns>
    public static (WikiPageFrontMatter FrontMatter, string Content) Parse(string rawContent)
    {
        var (frontMatter, content) = WikiFrontMatterParser.Parse<WikiPageFrontMatter>(rawContent);
        if (string.IsNullOrEmpty(frontMatter.Title)) frontMatter.Title = null;
        return (frontMatter, content);
    }
}
