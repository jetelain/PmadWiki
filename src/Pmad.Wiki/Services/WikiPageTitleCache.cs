using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Options;
using Pmad.Git.HttpServer;
using Pmad.Git.LocalRepositories;
using Pmad.Wiki.Helpers;

namespace Pmad.Wiki.Services;

public class WikiPageTitleCache : IWikiPageTitleCache
{
    private readonly IGitRepositoryService _gitRepositoryService;
    private readonly WikiOptions _options;
    private readonly ConcurrentDictionary<string, string> _titleCache;

    public WikiPageTitleCache(
        IGitRepositoryService gitRepositoryService,
        IOptions<WikiOptions> options)
    {
        _gitRepositoryService = gitRepositoryService;
        _options = options.Value;
        _titleCache = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
    }

    public async Task<string?> GetPageTitleAsync(string pageName, string? culture, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(pageName, culture);

        if (_titleCache.TryGetValue(cacheKey, out var cachedTitle))
        {
            return cachedTitle;
        }

        var repository = GetRepository();
        var filePath = WikiFilePathHelper.GetFilePath(pageName, culture, _options.NeutralMarkdownPageCulture);

        try
        {
            var content = await repository.ReadFileAsync(filePath, _options.BranchName, cancellationToken);
            var contentText = Encoding.UTF8.GetString(content);
            var title = ExtractAndCacheTitle(pageName, culture, contentText);
            return title;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    public void ClearCache()
    {
        _titleCache.Clear();
    }

    public string ExtractAndCacheTitle(string pageName, string? culture, string content)
    {
        var title = MarkdownTitleExtractor.ExtractFirstTitle(content, pageName);
        var cacheKey = GetCacheKey(pageName, culture);

        _titleCache[cacheKey] = title;

        return title;
    }

    private string GetCacheKey(string pageName, string? culture)
    {
        return $"{pageName}:{culture ?? _options.NeutralMarkdownPageCulture}";
    }

    private IGitRepository GetRepository()
    {
        var repositoryPath = Path.Combine(_options.RepositoryRoot, _options.WikiRepositoryName);
        return _gitRepositoryService.GetRepository(repositoryPath);
    }
}
