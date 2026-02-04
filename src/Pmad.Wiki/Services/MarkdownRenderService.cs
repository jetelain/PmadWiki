using System.Collections.Concurrent;
using Markdig;
using Markdig.Parsers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Pmad.Wiki.Helpers;

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

    public string ToHtml(string markdown, string? culture = null, string? currentPageName = null)
    {
        var pipeline = GetOrCreatePipeline(culture); 

        var document = MarkdownParser.Parse(markdown, pipeline);

        // Process wiki links to make them relative to the current page
        ProcessWikiLinks(document, currentPageName ?? string.Empty, culture);

        return Markdown.ToHtml(document, pipeline);
    }

    private void ProcessWikiLinks(Markdig.Syntax.MarkdownDocument document, string currentPageName, string? culture)
    {
        // Pre-compute current page directory parts to avoid repeated splitting
        var currentPageDirectoryParts = GetDirectoryParts(currentPageName);

        foreach (var linkInline in document.Descendants<LinkInline>())
        {
            if (linkInline.Url != null && !IsAbsoluteUrl(linkInline.Url))
            {
                var match = WikiInputValidator.PageNativePathRegex().Match(linkInline.Url);
                if (match.Success)
                {
                    linkInline.Url = ProcessSingleWikiLink(match.Groups[1].Value, match.Groups[2].Value, currentPageDirectoryParts, culture);
                }
                else if (IsMedia(linkInline.Url))
                {
                    linkInline.Url = ProcessMediaLink(linkInline.Url, currentPageDirectoryParts);
                }
            }
        }
    }

    private bool IsMedia(string url)
    {
        return WikiInputValidator.MediaPathRegex().IsMatch(url)
            && _options.AllowedMediaExtensions.Any(ext => url.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    private string ProcessMediaLink(string url, List<string> currentPageDirectoryParts)
    {
        var targetMedia = ResolvePath(url, currentPageDirectoryParts);

        return GenerateMediaUrl(targetMedia);
    }

    private string ProcessSingleWikiLink(string urlWithoutExtension, string anchor, List<string> currentPageDirectoryParts, string? culture)
    {
        var targetPageName = ResolvePath(urlWithoutExtension, currentPageDirectoryParts);
        var generatedUrl = GenerateWikiUrl(targetPageName, culture);
        return generatedUrl + anchor;
    }


    private static string ResolvePath(string url, List<string> currentPageDirectoryParts)
    {
        if (url.StartsWith("/"))
        {
            // Absolute path from wiki root
            return url.TrimStart('/');
        }
        
        // Relative path - resolve against current page directory
        return ResolveRelativePath(currentPageDirectoryParts, url);
    }

    private string GenerateWikiUrl(string targetPageName, string? culture)
    {
        var generatedUrl = _linkGenerator.GetPathByAction(
            action: "View",
            controller: "Wiki",
            values: new { id = targetPageName, culture = culture });
        
        if (generatedUrl != null)
        {
            return generatedUrl;
        }
        
        // Fallback if LinkGenerator fails
        return $"/wiki/view/{targetPageName}";
    }

    private string GenerateMediaUrl(string mediaPath)
    {
        var generatedUrl = _linkGenerator.GetPathByAction(
            action: "Media",
            controller: "Wiki",
            values: new { id = mediaPath });

        if (generatedUrl != null)
        {
            return generatedUrl;
        }

        // Fallback if LinkGenerator fails
        return $"/wiki/media/{mediaPath}";
    }

    private static List<string> GetDirectoryParts(string pagePath)
    {
        var pageParts = pagePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Get all parts except the last one (the file name)
        return [.. pageParts.Take(pageParts.Length - 1)];
    }

    private static string ResolveRelativePath(List<string> currentPageDirectoryParts, string relativePath)
    {
        var relativeParts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var resultParts = new List<string>(currentPageDirectoryParts);
        
        // Process each part of the relative path
        foreach (var part in relativeParts)
        {
            if (part == "..")
            {
                // Go up one level
                if (resultParts.Count > 0)
                {
                    resultParts.RemoveAt(resultParts.Count - 1);
                }
            }
            else if (part != ".")
            {
                // Add the part
                resultParts.Add(part);
            }
            // Skip "." as it means current directory
        }
        
        return string.Join("/", resultParts);
    }

    private static bool IsAbsoluteUrl(string url)
    {
        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               url.StartsWith("//", StringComparison.Ordinal);
    }

    private MarkdownPipeline GetOrCreatePipeline(string? culture)
    {
        var cacheKey = culture ?? _options.NeutralMarkdownPageCulture;
        
        return _pipelineCache.GetOrAdd(cacheKey, key =>
        {
            var builder = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .DisableHtml();

            if (_options.ConfigureMarkdown != null)
            {
                _options.ConfigureMarkdown(builder);
            }

            return builder.Build();
        });
    }
}
