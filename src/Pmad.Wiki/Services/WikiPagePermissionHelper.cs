using Microsoft.Extensions.Options;
using Pmad.Wiki.Models;

namespace Pmad.Wiki.Services;

public sealed class WikiPagePermissionHelper : IWikiPagePermissionHelper
{
    private readonly IWikiPageService _pageService;
    private readonly IPageAccessControlService _accessControlService;
    private readonly IWikiUserService _userService;
    private readonly WikiOptions _options;

    public WikiPagePermissionHelper(IWikiPageService pageService, IPageAccessControlService accessControlService, IWikiUserService userService, IOptions<WikiOptions> options)
    {
        _pageService = pageService;
        _accessControlService = accessControlService;
        _userService = userService;
        _options = options.Value;
    }

    public async ValueTask<bool> CanEdit(IWikiUserWithPermissions? wikiUser, string pageName, CancellationToken cancellationToken = default)
    {
        if (wikiUser == null || !wikiUser.CanEdit)
        {
            return false;
        }

        // Check page-level permissions
        if (_options.UsePageLevelPermissions)
        {
            var pageAccess = await _accessControlService.CheckPageAccessAsync(pageName, wikiUser.Groups, cancellationToken);
            if (!pageAccess.CanEdit)
            {
                return false;
            }
        }

        return true;
    }

    public async ValueTask<bool> CanView(IWikiUserWithPermissions? wikiUser, string pageName, CancellationToken cancellationToken = default)
    {
        if (!_options.AllowAnonymousViewing && (wikiUser == null || !wikiUser.CanView))
        {
            return false;
        }
        
        // Check page-level permissions
        if (_options.UsePageLevelPermissions)
        {
            var userGroups = wikiUser?.Groups ?? [];
            var pageAccess = await _accessControlService.CheckPageAccessAsync(pageName, userGroups, cancellationToken);
            if (!pageAccess.CanRead)
            {
                return false;
            }
        }

        return true;
    }

    public async Task<List<WikiPageInfo>> GetAccessibleSubPagesAsync(IWikiUserWithPermissions? wikiUser, string pageName, bool recursive = true, CancellationToken cancellationToken = default)
    {
        var subPages = await _pageService.GetSubPagesAsync(pageName, recursive, cancellationToken);

        return await FilterAccessiblePages(wikiUser, subPages, cancellationToken);
    }

    public async Task<List<WikiPageInfo>> GetAllAccessiblePagesAsync(IWikiUserWithPermissions? wikiUser, CancellationToken cancellationToken = default)
    {
        var allPages = await _pageService.GetAllPagesAsync(cancellationToken);

        return await FilterAccessiblePages(wikiUser, allPages, cancellationToken);
    }

    public async Task<WikiPagePermissionSummaryViewModel> GetPagePermissionSummaryAsync(string pageName, CancellationToken cancellationToken = default)
    {
        if (!_options.UsePageLevelPermissions)
        {
            return new WikiPagePermissionSummaryViewModel { IsEnabled = false };
        }

        var matchedRule = await _accessControlService.GetMatchingRuleAsync(pageName, cancellationToken);

        if (matchedRule == null)
        {
            return new WikiPagePermissionSummaryViewModel { IsEnabled = true };
        }

        var groups = (await _userService.GetAllWikiGroupsAsync(cancellationToken))
            .ToDictionary(g => g.Name, StringComparer.OrdinalIgnoreCase);

        return new WikiPagePermissionSummaryViewModel
        {
            IsEnabled = true,
            EffectiveRule = new WikiAccessControlRuleViewModel
            {
                Pattern = matchedRule.Pattern,
                ReadGroups = matchedRule.ReadGroups.Select(groups.ResolveGroup).ToList(),
                WriteGroups = matchedRule.WriteGroups.Select(groups.ResolveGroup).ToList(),
                Order = matchedRule.Order
            }
        };
    }

    private async ValueTask<List<WikiPageInfo>> FilterAccessiblePages(IWikiUserWithPermissions? wikiUser, List<WikiPageInfo> pages, CancellationToken cancellationToken)
    {
        if (!_options.UsePageLevelPermissions)
        {
            return pages;
        }

        // Filter pages based on page-level permissions
        var userGroups = wikiUser?.Groups ?? [];

        var filteredPages = new List<WikiPageInfo>();
        foreach (var page in pages)
        {
            var pageAccess = await _accessControlService.CheckPageAccessAsync(page.PageName, userGroups, cancellationToken);
            if (pageAccess.CanRead)
            {
                filteredPages.Add(page);
            }
        }
        return filteredPages;
    }
}
