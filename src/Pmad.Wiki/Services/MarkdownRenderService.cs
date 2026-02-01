using System.Collections.Concurrent;
using Markdig;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Pmad.Wiki.Services;

public sealed class MarkdownRenderService : IMarkdownRenderService
{
    private readonly ConcurrentDictionary<string, MarkdownPipeline> _pipelineCache = new();
    private readonly WikiOptions _options;
    private readonly LinkGenerator _linkGenerator;

    public MarkdownRenderService(IOptions<WikiOptions> options, LinkGenerator linkGenerator)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _linkGenerator = linkGenerator ?? throw new ArgumentNullException(nameof(linkGenerator));
    }

    public string ToHtml(string markdown, string? culture = null)
    {
        var pipeline = GetOrCreatePipeline(culture);
        return Markdown.ToHtml(markdown, pipeline);
    }

    private MarkdownPipeline GetOrCreatePipeline(string? culture)
    {
        var cacheKey = culture ?? _options.NeutralMarkdownPageCulture;
        
        return _pipelineCache.GetOrAdd(cacheKey, key =>
        {
            var builder = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .DisableHtml()
                .Use(new WikiLinkExtension(_linkGenerator, _options.NeutralMarkdownPageCulture == key ? null : key));

            if (_options.ConfigureMarkdown != null)
            {
                _options.ConfigureMarkdown(builder);
            }

            return builder.Build();
        });
    }
}
