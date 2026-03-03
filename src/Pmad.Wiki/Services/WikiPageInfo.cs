namespace Pmad.Wiki.Services;

/// <summary>
/// Provides summary information about a wiki page without loading its full content.
/// </summary>
public class WikiPageInfo
{
    /// <summary>Gets or sets the name (path) of the page within the wiki.</summary>
    public required string PageName { get; set; }

    /// <summary>Gets or sets the display title of the page, or <c>null</c> if not yet resolved.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the culture code of this page, or <c>null</c> for the neutral culture.</summary>
    public string? Culture { get; set; }

    /// <summary>Gets or sets the date and time of the last modification.</summary>
    public DateTimeOffset? LastModified { get; set; }

    /// <summary>Gets or sets the display name of the user who last modified the page.</summary>
    public string? LastModifiedBy { get; set; }
}
