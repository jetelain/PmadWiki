using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pmad.Git.HttpServer;

namespace Pmad.Wiki
{
    public static class WikiEndpointRouteBuilderExtensions
    {
        public static IEndpointRouteBuilder MapWiki(this IEndpointRouteBuilder endpoints, string pattern = "wiki")
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            var options = endpoints.ServiceProvider.GetRequiredService<IOptions<WikiOptions>>().Value;

            endpoints.MapControllerRoute(
                name: "wiki-view",
                pattern: $"{pattern}/{{id?}}",
                defaults: new { controller = "Wiki", action = "View" });

            endpoints.MapControllerRoute(
                name: "wiki-history",
                pattern: $"{pattern}/{{id}}/history",
                defaults: new { controller = "Wiki", action = "History" });

            endpoints.MapControllerRoute(
                name: "wiki-edit",
                pattern: $"{pattern}/{{id}}/edit",
                defaults: new { controller = "Wiki", action = "Edit" });

            return endpoints;
        }

        public static IEndpointRouteBuilder MapWikiGitHttpServer(this IEndpointRouteBuilder endpoints, string pattern = "/wiki.git")
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            endpoints.MapGitSmartHttp(pattern);

            return endpoints;
        }
    }
}
