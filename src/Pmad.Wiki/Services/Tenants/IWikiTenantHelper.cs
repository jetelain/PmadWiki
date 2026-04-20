using Microsoft.AspNetCore.Http;

namespace Pmad.Wiki.Services.Tenants;

public interface IWikiTenantHelper
{
    ValueTask<bool> TryConfigureOptionsForTenantAsync(HttpContext context);

    string? ResolveRepositoryName(HttpContext context);
}