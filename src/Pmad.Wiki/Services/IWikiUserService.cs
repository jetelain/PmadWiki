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
    /// <param name="shouldCreate">When true, the host may create a corresponding wiki user if one does not already exist.
    /// When false, the host must not create a new user and should return an existing user if found; otherwise, null. This allows callers to avoid creating database users for read-only operations.</param>
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
