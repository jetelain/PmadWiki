using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Pmad.Wiki.Test.Infrastructure;

internal class TestLinkGenerator : LinkGenerator
{
    private readonly string _basePath;

    public TestLinkGenerator(string basePath = "wiki")
    {
        _basePath = basePath;
    }

    public override string? GetPathByAddress<TAddress>(
        HttpContext httpContext,
        TAddress address,
        RouteValueDictionary values,
        RouteValueDictionary? ambientValues = null,
        PathString? pathBase = null,
        FragmentString fragment = default,
        LinkOptions? options = null)
    {
        return GetPathByAddress(address, values, pathBase ?? PathString.Empty, fragment, options);
    }

    public override string? GetPathByAddress<TAddress>(
        TAddress address,
        RouteValueDictionary values,
        PathString pathBase = default,
        FragmentString fragment = default,
        LinkOptions? options = null)
    {
        if (values == null)
        {
            return null;
        }

        var action = values["action"]?.ToString();
        var controller = values["controller"]?.ToString();
        var id = values["id"]?.ToString() ?? "";

        if (action == "View" && controller == "Wiki")
        {
            var culture = values["culture"]?.ToString();
            var path = $"/{_basePath}/view/{id}";
            if (!string.IsNullOrEmpty(culture))
            {
                path += $"?culture={culture}";
            }
            return path;
        }
        else if (action == "Media" && controller == "Wiki")
        {
            var path = $"/{_basePath}/media/{id}";
            return path;
        }
        else if (action == "TempMedia" && controller == "Wiki")
        {
            var path = $"/{_basePath}/tempmedia/{id}";
            return path;
        }

        return null;
    }

    public override string? GetUriByAddress<TAddress>(
        HttpContext httpContext,
        TAddress address,
        RouteValueDictionary values,
        RouteValueDictionary? ambientValues = null,
        string? scheme = null,
        HostString? host = null,
        PathString? pathBase = null,
        FragmentString fragment = default,
        LinkOptions? options = null)
    {
        throw new NotImplementedException();
    }

    public override string? GetUriByAddress<TAddress>(
        TAddress address,
        RouteValueDictionary values,
        string scheme,
        HostString host,
        PathString pathBase = default,
        FragmentString fragment = default,
        LinkOptions? options = null)
    {
        throw new NotImplementedException();
    }
}
