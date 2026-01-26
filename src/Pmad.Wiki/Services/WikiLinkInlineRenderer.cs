using Markdig.Renderers;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax.Inlines;

namespace Pmad.Wiki.Services;

internal class WikiLinkInlineRenderer : LinkInlineRenderer
{
    private readonly string _basePath;

    public WikiLinkInlineRenderer(string basePath)
    {
        _basePath = basePath;
    }

    protected override void Write(HtmlRenderer renderer, LinkInline link)
    {
        if (link.Url != null && !IsAbsoluteUrl(link.Url) && link.Url.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            // Remove .md extension
            var url = link.Url;
            var anchorIndex = url.IndexOf('#');
            string anchor = string.Empty;
            
            if (anchorIndex >= 0)
            {
                anchor = url.Substring(anchorIndex);
                url = url.Substring(0, anchorIndex);
            }
            
            url = url.Substring(0, url.Length - 3);
            
            // Build proper route URL
            var pagePath = url.TrimStart('/');
            link.Url = $"/{_basePath}/view/{pagePath}{anchor}";
        }
        
        base.Write(renderer, link);
    }

    private static bool IsAbsoluteUrl(string url)
    {
        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               url.StartsWith("//", StringComparison.Ordinal);
    }
}
