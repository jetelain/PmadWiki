namespace Pmad.Wiki.Services;

public class WikiPage
{
    public required string PageName { get; set; }
    public required string Content { get; set; }
    public required string Title { get; set; }
    public string? Culture { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public required string ContentHash { get; set; }
}
