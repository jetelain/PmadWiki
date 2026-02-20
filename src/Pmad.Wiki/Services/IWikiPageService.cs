namespace Pmad.Wiki.Services;

public interface IWikiPageService
{
    Task EnsureRepositoryCreated();

    Task<WikiPage?> GetPageAsync(string pageName, string? culture, CancellationToken cancellationToken = default);
    
    Task<List<WikiHistoryItem>> GetPageHistoryAsync(string pageName, string? culture, CancellationToken cancellationToken = default);
    
    Task<WikiPage?> GetPageAtRevisionAsync(string pageName, string? culture, string commitId, CancellationToken cancellationToken = default);
    
    Task<bool> PageExistsAsync(string pageName, string? culture, CancellationToken cancellationToken = default);
    
    Task<List<string>> GetAvailableCulturesForPageAsync(string pageName, CancellationToken cancellationToken = default);
    
    Task<List<WikiPageInfo>> GetAllPagesAsync(CancellationToken cancellationToken = default);
    
    Task SavePageWithMediaAsync(string pageName, string? culture, string content, string commitMessage, Services.IWikiUser author, Dictionary<string, byte[]> mediaFiles, CancellationToken cancellationToken = default);

    Task<string?> GetPageTitleAsync(string pageName, string? culture, CancellationToken cancellationToken = default);

    Task<byte[]?> GetMediaFileAsync(string filePath, CancellationToken cancellationToken = default);

    Task<List<Models.MediaFileInfo>> GetAllMediaFilesAsync(CancellationToken cancellationToken = default);
}
