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

    /// <summary>
    /// Gets or sets the sort order of this page among its siblings in the site map.
    /// Pages are sorted by ascending sort order, then alphabetically. Defaults to <c>0</c>.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the page content should be rendered as a reveal.js slide show.
    /// Use <c>---</c> on its own line to separate slides.
    /// </summary>
    public bool SlideShow { get; set; }

    /// <summary>
    /// Gets or sets the reveal.js theme name to use when <see cref="SlideShow"/> is enabled.
    /// Defaults to <c>black</c> when not set or when an unknown value is provided.
    /// </summary>
    public string? SlideShowTheme { get; set; }
}
