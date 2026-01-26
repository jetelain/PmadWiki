namespace Pmad.Wiki.Services;

public interface IWikiPageService
{
    Task EnsureRepositoryCreated();

    Task<WikiPage?> GetPageAsync(string pageName, string? culture, CancellationToken cancellationToken = default);
    
    Task<List<WikiHistoryItem>> GetPageHistoryAsync(string pageName, string? culture, CancellationToken cancellationToken = default);
    
    Task<bool> PageExistsAsync(string pageName, string? culture, CancellationToken cancellationToken = default);
    
    Task<List<string>> GetAvailableCulturesForPageAsync(string pageName, CancellationToken cancellationToken = default);
    
    Task<List<WikiPageInfo>> GetAllPagesAsync(CancellationToken cancellationToken = default);
    
    Task SavePageAsync(string pageName, string? culture, string content, string commitMessage, Services.IWikiUser author, CancellationToken cancellationToken = default);
}
