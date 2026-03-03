namespace Pmad.Wiki.Models;

/// <summary>
/// Classifies the broad media category of a file stored in the wiki.
/// </summary>
public enum MediaType
{
    /// <summary>An image file (e.g., PNG, JPEG, GIF, SVG, WebP).</summary>
    Image,
    /// <summary>A video file (e.g., MP4, WebM, OGG).</summary>
    Video,
    /// <summary>A document file (e.g., PDF).</summary>
    Document,
    /// <summary>A file that does not fit any other category.</summary>
    File
}
