using System.Security.Claims;

namespace Pmad.Wiki.Services;

/// <summary>
/// Defines the contract for user-related operations within the wiki system.
/// Must be implemented by the host application to manage wiki users.
/// </summary>
public interface IWikiUserService
{
    /// <summary>
    /// Retrieves the wiki user associated with the specified claims principal.
    /// </summary>
    /// <param name="principal">The claims principal representing the authenticated user. Cannot be null.</param>
    /// <param name="shouldCreate">If the host application has a lazy user-creation mechanism, it should create the user.
    /// This allows avoiding creating database users for read-only operations.</param>
    /// <param name="cancellationToken">Token used to cancel the async operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the wiki user associated with the
    /// principal, or null if no matching user is found.</returns>
    Task<IWikiUserWithPermissions?> GetWikiUserAsync(ClaimsPrincipal principal, bool shouldCreate, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the wiki user associated with the specified Git email address.
    /// </summary>
    /// <param name="gitEmail">The Git email address to look up. Cannot be null or empty.</param>
    /// <param name="cancellationToken">Token used to cancel the async operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the corresponding wiki user if
    /// found; otherwise, null.</returns>
    Task<IWikiUser?> GetWikiUserFromGitEmailAsync(string gitEmail, CancellationToken cancellationToken);
}
