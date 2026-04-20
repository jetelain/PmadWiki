using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Pmad.Wiki.Services.Tenants;

/// <summary>
/// MVC resource filter that resolves tenant options from the current HTTP context and populates
/// the scoped <see cref="MultiTenantWikiOptionsStateHolder"/> state holder before the controller is constructed.
/// Returns 404 if the tenant cannot be resolved.
/// </summary>
internal sealed class MultiTenantWikiFilter : IWikiTenantActivationFilter
{
    private readonly IWikiTenantHelper _helper;

    public MultiTenantWikiFilter(IWikiTenantHelper helper)
    {
        _helper = helper;
    }

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        if (!await _helper.TryConfigureOptionsForTenantAsync(context.HttpContext))
        {
            context.Result = new NotFoundResult();
            return;
        }
        await next();
    }
}