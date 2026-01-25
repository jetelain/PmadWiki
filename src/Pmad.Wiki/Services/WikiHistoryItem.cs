namespace Pmad.Wiki.Services;

public class WikiHistoryItem
{
    public required string CommitId { get; set; }
    public required string Message { get; set; }
    public required string AuthorName { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}
