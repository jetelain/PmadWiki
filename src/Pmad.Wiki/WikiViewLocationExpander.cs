using Microsoft.AspNetCore.Mvc.Razor;

namespace Pmad.Wiki;

internal sealed class WikiViewLocationExpander : IViewLocationExpander
{
    public void PopulateValues(ViewLocationExpanderContext context) 
    { 
    }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        if (context.ControllerName is "WikiAdmin")
        {
            return viewLocations.Select(l => l.Replace("{1}", "Wiki"));
        }
        return viewLocations;
    }
}
