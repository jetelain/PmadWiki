using Microsoft.Extensions.DependencyInjection;
using Pmad.Git.HttpServer;
using Pmad.Wiki.Services;

namespace Pmad.Wiki;

public static class WikiServiceCollectionExtensions
{
    public static void AddWiki(this IServiceCollection services, WikiOptions options)
    {
        if (options.EnableGitHttpServer)
        {
            services.AddGitSmartHttp(new GitSmartHttpOptions()
            {
                RepositoryRoot = options.RepositoryRoot,
                EnableUploadPack = true,
                EnableReceivePack = true,
                RepositoryNameNormalizer = _ => options.WikiRepositoryName,
                RepositoryResolver = _ => options.WikiRepositoryName,
                AuthorizeAsync = (context, _, cancellationToken) => 
                    context.RequestServices.GetRequiredService<IWikiGitAuthorization>().AuthorizeGitHttpAsync(context, options, cancellationToken)
            });

            services.AddScoped<IWikiGitAuthorization, WikiGitAuthorization>();
        }

    }
}
