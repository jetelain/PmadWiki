using Microsoft.Extensions.Options;

namespace Pmad.Wiki.Services;

public sealed class WikiPagePermissionHelper : IWikiPagePermissionHelper
{
    private readonly IWikiPageService _pageService;
    private readonly IPageAccessControlService _accessControlService;
    private readonly WikiOptions _options;

    public WikiPagePermissionHelper(IWikiPageService pageService, IPageAccessControlService accessControlService, IOptions<WikiOptions> options)
    {
        _pageService = pageService;
        _accessControlService = accessControlService;
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

    public async Task<List<WikiPageInfo>> GetAllAccessiblePages(IWikiUserWithPermissions? wikiUser, CancellationToken cancellationToken = default)
    {
        var allPages = await _pageService.GetAllPagesAsync(cancellationToken);

        if (!_options.UsePageLevelPermissions)
        {
            return allPages;
        }

        // Filter pages based on page-level permissions
        var userGroups = wikiUser?.Groups ?? [];

        var filteredPages = new List<WikiPageInfo>();
        foreach (var page in allPages)
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
