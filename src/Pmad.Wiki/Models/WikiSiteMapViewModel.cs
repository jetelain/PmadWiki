namespace Pmad.Wiki.Models;

public class WikiSiteMapViewModel
{
    public List<WikiSiteMapNode> RootNodes { get; set; } = new();
    public bool CanEdit { get; set; }
    public bool CanAdmin { get; set; }

    public required string HomePageName { get; set; }
}

public class WikiSiteMapNode
{
    public required string PageName { get; set; }
    public required string DisplayName { get; set; }
    public string? Title { get; set; }
    public string? Culture { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
    public int Level { get; set; }
    public List<WikiSiteMapNode> Children { get; set; } = new();
    public bool HasPage { get; set; }
}
