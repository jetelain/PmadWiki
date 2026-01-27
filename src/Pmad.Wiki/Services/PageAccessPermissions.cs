namespace Pmad.Wiki.Services;

/// <summary>
/// Represents the page-level access permissions on a specific page.
/// </summary>
public class PageAccessPermissions
{
    /// <summary>
    /// Gets or sets a value indicating whether the user can read the page.
    /// </summary>
    /// <remarks>
    /// To be allowed to view a page either <see cref="WikiOptions.AllowAnonymousViewing"/> or <see cref="IWikiUserWithPermissions.CanView"/> must also be true.
    /// </remarks>
    public bool CanRead { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user can edit the page.
    /// </summary>
    /// <remarks>
    /// To be allowed to edit a page <see cref="IWikiUserWithPermissions.CanEdit"/> must also be true.
    /// </remarks>
    public bool CanEdit { get; set; }

    /// <summary>
    /// Gets or sets the matched rule pattern (for debugging/auditing).
    /// </summary>
    public string? MatchedPattern { get; set; }
}
