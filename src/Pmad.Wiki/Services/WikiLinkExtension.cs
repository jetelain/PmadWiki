using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html.Inlines;

namespace Pmad.Wiki.Services;

internal class WikiLinkExtension : IMarkdownExtension
{
    private readonly string _basePath;

    public WikiLinkExtension(string basePath)
    {
        _basePath = basePath;
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
            htmlRenderer.ObjectRenderers.AddIfNotAlready(new WikiLinkInlineRenderer(_basePath));
        }
    }
}
