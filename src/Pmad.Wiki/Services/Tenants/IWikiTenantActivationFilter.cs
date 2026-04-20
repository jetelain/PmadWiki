using Microsoft.AspNetCore.Mvc.Filters;

namespace Pmad.Wiki.Services.Tenants;

/// <summary>
/// Marker interface used as the DI key for the wiki tenant activation filter.
/// Registered as a no-op in single-tenant mode (<see cref="NullWikiTenantActivationFilter"/>)
/// and as <see cref="MultiTenantWikiFilter"/> in multi-tenant mode.
/// </summary>
public interface IWikiTenantActivationFilter : IAsyncResourceFilter { }
