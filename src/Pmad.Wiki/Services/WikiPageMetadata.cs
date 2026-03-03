using Pmad.Wiki.Models;

namespace Pmad.Wiki.Services;

/// <summary>
/// Represents the cached metadata extracted from a wiki page (title and front matter).
/// </summary>
/// <param name="Title">The resolved display title of the page.</param>
/// <param name="FrontMatter">The parsed front matter of the page.</param>
public record WikiPageMetadata(string Title, WikiPageFrontMatter FrontMatter);
