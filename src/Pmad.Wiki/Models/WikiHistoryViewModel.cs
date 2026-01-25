namespace Pmad.Wiki.Models;

public class WikiHistoryViewModel
{
    public required string PageName { get; set; }
    public List<WikiHistoryEntry> Entries { get; set; } = new();
    public string? Culture { get; set; }
}
