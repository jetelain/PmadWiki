using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Pmad.Wiki.Services.Tenants;

internal class MultiTenantWikiHelper : IWikiTenantHelper
{
    private readonly MultiTenantWikiOptionsStateHolder _tenantOptions;
    private readonly WikiGlobalOptions _globalOptions;
    private readonly IWikiTenantOptionsResolver _resolver;

    public MultiTenantWikiHelper(
        MultiTenantWikiOptionsStateHolder tenantOptions,
        IOptions<WikiGlobalOptions> globalOptions,
        IWikiTenantOptionsResolver resolver)
    {
        _tenantOptions = tenantOptions;
        _globalOptions = globalOptions.Value;
        _resolver = resolver;
    }

    public string? ResolveRepositoryName(HttpContext context)
    {
        return _resolver.ResolveRepositoryName(context);
    }

    public async ValueTask<bool> TryConfigureOptionsForTenantAsync(HttpContext context)
    {
        var repositoryName = ResolveRepositoryName(context);
        if (string.IsNullOrEmpty(repositoryName))
        {
            return false;
        }
        var options = await _resolver.ResolveRepositoryOptionsAsync(repositoryName).ConfigureAwait(false);
        if (options == null)
        {
            return false;
        }
        options.WikiRepositoryName = repositoryName;
        _globalOptions.ApplyTo(options);
        _tenantOptions.Value = options;
        return true;
    }
}
