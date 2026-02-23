using System.Text.RegularExpressions;
using Pmad.Wiki.Models;

namespace Pmad.Wiki.Helpers;

public static class WikiTemplateFrontMatterParser
{
    /// <summary>
    /// Parses the front matter from raw content and returns a strongly-typed object.
    /// </summary>
    /// <param name="rawContent">The raw content containing potential front matter.</param>
    /// <returns>A tuple containing the parsed front matter and the remaining content.</returns>
    public static (WikiTemplateFrontMatter FrontMatter, string Content) Parse(string rawContent)
    {
        var (frontMatter, content) = WikiFrontMatterParser.Parse<WikiTemplateFrontMatter>(rawContent);
        // Convert empty strings to null for consistency
        if (string.IsNullOrEmpty(frontMatter.Title)) frontMatter.Title = null;
        if (string.IsNullOrEmpty(frontMatter.Description)) frontMatter.Description = null;
        if (string.IsNullOrEmpty(frontMatter.Location)) frontMatter.Location = null;
        if (string.IsNullOrEmpty(frontMatter.Pattern)) frontMatter.Pattern = null;
        return (frontMatter, content);
    }
}
