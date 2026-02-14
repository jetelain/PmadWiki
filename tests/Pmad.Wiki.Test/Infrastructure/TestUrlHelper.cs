using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Pmad.Wiki.Test.Infrastructure;

/// <summary>
/// Simple URL helper for integration tests.
/// Provides consistent behavior with TestLinkGenerator.
/// </summary>
internal class TestUrlHelper : IUrlHelper
{
    private readonly string _basePath;

    public TestUrlHelper(string basePath = "wiki")
    {
        _basePath = basePath;
    }

    public ActionContext ActionContext => new ActionContext();

    public string? Action(UrlActionContext actionContext)
    {
        var routeValues = actionContext.Values as RouteValueDictionary;
        var action = actionContext.Action;
        var controller = actionContext.Controller;
        var id = routeValues?["id"]?.ToString() ?? "";

        // Match TestLinkGenerator behavior
        if (action == "View" && controller == "Wiki")
        {
            var culture = routeValues?["culture"]?.ToString();
            var path = $"/{_basePath}/view/{id}";
            if (!string.IsNullOrEmpty(culture))
            {
                path += $"?culture={culture}";
            }
            return path;
        }
        else if (action == "Media" && controller == "Wiki")
        {
            return $"/{_basePath}/media/{id}";
        }
        else if (action == "TempMedia" && controller == "Wiki")
        {
            return $"/{_basePath}/tempmedia/{id}";
        }

        // Fallback for other actions
        return $"/{_basePath}/{action?.ToLowerInvariant()}/{id}";
    }

    public string? Content(string? contentPath) => contentPath;

    public bool IsLocalUrl(string? url) => true;

    public string? Link(string? routeName, object? values) => null;

    public string? RouteUrl(UrlRouteContext routeContext) => null;
}
