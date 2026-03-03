namespace Pmad.Wiki.Models;

/// <summary>
/// Represents the parsed content of a wiki page, separating front matter from the Markdown body.
/// </summary>
/// <param name="FrontMatter">The parsed front matter metadata.</param>
/// <param name="ContentWithoutFrontMatter">The Markdown body with the front matter block removed.</param>
/// <param name="RawContent">The full raw Markdown source including front matter.</param>
public record WikiPageContent(WikiPageFrontMatter FrontMatter, string ContentWithoutFrontMatter, string RawContent);
