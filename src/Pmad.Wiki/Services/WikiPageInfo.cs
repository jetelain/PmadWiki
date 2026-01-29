namespace Pmad.Wiki.Services;

public class WikiPageInfo
{
    public required string PageName { get; set; }
    public string? Title { get; set; }
    public string? Culture { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
