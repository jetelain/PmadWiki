namespace Pmad.Wiki.Services;

public interface IWikiPageEditService
{
    Task SavePageAsync(string pageName, string? culture, string content, string commitMessage, IWikiUser author, CancellationToken cancellationToken = default);
}
