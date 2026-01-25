using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pmad.Git.HttpServer;
using Pmad.Wiki.Services;

namespace Pmad.Wiki;

public static class WikiServiceCollectionExtensions
{
    public static IServiceCollection AddWiki(this IServiceCollection services, Action<WikiOptions> options)
    {
        services.Configure<WikiOptions>(options);

        services.AddScoped<IWikiPageService, WikiPageService>();
        services.AddGitRepositoryService();

        return services;
    }

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
                gitOptions.AuthorizeAsync = (context, _, cancellationToken) =>
                    context.RequestServices.GetRequiredService<IWikiGitAuthorization>().AuthorizeGitHttpAsync(context, cancellationToken);
            });

        services.AddGitSmartHttp(_ => { });

        services.AddScoped<IWikiGitAuthorization, WikiGitAuthorization>();

        return services;
    }
}
