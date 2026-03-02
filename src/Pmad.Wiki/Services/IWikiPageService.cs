namespace Pmad.Wiki.Services;

/// <summary>
/// Defines the contract for reading and writing wiki pages and media files.
/// </summary>
public interface IWikiPageService
{
    /// <summary>
    /// Ensures the underlying Git repository exists, creating it if necessary.
    /// </summary>
    Task EnsureRepositoryCreated();

    /// <summary>
    /// Gets the latest version of a page.
    /// </summary>
    /// <param name="pageName">The name of the page.</param>
    /// <param name="culture">The optional culture code. Pass <c>null</c> for the neutral culture.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The page, or <c>null</c> if it does not exist.</returns>
    Task<WikiPage?> GetPageAsync(string pageName, string? culture, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the revision history for a page.
    /// </summary>
    /// <param name="pageName">The name of the page.</param>
    /// <param name="culture">The optional culture code. Pass <c>null</c> for the neutral culture.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The list of history entries, ordered from newest to oldest.</returns>
    Task<List<WikiHistoryItem>> GetPageHistoryAsync(string pageName, string? culture, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a specific historical revision of a page.
    /// </summary>
    /// <param name="pageName">The name of the page.</param>
    /// <param name="culture">The optional culture code. Pass <c>null</c> for the neutral culture.</param>
    /// <param name="commitId">The Git commit identifier of the desired revision.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The page at the specified revision, or <c>null</c> if not found.</returns>
    Task<WikiPage?> GetPageAtRevisionAsync(string pageName, string? culture, string commitId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Determines whether a page exists.
    /// </summary>
    /// <param name="pageName">The name of the page.</param>
    /// <param name="culture">The optional culture code. Pass <c>null</c> for the neutral culture.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns><c>true</c> if the page exists; otherwise, <c>false</c>.</returns>
    Task<bool> PageExistsAsync(string pageName, string? culture, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all culture codes for which a page has a translation.
    /// </summary>
    /// <param name="pageName">The name of the page.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The list of culture codes available for the page.</returns>
    Task<List<string>> GetAvailableCulturesForPageAsync(string pageName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets summary information for every page in the wiki.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The list of all pages.</returns>
    Task<List<WikiPageInfo>> GetAllPagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets summary information for pages that are direct or indirect children of the specified page.
    /// </summary>
    /// <param name="pageName">The parent page name.</param>
    /// <param name="recursive">When <c>true</c> (default), all descendants are returned; otherwise only direct children.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The list of sub-pages.</returns>
    Task<List<WikiPageInfo>> GetSubPagesAsync(string pageName, bool recursive = true, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves a page along with any associated media files in a single Git commit.
    /// </summary>
    /// <param name="pageName">The name of the page.</param>
    /// <param name="culture">The optional culture code. Pass <c>null</c> for the neutral culture.</param>
    /// <param name="content">The raw Markdown content of the page.</param>
    /// <param name="commitMessage">The Git commit message.</param>
    /// <param name="author">The wiki user performing the save.</param>
    /// <param name="mediaFiles">A dictionary mapping relative media paths to their binary content.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    Task SavePageWithMediaAsync(string pageName, string? culture, string content, string commitMessage, Services.IWikiUser author, Dictionary<string, byte[]> mediaFiles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the title of a page without loading its full content.
    /// </summary>
    /// <param name="pageName">The name of the page.</param>
    /// <param name="culture">The optional culture code. Pass <c>null</c> for the neutral culture.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The page title, or <c>null</c> if the page does not exist.</returns>
    Task<string?> GetPageTitleAsync(string pageName, string? culture, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the raw bytes of a media file stored in the wiki repository.
    /// </summary>
    /// <param name="filePath">The repository-relative path of the media file.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The file bytes, or <c>null</c> if the file does not exist.</returns>
    Task<byte[]?> GetMediaFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about all media files stored in the wiki repository.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The list of all media files.</returns>
    Task<List<Models.MediaFileInfo>> GetAllMediaFilesAsync(CancellationToken cancellationToken = default);
}
