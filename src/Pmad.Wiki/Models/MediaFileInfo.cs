namespace Pmad.Wiki.Models;

/// <summary>
/// Provides information about a media file stored in the wiki repository.
/// </summary>
public class MediaFileInfo
{
    /// <summary>Gets or sets the absolute URL path used to serve the file.</summary>
    public required string AbsolutePath { get; set; }

    /// <summary>Gets or sets the file name including its extension.</summary>
    public required string FileName { get; set; }

    /// <summary>Gets or sets the broad media category of the file.</summary>
    public required MediaType MediaType { get; set; }
}
