namespace Pmad.Wiki.Services;

public interface IWikiPagePermissionHelper
{
    Task<List<WikiPageInfo>> GetAllAccessiblePages(IWikiUserWithPermissions? wikiUser, CancellationToken cancellationToken = default);

    ValueTask<bool> CanView(IWikiUserWithPermissions? wikiUser, string pageName, CancellationToken cancellationToken = default);

    ValueTask<bool> CanEdit(IWikiUserWithPermissions? wikiUser, string pageName, CancellationToken cancellationToken = default);

}
