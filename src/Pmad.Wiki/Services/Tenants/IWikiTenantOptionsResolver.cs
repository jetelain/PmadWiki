using Microsoft.AspNetCore.Http;

namespace Pmad.Wiki.Services.Tenants;

public interface IWikiTenantOptionsResolver
{
    string? ResolveRepositoryName(HttpContext httpContext);

    Task<WikiOptions?> ResolveRepositoryOptionsAsync(string repositoryName);
}