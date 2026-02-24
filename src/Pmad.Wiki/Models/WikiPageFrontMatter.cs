namespace Pmad.Wiki.Models;

/// <summary>
/// Represents the front matter metadata for a wiki content page.
/// </summary>
public record class WikiPageFrontMatter
{
    /// <summary>
    /// Gets or sets the display title of the page, overriding the first H1 heading.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to display a list of direct sub-pages below the page content.
    /// When set to <c>true</c>, only immediate children are listed.
    /// Use <see cref="SubPagesRecursive"/> to list all descendants.
    /// </summary>
    public bool ShowSubPages { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the sub-page list should include all descendants recursively.
    /// Has no effect when <see cref="ShowSubPages"/> is <c>false</c>.
    /// </summary>
    public bool SubPagesRecursive { get; set; }
}
