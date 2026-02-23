using Pmad.Wiki.Models;

namespace Pmad.Wiki.Services;

public interface IWikiPageTitleCache
{
    Task<string?> GetPageTitleAsync(string pageName, string? culture, CancellationToken cancellationToken = default);
    string ExtractAndCacheTitle(string pageName, string? culture, string content);
    string ExtractAndCacheTitle(string pageName, string? culture, WikiPageContent content);
    void ClearCache();
}
