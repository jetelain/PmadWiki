using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Pmad.Git.HttpServer;
using Pmad.Git.LocalRepositories;
using Pmad.Wiki.Helpers;
using Pmad.Wiki.Models;

namespace Pmad.Wiki.Services;

public class WikiPageMetadataCache : IWikiPageMetadataCache
{
    private readonly IGitRepositoryService _gitRepositoryService;
    private readonly WikiOptions _options;
    private readonly MemoryCacheGroup _cache;

    public WikiPageMetadataCache(
        IGitRepositoryService gitRepositoryService,
        IOptions<WikiOptions> options,
        IMemoryCache memoryCache)
    {
        ArgumentNullException.ThrowIfNull(gitRepositoryService);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(memoryCache);

        _gitRepositoryService = gitRepositoryService;
        _options = options.Value;
        _cache = new MemoryCacheGroup(memoryCache, $"WikiPageMetadataCache:{_options.WikiRepositoryName}", TimeSpan.FromDays(1));
    }

    public async Task<WikiPageMetadata?> GetPageMetadataAsync(string pageName, string? culture, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(pageName, culture);

        if (_cache.TryGetValue(cacheKey, out WikiPageMetadata? cachedMetadata))
        {
            return cachedMetadata;
        }

        var repository = GetRepository();
        var filePath = WikiFilePathHelper.GetFilePath(pageName, culture, _options.NeutralMarkdownPageCulture);

        try
        {
            var content = await repository.ReadFileAsync(filePath, _options.BranchName, cancellationToken);
            var contentText = Encoding.UTF8.GetString(content);
            return ExtractAndCacheMetadata(pageName, culture, contentText);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    public void ClearCache()
    {
        _cache.Clear();
    }

    public WikiPageMetadata ExtractAndCacheMetadata(string pageName, string? culture, string content)
    {
        var parsed = WikiPageContentParser.Parse(content);
        return ExtractAndCacheMetadata(pageName, culture, parsed);
    }

    public WikiPageMetadata ExtractAndCacheMetadata(string pageName, string? culture, WikiPageContent content)
    {
        var title = MarkdownTitleExtractor.ExtractFirstTitle(content, pageName);
        var metadata = new WikiPageMetadata(title, content.FrontMatter);
        var cacheKey = GetCacheKey(pageName, culture);
        _cache.Set(cacheKey, metadata);
        return metadata;
    }

    private string GetCacheKey(string pageName, string? culture)
    {
        return $"{pageName}:{culture ?? _options.NeutralMarkdownPageCulture}";
    }

    private IGitRepository GetRepository()
    {
        var repositoryPath = Path.Combine(_options.RepositoryRoot, _options.WikiRepositoryName);
        return _gitRepositoryService.GetRepositoryByPath(repositoryPath);
    }
}
