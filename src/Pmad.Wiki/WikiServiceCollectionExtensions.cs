using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pmad.Git.HttpServer;
using Pmad.Wiki.Services;
using Pmad.Wiki.Services.Tenants;

namespace Pmad.Wiki;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register wiki services.
/// </summary>
public static class WikiServiceCollectionExtensions
{
    /// <summary>
    /// Registers all core wiki services into the dependency injection container with support for single-tenant scenarios.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">A delegate to configure <see cref="WikiOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWiki(this IServiceCollection services, Action<WikiOptions> options)
    {
        services.Configure<WikiOptions>(options);
        services.AddSingleton<IOptions<WikiGlobalOptions>>(sp => sp.GetRequiredService<IOptions<WikiOptions>>());
        services.AddSingleton<IWikiTenantActivationFilter, NullWikiTenantActivationFilter>();
        services.AddSingleton<IWikiTenantHelper, NullWikiTenantHelper>();

        AddCommonServices(services);

        return services;
    }

    /// <summary>
    /// Registers all core wiki services into the dependency injection container with support for multi-tenancy. 
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">A delegate to configure <see cref="WikiGlobalOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWikiMultiTenant(this IServiceCollection services, Action<WikiGlobalOptions> options)
    {
        services.Configure<WikiGlobalOptions>(options);
        services.AddScoped<MultiTenantWikiOptionsStateHolder>();
        services.AddScoped<IOptions<WikiOptions>>(sp => sp.GetRequiredService<MultiTenantWikiOptionsStateHolder>());
        services.AddScoped<IWikiTenantActivationFilter, MultiTenantWikiFilter>();
        services.AddScoped<IWikiTenantHelper, MultiTenantWikiHelper>();
        services.AddScoped<IWikiUserService, MultiTenantUserServiceAdapter>();

        AddCommonServices(services);

        return services;
    }

    private static void AddCommonServices(IServiceCollection services)
    {
        services.AddLocalization();

        services.AddScoped<IMarkdownRenderService, MarkdownRenderService>();
        services.AddScoped<IWikiPageService, WikiPageService>();
        services.AddScoped<IPageAccessControlService, PageAccessControlService>();
        services.AddScoped<IWikiPageMetadataCache, WikiPageMetadataCache>();
        services.AddSingleton<ITemporaryMediaStorageService, TemporaryMediaStorageService>();
        services.AddScoped<IWikiPageEditService, WikiPageEditService>();
        services.AddScoped<IWikiTemplateService, WikiTemplateService>();
        services.AddScoped<IWikiPagePermissionHelper, WikiPagePermissionHelper>();

        services.AddMemoryCache();
        services.AddGitRepositoryService();

        services.Configure<RazorViewEngineOptions>(options =>
        {
            options.ViewLocationExpanders.Add(new WikiViewLocationExpander());
        });
    }

    /// <summary>
    /// Registers a background service that periodically cleans up abandoned temporary media files.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWikiTemporaryMediaCleanup(this IServiceCollection services)
    {
        services.AddHostedService<TemporaryMediaCleanupService>();
        return services;
    }

    /// <summary>
    /// Registers services required to expose the wiki repository over the Git Smart HTTP protocol.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWikiGitHttpServer(this IServiceCollection services)
    {
        services.AddOptions<GitSmartHttpOptions>()
            .Configure<IOptions<WikiGlobalOptions>>((gitOptions, wikiOptions) =>
            {
                gitOptions.RepositoryRoot = wikiOptions.Value.RepositoryRoot;
                gitOptions.EnableUploadPack = true;
                gitOptions.EnableReceivePack = true;
                gitOptions.RepositoryResolver = (context) =>
                    context.RequestServices.GetRequiredService<IWikiTenantHelper>().ResolveRepositoryName(context);
                gitOptions.AuthorizeAsync = (context, _, operation, cancellationToken) =>
                    context.RequestServices.GetRequiredService<IWikiGitAuthorization>().AuthorizeGitHttpAsync(context, operation, cancellationToken);
                gitOptions.OnReceivePackCompleted = async (context, repo, result) => {
                    if (!await context.RequestServices.GetRequiredService<IWikiTenantHelper>().TryConfigureOptionsForTenantAsync(context))
                    {
                        context.RequestServices.GetRequiredService<IPageAccessControlService>().ClearCache();
                        context.RequestServices.GetRequiredService<IWikiPageMetadataCache>().ClearCache();
                    }
                };
            });

        services.AddGitSmartHttp(_ => { });

        services.AddScoped<IWikiGitAuthorization, WikiGitAuthorization>();

        return services;
    }
}
