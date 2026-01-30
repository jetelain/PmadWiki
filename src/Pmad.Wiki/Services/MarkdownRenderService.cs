using Markdig;
using Microsoft.Extensions.Options;

namespace Pmad.Wiki.Services;

public sealed class MarkdownRenderService : IMarkdownRenderService
{
    private readonly MarkdownPipeline _markdownPipeline;

    public MarkdownRenderService(IOptions<WikiOptions> options)
    {
        var actualOptions = options.Value ?? throw new ArgumentNullException(nameof(options));

        var builder = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .DisableHtml()
            .Use(new WikiLinkExtension(actualOptions.BasePath));

        if (actualOptions.ConfigureMarkdown != null)
        {
            actualOptions.ConfigureMarkdown(builder);
        }

        _markdownPipeline = builder.Build();
    }

    public string ToHtml(string markdown)
    {
        return Markdown.ToHtml(markdown, _markdownPipeline);
    }
}
