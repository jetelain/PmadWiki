using Pmad.Wiki.Models;

namespace Pmad.Wiki.Services;

public interface IWikiPageMetadataCache
{
    Task<WikiPageMetadata?> GetPageMetadataAsync(string pageName, string? culture, CancellationToken cancellationToken = default);
    WikiPageMetadata ExtractAndCacheMetadata(string pageName, string? culture, string content);
    WikiPageMetadata ExtractAndCacheMetadata(string pageName, string? culture, WikiPageContent content);
    void ClearCache();
}
