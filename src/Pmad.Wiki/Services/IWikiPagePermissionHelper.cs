namespace Pmad.Wiki.Services;

/// <summary>
/// Defines the contract for evaluating page-level permissions for a given wiki user.
/// </summary>
public interface IWikiPagePermissionHelper
{
    /// <summary>
    /// Gets all pages that the specified user has permission to view.
    /// </summary>
    /// <param name="wikiUser">The user whose permissions are checked, or <c>null</c> for anonymous access.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The list of pages accessible to the user.</returns>
    Task<List<WikiPageInfo>> GetAllAccessiblePages(IWikiUserWithPermissions? wikiUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sub-pages of a given page that the specified user has permission to view.
    /// </summary>
    /// <param name="wikiUser">The user whose permissions are checked, or <c>null</c> for anonymous access.</param>
    /// <param name="pageName">The parent page name.</param>
    /// <param name="recursive">When <c>true</c> (default), all descendants are included; otherwise only direct children.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The list of accessible sub-pages.</returns>
    Task<List<WikiPageInfo>> GetAccessibleSubPages(IWikiUserWithPermissions? wikiUser, string pageName, bool recursive = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the specified user can view the given page.
    /// </summary>
    /// <param name="wikiUser">The user whose permissions are checked, or <c>null</c> for anonymous access.</param>
    /// <param name="pageName">The name of the page to check.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns><c>true</c> if the user can view the page; otherwise, <c>false</c>.</returns>
    ValueTask<bool> CanView(IWikiUserWithPermissions? wikiUser, string pageName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the specified user can edit the given page.
    /// </summary>
    /// <param name="wikiUser">The user whose permissions are checked, or <c>null</c> for anonymous access.</param>
    /// <param name="pageName">The name of the page to check.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns><c>true</c> if the user can edit the page; otherwise, <c>false</c>.</returns>
    ValueTask<bool> CanEdit(IWikiUserWithPermissions? wikiUser, string pageName, CancellationToken cancellationToken = default);

}
