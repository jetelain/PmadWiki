using Pmad.Wiki.Models;

namespace Pmad.Wiki.Services;

/// <summary>
/// Defines the contract for caching extracted wiki page metadata (title and front matter).
/// </summary>
public interface IWikiPageMetadataCache
{
    /// <summary>
    /// Gets the cached metadata for a page, reading it from the repository if not already cached.
    /// </summary>
    /// <param name="pageName">The name of the page.</param>
    /// <param name="culture">The optional culture code. Pass <c>null</c> for the neutral culture.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The metadata, or <c>null</c> if the page does not exist.</returns>
    Task<WikiPageMetadata?> GetPageMetadataAsync(string pageName, string? culture, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts metadata from raw Markdown content and stores it in the cache.
    /// </summary>
    /// <param name="pageName">The name of the page.</param>
    /// <param name="culture">The optional culture code. Pass <c>null</c> for the neutral culture.</param>
    /// <param name="content">The raw Markdown content string.</param>
    /// <returns>The extracted metadata.</returns>
    WikiPageMetadata ExtractAndCacheMetadata(string pageName, string? culture, string content);

    /// <summary>
    /// Extracts metadata from a parsed <see cref="WikiPageContent"/> and stores it in the cache.
    /// </summary>
    /// <param name="pageName">The name of the page.</param>
    /// <param name="culture">The optional culture code. Pass <c>null</c> for the neutral culture.</param>
    /// <param name="content">The parsed page content.</param>
    /// <returns>The extracted metadata.</returns>
    WikiPageMetadata ExtractAndCacheMetadata(string pageName, string? culture, WikiPageContent content);

    /// <summary>
    /// Clears all cached metadata, forcing it to be reloaded on next access.
    /// </summary>
    void ClearCache();
}
