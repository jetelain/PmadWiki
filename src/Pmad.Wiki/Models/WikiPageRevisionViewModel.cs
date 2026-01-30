namespace Pmad.Wiki.Models;

public class WikiPageRevisionViewModel
{
    public List<WikiPageLink> Breadcrumb { get; } = new();
    public required string PageName { get; set; }
    public required string HtmlContent { get; set; }
    public required string Title { get; set; }
    public string? Culture { get; set; }
    public required string CommitId { get; set; }
    public required string AuthorName { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
    public required string Message { get; set; }
    public bool CanEdit { get; set; }
}
