using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Pmad.Git.HttpServer;

namespace Pmad.Wiki
{
    public static class WikiEndpointRouteBuilderExtensions
    {
        public static IEndpointRouteBuilder MapWiki(this IEndpointRouteBuilder endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            var options = endpoints.ServiceProvider.GetRequiredService<WikiOptions>();

            if (options.EnableGitHttpServer)
            {
                endpoints.MapGitSmartHttp("/wiki.git");
            }

            return endpoints;
        }
    }
}
