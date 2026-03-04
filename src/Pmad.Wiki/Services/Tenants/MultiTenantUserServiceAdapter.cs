using System.Security.Claims;

namespace Pmad.Wiki.Services.Tenants;

internal class MultiTenantUserServiceAdapter : IWikiUserService
{
    private readonly IWikiMultiTenantUserService _wikiMultiTenantUserService;
    private readonly WikiOptions _options;

    public MultiTenantUserServiceAdapter(IWikiMultiTenantUserService wikiMultiTenantUserService, WikiOptions options)
    {
        _wikiMultiTenantUserService = wikiMultiTenantUserService;
        _options = options;
    }

    public Task<IWikiUserWithPermissions?> GetWikiUserAsync(ClaimsPrincipal principal, bool shouldCreate, CancellationToken cancellationToken)
    {
        return _wikiMultiTenantUserService.GetWikiUserAsync(_options.WikiRepositoryName, principal, shouldCreate, cancellationToken);
    }

    public Task<IWikiUser?> GetWikiUserFromGitEmailAsync(string gitEmail, CancellationToken cancellationToken)
    {
        return _wikiMultiTenantUserService.GetWikiUserFromGitEmailAsync(_options.WikiRepositoryName, gitEmail, cancellationToken);
    }
}
