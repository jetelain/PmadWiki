namespace Pmad.Wiki.Models;

public class UploadMediaResponse
{
    public required string TemporaryId { get; set; }
    public required string FileName { get; set; }
    public required string Url { get; set; }
    public long Size { get; set; }
}
