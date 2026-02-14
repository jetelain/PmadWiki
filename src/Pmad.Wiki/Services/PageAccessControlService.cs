using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Pmad.Git.HttpServer;
using Pmad.Git.LocalRepositories;
using Pmad.Wiki.Helpers;

namespace Pmad.Wiki.Services;

/// <summary>
/// Service for managing page-level access control rules.
/// Rules are stored in a .wikipermissions file at the repository root and cached for performance.
/// </summary>
public class PageAccessControlService : IPageAccessControlService
{
    private const string PermissionsFileName = ".wikipermissions";
    private const string CacheKey = "WikiPageAccessRules";

    private readonly IGitRepositoryService _gitRepositoryService;
    private readonly WikiOptions _options;
    private readonly IMemoryCache _cache;

    public PageAccessControlService(
        IGitRepositoryService gitRepositoryService,
        IOptions<WikiOptions> options,
        IMemoryCache cache)
    {
        _gitRepositoryService = gitRepositoryService;
        _options = options.Value;
        _cache = cache;
    }

    public async Task<PageAccessPermissions> CheckPageAccessAsync(string pageName, string[] userGroups, CancellationToken cancellationToken = default)
    {
        if (!_options.UsePageLevelPermissions)
        {
            return new PageAccessPermissions { CanRead = true, CanEdit = true };
        }

        var rules = await GetRulesAsync(cancellationToken);

        // Sort rules by order (lower first)
        var sortedRules = rules.OrderBy(r => r.Order).ToList();

        // Find the first matching rule
        foreach (var rule in sortedRules)
        {
            if (rule.Matches(pageName))
            {
                var canRead = rule.ReadGroups.Length == 0 || rule.ReadGroups.Intersect(userGroups, StringComparer.OrdinalIgnoreCase).Any();
                var canEdit = rule.WriteGroups.Length == 0 || rule.WriteGroups.Intersect(userGroups, StringComparer.OrdinalIgnoreCase).Any();

                return new PageAccessPermissions
                {
                    CanRead = canRead,
                    CanEdit = canEdit,
                    MatchedPattern = rule.Pattern
                };
            }
        }

        // No rule matched - default to allow all
        return new PageAccessPermissions { CanRead = true, CanEdit = true };
    }

    public async Task<List<PageAccessRule>> GetRulesAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.UsePageLevelPermissions)
        {
            return new List<PageAccessRule>();
        }

        if (_cache.TryGetValue<List<PageAccessRule>>(CacheKey, out var cachedRules) && cachedRules != null)
        {
            return cachedRules;
        }

        var repository = GetRepository();

        try
        {
            var gitFile = await repository.ReadFileAsync(PermissionsFileName, _options.BranchName, cancellationToken);
            var content = Encoding.UTF8.GetString(gitFile);
            var rules = AccessControlRuleSerializer.ParseRules(content);
            _cache.Set(CacheKey, rules, TimeSpan.FromMinutes(15));
            return rules;
        }
        catch (FileNotFoundException)
        {
            // No permissions file exists - allow all by default
            var emptyRules = new List<PageAccessRule>();
            _cache.Set(CacheKey, emptyRules, TimeSpan.FromMinutes(15));
            return emptyRules;
        }
    }

    public async Task SaveRulesAsync(List<PageAccessRule> rules, string commitMessage, IWikiUser author, CancellationToken cancellationToken = default)
    {
        var repository = GetRepository();
        var content = AccessControlRuleSerializer.SerializeRules(rules);
        var contentBytes = Encoding.UTF8.GetBytes(content);

        var type = await repository.GetPathTypeAsync(PermissionsFileName, _options.BranchName, cancellationToken);

        GitCommitOperation operation = type == GitTreeEntryKind.Blob
            ? new UpdateFileOperation(PermissionsFileName, contentBytes)
            : new AddFileOperation(PermissionsFileName, contentBytes);

        var authorSignature = WikiUserHelper.CreateGitCommitSignature(author);
        var metadata = new GitCommitMetadata(commitMessage, authorSignature);

        await repository.CreateCommitAsync(_options.BranchName, new[] { operation }, metadata, cancellationToken);

        ClearCache();
    }

    public void ClearCache()
    {
        _cache.Remove(CacheKey);
    }

    private IGitRepository GetRepository()
    {
        var repositoryPath = Path.Combine(_options.RepositoryRoot, _options.WikiRepositoryName);
        return _gitRepositoryService.GetRepositoryByPath(repositoryPath);
    }
}
