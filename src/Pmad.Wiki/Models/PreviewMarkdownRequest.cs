namespace Pmad.Wiki.Models;

public class PreviewMarkdownRequest
{
    public string Markdown { get; set; } = string.Empty;
    public string? PageName { get; set; }
    public string? Culture { get; set; }
}
