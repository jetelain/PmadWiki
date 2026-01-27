using System.Globalization;
using System.Text;
using Markdig;
using Microsoft.Extensions.Options;
using Pmad.Git.HttpServer;
using Pmad.Git.LocalRepositories;
using Pmad.Wiki.Helpers;

namespace Pmad.Wiki.Services;

public class WikiPageService : IWikiPageService
{
    private readonly IGitRepositoryService _gitRepositoryService;
    private readonly IWikiUserService _wikiUserService;
    private readonly IPageAccessControlService _pageAccessControlService;
    private readonly WikiOptions _options;
    private readonly MarkdownPipeline _markdownPipeline;

    public WikiPageService(
        IGitRepositoryService gitRepositoryService, 
        IWikiUserService wikiUserService, 
        IPageAccessControlService pageAccessControlService,
        IOptions<WikiOptions> options)
    {
        _wikiUserService = wikiUserService;
        _gitRepositoryService = gitRepositoryService;
        _pageAccessControlService = pageAccessControlService;
        _options = options.Value;
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Use(new WikiLinkExtension(_options.BasePath))
            .Build();
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
        var filePath = GetFilePath(pageName, culture);

        try
        {
            var gitFile = await repository.ReadFileAndHashAsync(filePath, _options.BranchName, cancellationToken);
            var contentText = Encoding.UTF8.GetString(gitFile.Content);
            var htmlContent = Markdig.Markdown.ToHtml(contentText, _markdownPipeline);
            
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
        var filePath = GetFilePath(pageName, culture);
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

    public async Task<bool> PageExistsAsync(string pageName, string? culture, CancellationToken cancellationToken = default)
    {
        var repository = GetRepository();
        var filePath = GetFilePath(pageName, culture);

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
        var baseFileName = GetBaseFileName(pageName);
        var directory = Path.GetDirectoryName(baseFileName) ?? string.Empty;

        try
        {
            var commit = await repository.GetCommitAsync(_options.BranchName, cancellationToken);
            
            await foreach (var item in repository.EnumerateCommitTreeAsync(_options.BranchName, string.IsNullOrEmpty(directory) ? null : directory, cancellationToken))
            {
                if (item.Entry.Kind == GitTreeEntryKind.Blob)
                {
                    var fileName = Path.GetFileName(item.Path);
                    if (IsLocalizedVersionOfPage(fileName, pageName, out var culture))
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
                    var (pageName, culture) = ParsePagePath(item.Path);
                    
                    var key = $"{pageName}:{culture ?? _options.NeutralMarkdownPageCulture}";
                    
                    if (!pages.ContainsKey(key))
                    {
                        GitCommit? lastCommit = null;
                        await foreach (var commit in repository.GetFileHistoryAsync(item.Path, _options.BranchName, cancellationToken))
                        {
                            lastCommit = commit;
                            break;
                        }

                        pages[key] = new WikiPageInfo
                        {
                            PageName = pageName,
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

    public async Task SavePageAsync(string pageName, string? culture, string content, string commitMessage, IWikiUser author, CancellationToken cancellationToken = default)
    {
        var repository = GetRepository();
        var filePath = GetFilePath(pageName, culture);
        var contentBytes = Encoding.UTF8.GetBytes(content);

        var type = await repository.GetPathTypeAsync(filePath, _options.BranchName, cancellationToken);

        if (type != null && type != GitTreeEntryKind.Blob)
        {
            throw new InvalidOperationException("Cannot save a page where a directory exists with the same name.");
        }

        GitCommitOperation operation = type == GitTreeEntryKind.Blob
            ? new UpdateFileOperation(filePath, contentBytes)
            : new AddFileOperation(filePath, contentBytes);

        var authorSignature = new GitCommitSignature(author.GitName, author.GitEmail, DateTimeOffset.UtcNow);
        var metadata = new GitCommitMetadata(commitMessage, authorSignature);

        await repository.CreateCommitAsync(_options.BranchName, new[] { operation }, metadata, cancellationToken);
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

    private string GetFilePath(string pageName, string? culture)
    {
        WikiInputValidator.ValidatePageName(pageName);
        
        if (!string.IsNullOrEmpty(culture) && culture != _options.NeutralMarkdownPageCulture)
        {
            WikiInputValidator.ValidateCulture(culture);
        }

        var baseFileName = GetBaseFileName(pageName);

        if (string.IsNullOrEmpty(culture) || culture == _options.NeutralMarkdownPageCulture)
        {
            return baseFileName + ".md";
        }

        var directory = Path.GetDirectoryName(baseFileName);
        var fileName = Path.GetFileNameWithoutExtension(baseFileName);
        var localizedFileName = $"{fileName}.{culture}.md";
        
        return string.IsNullOrEmpty(directory) 
            ? localizedFileName 
            : Path.Combine(directory, localizedFileName).Replace('\\', '/');
    }

    private string GetBaseFileName(string pageName)
    {
        return pageName.Replace('\\', '/').Trim('/');
    }

    private (string pageName, string? culture) ParsePagePath(string filePath)
    {
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        var directory = Path.GetDirectoryName(filePath)?.Replace('\\', '/');
        
        // Check if the file has a culture suffix
        var parts = fileNameWithoutExt.Split('.');
        if (parts.Length > 1 && IsValidCulture(parts[^1]))
        {
            var culture = parts[^1];
            var baseName = string.Join(".", parts.Take(parts.Length - 1));
            var pageName = string.IsNullOrEmpty(directory) ? baseName : $"{directory}/{baseName}";
            return (pageName, culture);
        }
        
        // No culture suffix
        var pageNameWithoutCulture = string.IsNullOrEmpty(directory) ? fileNameWithoutExt : $"{directory}/{fileNameWithoutExt}";
        return (pageNameWithoutCulture, null);
    }

    private bool IsLocalizedVersionOfPage(string fileName, string pageName, out string? culture)
    {
        culture = null;
        var basePageName = Path.GetFileNameWithoutExtension(GetBaseFileName(pageName));
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

        if (fileNameWithoutExt == basePageName)
        {
            culture = _options.NeutralMarkdownPageCulture;
            return true;
        }

        if (fileNameWithoutExt.StartsWith(basePageName + ".", StringComparison.OrdinalIgnoreCase))
        {
            var potentialCulture = fileNameWithoutExt.Substring(basePageName.Length + 1);
            if (IsValidCulture(potentialCulture))
            {
                culture = potentialCulture;
                return true;
            }
        }

        return false;
    }

    private bool IsValidCulture(string culture)
    {
        try
        {
            CultureInfo.GetCultureInfo(culture);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task<PageAccessPermissions> CheckPageAccessAsync(string pageName, string[] userGroups, CancellationToken cancellationToken = default)
    {
        return _pageAccessControlService.CheckPageAccessAsync(pageName, userGroups, cancellationToken);
    }
}
