using System.Security.Claims;

namespace Pmad.Wiki.Services;

/// <summary>
/// Defines the contract for user-related operations within the wiki system.
/// 
/// For single-tenant scenarios, it must be implemented by the host application to manage wiki users.
/// For multi-tenant scenarios, implement <see cref="Tenants.IWikiMultiTenantUserService"/> instead to provide tenant-aware user management.
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

    /// <summary>
    /// Retrieves all user groups defined in the system.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the async operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the collection of all
    /// user groups available in the system. Each group exposes a <see cref="IWikiUserGroup.Name"/>,
    /// an optional <see cref="IWikiUserGroup.Label"/> for display purposes, and an optional
    /// <see cref="IWikiUserGroup.Description"/>. Returns an empty collection if no groups are defined.
    /// </returns>
    /// <remarks>
    /// Implementing this method is optional. It is only used to enrich the access control administration
    /// UI by resolving group names to human-readable labels and descriptions. The default implementation
    /// returns an empty collection, which causes group names to be displayed as-is.
    /// </remarks>
    Task<IEnumerable<IWikiUserGroup>> GetAllWikiGroupsAsync(CancellationToken cancellationToken)
        => Task.FromResult(Enumerable.Empty<IWikiUserGroup>());
}
