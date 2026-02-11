namespace Pmad.Wiki.Services;

/// <summary>
/// Information about a temporary media file.
/// </summary>
public class TemporaryMediaInfo
{
    public required string TemporaryId { get; set; }
    public required string OriginalFileName { get; set; }
    public required string FilePath { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
