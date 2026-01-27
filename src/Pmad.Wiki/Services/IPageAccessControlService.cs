namespace Pmad.Wiki.Services;

/// <summary>
/// Defines the contract for page-level access control operations.
/// </summary>
public interface IPageAccessControlService
{
    /// <summary>
    /// Checks if a user has permission to access a specific page.
    /// </summary>
    /// <param name="pageName">The name of the page to check.</param>
    /// <param name="userGroups">The groups the user belongs to.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The access permissions for the page.</returns>
    Task<PageAccessPermissions> CheckPageAccessAsync(string pageName, string[] userGroups, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all access control rules.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The list of all access control rules.</returns>
    Task<List<PageAccessRule>> GetRulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the access control rules.
    /// </summary>
    /// <param name="rules">The rules to save.</param>
    /// <param name="commitMessage">The commit message.</param>
    /// <param name="author">The author of the change.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    Task SaveRulesAsync(List<PageAccessRule> rules, string commitMessage, IWikiUser author, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the cached access control rules.
    /// </summary>
    void ClearCache();
}
