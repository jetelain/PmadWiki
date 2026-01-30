namespace Pmad.Wiki.Models;

public class WikiPageLinkInfo
{
    public required string PageName { get; set; }
    
    public string? Title { get; set; }
    
    public required string RelativePath { get; set; }
}
