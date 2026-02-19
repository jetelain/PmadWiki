namespace Pmad.Wiki.Models;

public class MediaGalleryItem
{
    public required string Path { get; set; }
    public required string FileName { get; set; }
    public required string Url { get; set; }
    public required MediaType MediaType { get; set; }
    public DateTimeOffset? LastModified { get; set; }
}
