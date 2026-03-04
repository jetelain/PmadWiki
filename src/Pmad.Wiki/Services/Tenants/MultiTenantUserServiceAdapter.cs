using System.Security.Claims;

namespace Pmad.Wiki.Services.Tenants;

internal sealed class MultiTenantUserServiceAdapter : IWikiUserService
{
    private readonly IWikiMultiTenantUserService _wikiMultiTenantUserService;
    private readonly WikiOptions _options;

    public MultiTenantUserServiceAdapter(IWikiMultiTenantUserService wikiMultiTenantUserService, MultiTenantWikiOptionsStateHolder optionsHolder)
    {
        _wikiMultiTenantUserService = wikiMultiTenantUserService;
        _options = optionsHolder.Value;
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
