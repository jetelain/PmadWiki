namespace Pmad.Wiki.Services;

/// <summary>
/// Information about a temporary media file.
/// </summary>
public class TemporaryMediaInfo
{
    /// <summary>Gets or sets the unique identifier assigned to this temporary file.</summary>
    public required string TemporaryId { get; set; }

    /// <summary>Gets or sets the original file name as uploaded by the user.</summary>
    public required string OriginalFileName { get; set; }

    /// <summary>Gets or sets the absolute path to the temporary file on disk.</summary>
    public required string FilePath { get; set; }

    /// <summary>Gets or sets the date and time when the temporary file was created.</summary>
    public required DateTimeOffset CreatedAt { get; set; }
}
