namespace Pmad.Wiki.Services;

/// <summary>
/// Represents a single entry in a wiki page's revision history.
/// </summary>
public class WikiHistoryItem
{
    /// <summary>Gets or sets the Git commit SHA that identifies this revision.</summary>
    public required string CommitId { get; set; }

    /// <summary>Gets or sets the commit message describing the change.</summary>
    public required string Message { get; set; }

    /// <summary>Gets or sets the display name of the author who made the change.</summary>
    public required string AuthorName { get; set; }

    /// <summary>Gets or sets the date and time when the commit was created.</summary>
    public required DateTimeOffset Timestamp { get; set; }
}
