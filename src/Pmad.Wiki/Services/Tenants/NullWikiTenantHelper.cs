using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Pmad.Wiki.Services.Tenants;

internal class NullWikiTenantHelper : IWikiTenantHelper
{
    private readonly IOptions<WikiOptions> _options;

    public NullWikiTenantHelper(IOptions<WikiOptions> options)
    {
        _options = options;
    }

    public string? ResolveRepositoryName(HttpContext context)
    {
        return _options.Value.WikiRepositoryName;
    }

    public ValueTask<bool> TryConfigureOptionsForTenantAsync(HttpContext context)
    {
        return ValueTask.FromResult(true);
    }
}
