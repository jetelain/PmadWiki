using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html.Inlines;
using Microsoft.AspNetCore.Routing;

namespace Pmad.Wiki.Services;

internal class WikiLinkExtension : IMarkdownExtension
{
    private readonly LinkGenerator _linkGenerator;
    private readonly string? _culture;

    public WikiLinkExtension(LinkGenerator linkGenerator, string? culture)
    {
        _linkGenerator = linkGenerator;
        _culture = culture;
    }

    public void Setup(MarkdownPipelineBuilder pipeline)
    {
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer)
        {
            var linkRenderer = htmlRenderer.ObjectRenderers.FindExact<LinkInlineRenderer>();
            if (linkRenderer != null)
            {
                htmlRenderer.ObjectRenderers.Remove(linkRenderer);
            }
            htmlRenderer.ObjectRenderers.AddIfNotAlready(new WikiLinkInlineRenderer(_linkGenerator, _culture));
        }
    }
}
