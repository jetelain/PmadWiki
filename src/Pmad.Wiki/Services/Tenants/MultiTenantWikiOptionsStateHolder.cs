using Microsoft.Extensions.Options;

namespace Pmad.Wiki.Services.Tenants;

/// <summary>
/// State Holder to allow services to be used in a multi tenant context without having to pass tenant id to each method. 
/// The tenant-aware services will set the Value property at the beginning of each request, and the wiki services will read from it when needed.
/// </summary>
/// <remarks>
/// In a single scope, we will be able to process only a single tenant. To do actions on multiple tenants, create a scope for each tenant. 
/// This is typically not an issue in a web application where each request is a separate scope, but it may be a consideration in background processing scenarios.
/// </remarks>
internal class MultiTenantWikiOptionsStateHolder : IOptions<WikiOptions>
{
    private WikiOptions? _options;

    public WikiOptions Value 
    { 
        get
        {
            if (_options == null)
            {
                throw new InvalidOperationException("In a multi-tenant mode, you must call IWikiTenantHelper.TryConfigureOptionsForTenantAsync before resolving any Wiki service. On a MVC controller you can use [ServiceFilter(typeof(IWikiTenantActivationFilter))].");
            }
            return _options;
        }
        set
        {
            _options = value;
        }
    }
}
