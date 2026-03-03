using Pmad.Wiki.Helpers;
using Pmad.Wiki.Models;

namespace Pmad.Wiki.Services;

/// <summary>
/// Represents a wiki page and its content loaded from the repository.
/// </summary>
public class WikiPage
{
    /// <summary>Gets or sets the parsed content of the page.</summary>
    public required WikiPageContent Content { get; set; }

    /// <summary>Gets or sets the name (path) of the page within the wiki.</summary>
    public required string PageName { get; set; }

    /// <summary>Gets the raw Markdown source including any front matter.</summary>
    public string RawContent => Content.RawContent;

    /// <summary>Gets the parsed front matter of the page.</summary>
    public WikiPageFrontMatter FrontMatter => Content.FrontMatter;

    /// <summary>Gets the Markdown content with the front matter block removed.</summary>
    public string ContentWithoutFrontMatter => Content.ContentWithoutFrontMatter;

    /// <summary>Gets or sets the resolved display title of the page.</summary>
    public required string Title { get; set; }

    /// <summary>Gets or sets the culture code of this page, or <c>null</c> for the neutral culture.</summary>
    public string? Culture { get; set; }

    /// <summary>Gets or sets the display name of the user who last modified the page.</summary>
    public string? LastModifiedBy { get; set; }

    /// <summary>Gets or sets the date and time of the last modification.</summary>
    public DateTimeOffset? LastModified { get; set; }

    /// <summary>Gets or sets a hash of the page content, used for optimistic concurrency checks.</summary>
    public required string ContentHash { get; set; }
}
