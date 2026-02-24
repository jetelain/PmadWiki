using Pmad.Wiki.Helpers;
using Pmad.Wiki.Models;

namespace Pmad.Wiki.Services;

public class WikiPage
{
    public required WikiPageContent Content { get; set; }
    public required string PageName { get; set; }
    public string RawContent => Content.RawContent;
    public WikiPageFrontMatter FrontMatter => Content.FrontMatter;
    public string ContentWithoutFrontMatter => Content.ContentWithoutFrontMatter;
    public required string Title { get; set; }
    public string? Culture { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public required string ContentHash { get; set; }
}
