namespace Pmad.Wiki.Models;

public class MediaFileInfo
{
    public required string AbsolutePath { get; set; }
    public required string FileName { get; set; }
    public required MediaType MediaType { get; set; }
}
