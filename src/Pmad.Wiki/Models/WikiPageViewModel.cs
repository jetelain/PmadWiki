namespace Pmad.Wiki.Models;

public class WikiPageViewModel
{
    public List<WikiPageLink> Breadcrumb { get; } = new();
    public required string PageName { get; set; }
    public required string HtmlContent { get; set; }
    public required string Title { get; set; }
    public bool CanEdit { get; set; }
    public string? Culture { get; set; }
    public List<string> AvailableCultures { get; set; } = new();
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? LastModified { get; set; }
}
