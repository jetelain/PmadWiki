using Microsoft.AspNetCore.Mvc.Filters;

namespace Pmad.Wiki.Services.Tenants;

/// <summary>
/// No-op implementation used in single-tenant mode. Does nothing and passes through.
/// </summary>
internal sealed class NullWikiTenantActivationFilter : IWikiTenantActivationFilter
{
    public Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        => next();
}
