using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pmad.Git.HttpServer;
using Pmad.Wiki.Services;

namespace Pmad.Wiki;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register wiki services.
/// </summary>
public static class WikiServiceCollectionExtensions
{
    /// <summary>
    /// Registers all core wiki services into the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">A delegate to configure <see cref="WikiOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWiki(this IServiceCollection services, Action<WikiOptions> options)
    {
        services.Configure<WikiOptions>(options);

        services.AddLocalization();

        services.AddSingleton<IMarkdownRenderService, MarkdownRenderService>();
        services.AddScoped<IWikiPageService, WikiPageService>();
        services.AddScoped<IPageAccessControlService, PageAccessControlService>();
        services.AddSingleton<IWikiPageMetadataCache, WikiPageMetadataCache>();
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

        return services;
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
            .Configure<IOptions<WikiOptions>>((gitOptions, wikiOptions) =>
            {
                gitOptions.RepositoryRoot = wikiOptions.Value.RepositoryRoot;
                gitOptions.EnableUploadPack = true;
                gitOptions.EnableReceivePack = true;
                gitOptions.RepositoryNameNormalizer = _ => wikiOptions.Value.WikiRepositoryName;
                gitOptions.RepositoryResolver = _ => wikiOptions.Value.WikiRepositoryName;
                gitOptions.AuthorizeAsync = (context, _, operation, cancellationToken) =>
                    context.RequestServices.GetRequiredService<IWikiGitAuthorization>().AuthorizeGitHttpAsync(context, operation, cancellationToken);
                gitOptions.OnReceivePackCompleted = (context, repo, result) => {
                    context.RequestServices.GetRequiredService<IPageAccessControlService>().ClearCache();
                    context.RequestServices.GetRequiredService<IWikiPageMetadataCache>().ClearCache();
                    return ValueTask.CompletedTask;
                };
            });

        services.AddGitSmartHttp(_ => { });

        services.AddScoped<IWikiGitAuthorization, WikiGitAuthorization>();

        return services;
    }
}
