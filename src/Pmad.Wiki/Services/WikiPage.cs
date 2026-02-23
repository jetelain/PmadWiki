using Pmad.Wiki.Helpers;
using Pmad.Wiki.Models;

namespace Pmad.Wiki.Services;

public class WikiPage
{
    private WikiPageContent? _parsed;

    internal WikiPageContent Parsed
    {
        get => _parsed ??= WikiPageFrontMatterParser.Parse(Content);
        set => _parsed = value;
    }

    public required string PageName { get; set; }
    public required string Content { get; set; }
    public WikiPageFrontMatter FrontMatter => Parsed.FrontMatter;
    public string ContentWithoutFrontMatter => Parsed.ContentWithoutFrontMatter;
    public required string Title { get; set; }
    public string? Culture { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public required string ContentHash { get; set; }
}
