using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Pmad.Git.HttpServer;

namespace Pmad.Wiki;

/// <summary>
/// Extension methods for <see cref="IEndpointRouteBuilder"/> to map wiki routes.
/// </summary>
public static class WikiEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps all wiki controller routes under the specified URL prefix.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The URL prefix for all wiki routes. Defaults to <c>"wiki"</c>.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
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

        endpoints.MapControllerRoute(
            name: "wiki-media",
            pattern: $"{pattern}/media/{{**id}}",
            defaults: new { controller = "Wiki", action = "Media" });

        endpoints.MapControllerRoute(
            name: "wiki-create",
            pattern: $"{pattern}/create/{{**id}}",
            defaults: new { controller = "Wiki", action = "Create" });

        endpoints.MapControllerRoute(
            name: "wiki-create-page",
            pattern: $"{pattern}/createpage/{{**id}}",
            defaults: new { controller = "Wiki", action = "CreatePage" });

        endpoints.MapControllerRoute(
            name: "wiki-admin-access-control",
            pattern: $"{pattern}/admin/access-control",
            defaults: new { controller = "WikiAdmin", action = "AccessControl" });

        endpoints.MapControllerRoute(
            name: "wiki-admin-edit-access-control",
            pattern: $"{pattern}/admin/access-control/edit",
            defaults: new { controller = "WikiAdmin", action = "EditAccessControl" });

        return endpoints;
    }

    /// <summary>
    /// Maps the Git Smart HTTP server endpoint for the wiki repository.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The URL pattern for the Git Smart HTTP endpoint. Defaults to <c>"/wiki.git"</c>.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapWikiGitHttpServer(this IEndpointRouteBuilder endpoints, string pattern = "/wiki.git")
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGitSmartHttp(pattern);

        return endpoints;
    }
}
