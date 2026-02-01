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

            endpoints.MapControllerRoute(
                name: "wiki-sitemap",
                pattern: $"{pattern}/sitemap",
                defaults: new { controller = "Wiki", action = "SiteMap" });

            endpoints.MapControllerRoute(
                name: "wiki-view",
                pattern: $"{pattern}/view/{{**id}}",
                defaults: new { controller = "Wiki", action = "View" });

            endpoints.MapControllerRoute(
                name: "wiki-history",
                pattern: $"{pattern}/history/{{**id}}",
                defaults: new { controller = "Wiki", action = "History" });

            endpoints.MapControllerRoute(
                name: "wiki-edit",
                pattern: $"{pattern}/edit/{{**id}}",
                defaults: new { controller = "Wiki", action = "Edit" });

            endpoints.MapControllerRoute(
                name: "wiki-revision",
                pattern: $"{pattern}/revision/{{**id}}",
                defaults: new { controller = "Wiki", action = "Revision" });

            endpoints.MapControllerRoute(
                name: "wiki-diff",
                pattern: $"{pattern}/diff/{{**id}}",
                defaults: new { controller = "Wiki", action = "Diff" });

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
