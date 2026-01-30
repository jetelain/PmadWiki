namespace Pmad.Wiki.Models;

public class WikiPageDiffViewModel
{
    public List<WikiPageLink> Breadcrumb { get; } = new();
    public required string PageName { get; set; }
    public string? Culture { get; set; }
    public required string FromCommitId { get; set; }
    public required string ToCommitId { get; set; }
    public required string FromAuthorName { get; set; }
    public required string ToAuthorName { get; set; }
    public required DateTimeOffset FromTimestamp { get; set; }
    public required DateTimeOffset ToTimestamp { get; set; }
    public required string FromMessage { get; set; }
    public required string ToMessage { get; set; }
    public required string FromContent { get; set; }
    public required string ToContent { get; set; }
    public bool CanEdit { get; set; }
}
