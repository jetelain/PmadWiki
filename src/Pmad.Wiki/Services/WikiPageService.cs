using System.Text;
using Microsoft.Extensions.Options;
using Pmad.Git.HttpServer;
using Pmad.Git.LocalRepositories;
using Pmad.Wiki.Helpers;

namespace Pmad.Wiki.Services;

public sealed class WikiPageService : IWikiPageService
{
    private readonly IGitRepositoryService _gitRepositoryService;
    private readonly IWikiUserService _wikiUserService;
    private readonly IWikiPageTitleCache _titleCache;
    private readonly WikiOptions _options;
    private readonly IMarkdownRenderService _markdownRenderService;

    public WikiPageService(
        IGitRepositoryService gitRepositoryService, 
        IWikiUserService wikiUserService, 
        IWikiPageTitleCache titleCache,
        IMarkdownRenderService markdownRenderService,
        IOptions<WikiOptions> options)
    {
        _wikiUserService = wikiUserService;
        _gitRepositoryService = gitRepositoryService;
        _titleCache = titleCache;
        _options = options.Value;
        _markdownRenderService = markdownRenderService;
    }

    public async Task EnsureRepositoryCreated()
    {
        var repositoryPath = GetRepositoryPath();
        if (Directory.Exists(repositoryPath))
        {
            return;
        }

        var repo = GitRepository.Init(repositoryPath, false, _options.BranchName);

        await repo.CreateCommitAsync(_options.BranchName, 
            [
                new AddFileOperation(_options.HomePageName + ".md", Encoding.UTF8.GetBytes("# Welcome to the Wiki!\n\nThis is the home page of your wiki. Feel free to edit this page and add new pages as needed.\n")),
            ], 
            new GitCommitMetadata("Initial commit", new GitCommitSignature("Wiki System", "wiki@pmadwiki.local", DateTimeOffset.UtcNow)), CancellationToken.None);
    }

    public async Task<WikiPage?> GetPageAsync(string pageName, string? culture, CancellationToken cancellationToken = default)
    {
        var repository = GetRepository();
        var filePath = WikiFilePathHelper.GetFilePath(pageName, culture, _options.NeutralMarkdownPageCulture);

        try
        {
            var gitFile = await repository.ReadFileAndHashAsync(filePath, _options.BranchName, cancellationToken);
            var contentText = Encoding.UTF8.GetString(gitFile.Content);
            var htmlContent = _markdownRenderService.ToHtml(contentText, culture, pageName);
            
            // Extract title and populate cache
            var title = _titleCache.ExtractAndCacheTitle(pageName, culture, contentText);

            GitCommit? lastCommit = null;
            await foreach (var commit in repository.GetFileHistoryAsync(filePath, _options.BranchName, cancellationToken))
            {
                lastCommit = commit;
                break;
            }

            return new WikiPage
            {
                PageName = pageName,
                Content = contentText,
                ContentHash = gitFile.Hash.Value,
                HtmlContent = htmlContent,
                Title = title,
                Culture = culture,
                LastModifiedBy = lastCommit?.Metadata.AuthorName,
                LastModified = lastCommit?.Metadata.AuthorDate
            };
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    public async Task<List<WikiHistoryItem>> GetPageHistoryAsync(string pageName, string? culture, CancellationToken cancellationToken = default)
    {
        var repository = GetRepository();
        var filePath = WikiFilePathHelper.GetFilePath(pageName, culture, _options.NeutralMarkdownPageCulture);
        var history = new List<WikiHistoryItem>();
        var userCache = new Dictionary<string, IWikiUser>();
        try
        {
            await foreach (var commit in repository.GetFileHistoryAsync(filePath, _options.BranchName, cancellationToken))
            {
                if (!userCache.TryGetValue(commit.Metadata.AuthorEmail, out var user))
                {
                    user = await _wikiUserService.GetWikiUserFromGitEmail(commit.Metadata.AuthorEmail, cancellationToken);
                    if (user != null)
                    {
                        userCache[commit.Metadata.AuthorEmail] = user;
                    }
                }

                history.Add(new WikiHistoryItem
                {
                    CommitId = commit.Id.Value,
                    Message = commit.Message,
                    AuthorName = user?.DisplayName ?? commit.Metadata.AuthorName,
                    Timestamp = commit.Metadata.AuthorDate
                });
            }
        }
        catch (FileNotFoundException)
        {
            // Page doesn't exist, return empty history
        }

        return history;
    }

    public async Task<WikiPage?> GetPageAtRevisionAsync(string pageName, string? culture, string commitId, CancellationToken cancellationToken = default)
    {
        var repository = GetRepository();
        var filePath = WikiFilePathHelper.GetFilePath(pageName, culture, _options.NeutralMarkdownPageCulture);

        try
        {
            var gitFile = await repository.ReadFileAndHashAsync(filePath, commitId, cancellationToken);
            var contentText = Encoding.UTF8.GetString(gitFile.Content);
            var htmlContent = _markdownRenderService.ToHtml(contentText, culture, pageName);
            
            var title = MarkdownTitleExtractor.ExtractFirstTitle(contentText, pageName);

            var commit = await repository.GetCommitAsync(commitId, cancellationToken);

            return new WikiPage
            {
                PageName = pageName,
                Content = contentText,
                ContentHash = gitFile.Hash.Value,
                HtmlContent = htmlContent,
                Title = title,
                Culture = culture,
                LastModifiedBy = commit.Metadata.AuthorName,
                LastModified = commit.Metadata.AuthorDate
            };
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    public async Task<bool> PageExistsAsync(string pageName, string? culture, CancellationToken cancellationToken = default)
    {
        var repository = GetRepository();
        var filePath = WikiFilePathHelper.GetFilePath(pageName, culture, _options.NeutralMarkdownPageCulture);

        try
        {
            await repository.ReadFileAsync(filePath, _options.BranchName, cancellationToken);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }

    public async Task<List<string>> GetAvailableCulturesForPageAsync(string pageName, CancellationToken cancellationToken = default)
    {
        var repository = GetRepository();
        var cultures = new List<string>();
        var baseFileName = WikiFilePathHelper.GetBaseFileName(pageName);
        var directory = Path.GetDirectoryName(baseFileName) ?? string.Empty;

        try
        {
            var commit = await repository.GetCommitAsync(_options.BranchName, cancellationToken);
            
            await foreach (var item in repository.EnumerateCommitTreeAsync(_options.BranchName, string.IsNullOrEmpty(directory) ? null : directory, cancellationToken))
            {
                if (item.Entry.Kind == GitTreeEntryKind.Blob)
                {
                    var fileName = Path.GetFileName(item.Path);
                    if (WikiFilePathHelper.IsLocalizedVersionOfPage(fileName, pageName, _options.NeutralMarkdownPageCulture, out var culture))
                    {
                        cultures.Add(culture ?? _options.NeutralMarkdownPageCulture);
                    }
                }
            }
        }
        catch (DirectoryNotFoundException)
        {
            // Directory doesn't exist
        }

        return cultures;
    }

    public async Task<List<WikiPageInfo>> GetAllPagesAsync(CancellationToken cancellationToken = default)
    {
        var repository = GetRepository();
        var pages = new Dictionary<string, WikiPageInfo>();

        try
        {
            await foreach (var item in repository.EnumerateCommitTreeAsync(_options.BranchName, null, cancellationToken))
            {
                if (item.Entry.Kind == GitTreeEntryKind.Blob && item.Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                {
                    var (pageName, culture) = WikiFilePathHelper.ParsePagePath(item.Path);
                    
                    var key = $"{pageName}:{culture ?? _options.NeutralMarkdownPageCulture}";
                    
                    if (!pages.ContainsKey(key))
                    {
                        GitCommit? lastCommit = null;
                        await foreach (var commit in repository.GetFileHistoryAsync(item.Path, _options.BranchName, cancellationToken))
                        {
                            lastCommit = commit;
                            break;
                        }

                        var title = await _titleCache.GetPageTitleAsync(pageName, culture, cancellationToken);

                        pages[key] = new WikiPageInfo
                        {
                            PageName = pageName,
                            Title = title,
                            Culture = culture,
                            LastModified = lastCommit?.Metadata.AuthorDate,
                            LastModifiedBy = lastCommit?.Metadata.AuthorName
                        };
                    }
                }
            }
        }
        catch (Exception)
        {
            // Repository might be empty or branch doesn't exist
        }

        return pages.Values.OrderBy(p => p.PageName).ToList();
    }

    public async Task SavePageWithMediaAsync(string pageName, string? culture, string content, string commitMessage, IWikiUser author, Dictionary<string, byte[]> mediaFiles, CancellationToken cancellationToken = default)
    {
        var repository = GetRepository();
        var filePath = WikiFilePathHelper.GetFilePath(pageName, culture, _options.NeutralMarkdownPageCulture);
        var contentBytes = Encoding.UTF8.GetBytes(content);

        var type = await repository.GetPathTypeAsync(filePath, _options.BranchName, cancellationToken);

        if (type != null && type != GitTreeEntryKind.Blob)
        {
            throw new InvalidOperationException("Cannot save a page where a directory exists with the same name.");
        }

        var operations = new List<GitCommitOperation>();

        // Add the main page operation
        GitCommitOperation pageOperation = type == GitTreeEntryKind.Blob
            ? new UpdateFileOperation(filePath, contentBytes)
            : new AddFileOperation(filePath, contentBytes);
        operations.Add(pageOperation);

        // Add media file operations
        foreach (var (mediaPath, mediaContent) in mediaFiles)
        {
            WikiInputValidator.ValidateMediaPath(mediaPath);
            operations.Add(new AddFileOperation(mediaPath, mediaContent));
        }

        var authorSignature = new GitCommitSignature(author.GitName, author.GitEmail, DateTimeOffset.UtcNow);
        var metadata = new GitCommitMetadata(commitMessage, authorSignature);

        await repository.CreateCommitAsync(_options.BranchName, operations, metadata, cancellationToken);

        // Update the title cache immediately with the new content
        _titleCache.ExtractAndCacheTitle(pageName, culture, content);
    }

    private IGitRepository GetRepository()
    {
        var repositoryPath = GetRepositoryPath();
        return _gitRepositoryService.GetRepository(repositoryPath);
    }

    private string GetRepositoryPath()
    {
        return Path.Combine(_options.RepositoryRoot, _options.WikiRepositoryName);
    }

    public Task<string?> GetPageTitleAsync(string pageName, string? culture, CancellationToken cancellationToken = default)
    {
        return _titleCache.GetPageTitleAsync(pageName, culture, cancellationToken);
    }

    public async Task<byte[]?> GetMediaFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        WikiInputValidator.ValidateMediaPath(filePath);

        var repository = GetRepository();

        try
        {
            return await repository.ReadFileAsync(filePath, _options.BranchName, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }
}
