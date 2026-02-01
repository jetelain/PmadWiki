using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Routing;

namespace Pmad.Wiki.Services;

internal class WikiLinkInlineRenderer : LinkInlineRenderer
{
    private readonly LinkGenerator _linkGenerator;
    private readonly string? _culture;

    public WikiLinkInlineRenderer(LinkGenerator linkGenerator, string? culture)
    {
        _linkGenerator = linkGenerator;
        _culture = culture;
    }

    protected override void Write(HtmlRenderer renderer, LinkInline link)
    {
        if (link.Url != null && !IsAbsoluteUrl(link.Url) && IsWikiLink(link.Url))
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
            
            // Build proper route URL using LinkGenerator
            var pagePath = url.TrimStart('/');
            var generatedUrl = _linkGenerator.GetPathByAction(
                action: "View",
                controller: "Wiki",
                values: new { id = pagePath, culture = _culture });
            
            if (generatedUrl != null)
            {
                link.Url = generatedUrl + anchor;
            }
            else
            {
                // Fallback if LinkGenerator fails (shouldn't happen in normal circumstances)
                link.Url = $"/wiki/view/{pagePath}{anchor}";
            }
        }
        
        base.Write(renderer, link);
    }

    private static bool IsAbsoluteUrl(string url)
    {
        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               url.StartsWith("//", StringComparison.Ordinal);
    }

    private static bool IsWikiLink(string url)
    {
        // Check if URL ends with .md or contains .md# (for anchors)
        return url.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
               url.Contains(".md#", StringComparison.OrdinalIgnoreCase);
    }
}
