using Pmad.Wiki.Helpers;
using Pmad.Wiki.Models;

namespace Pmad.Wiki.Services;

public sealed class WikiTemplateService : IWikiTemplateService
{
    private readonly IWikiPagePermissionHelper _pagePermissionHelper;
    private readonly IWikiPageService _pageService;

    public WikiTemplateService(
        IWikiPageService pageService,
        IWikiPagePermissionHelper pagePermissionHelper)
    {
        _pagePermissionHelper = pagePermissionHelper;
        _pageService = pageService;
    }

    public async Task<List<WikiTemplate>> GetAllTemplatesAsync(IWikiUserWithPermissions wikiUser, CancellationToken cancellationToken = default)
    {
        var templates = new List<WikiTemplate>();

        // Get all pages from the wiki
        var allPages = await _pagePermissionHelper.GetAllAccessiblePages(wikiUser, cancellationToken);

        // Filter pages that are templates
        // Templates are stored in _templates/ directory or named _template
        // Get distinct template pages by PageName to avoid duplicate loads for culture variants
        var templatePages = allPages
            .Where(p => WikiFilePathHelper.IsTemplatePageName(p.PageName))
            .GroupBy(p => p.PageName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First());

        foreach (var templatePage in templatePages)
        {
            var template = await LoadTemplateFromPageAsync(templatePage.PageName, cancellationToken);
            if (template != null)
            {
                templates.Add(template);
            }
        }

        // Sort templates by display name or template name
        return templates.OrderBy(t => t.DisplayName ?? t.TemplateName).ToList();
    }

    public async Task<WikiTemplate?> GetTemplateAsync(IWikiUserWithPermissions wikiUser, string templateId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(templateId))
        {
            return null;
        }

        WikiInputValidator.ValidatePageName(templateId);

        if (!WikiFilePathHelper.IsTemplatePageName(templateId))
        {
            throw new ArgumentException("Invalid template ID.", nameof(templateId));
        }

        if (!await _pagePermissionHelper.CanView(wikiUser, templateId, cancellationToken))
        {
            // User does not have permission to view this template, return null to avoid exposing its existence
            return null;
        }

        return await LoadTemplateFromPageAsync(templateId, cancellationToken);
    }

    public string ResolvePlaceHolders(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return string.Empty;
        }

        var now = DateTimeOffset.UtcNow;
        var result = pattern;

        // Replace {date} with current date in ISO format
        result = result.Replace("{date}", now.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase);
        
        // Replace {datetime} with current date and time
        result = result.Replace("{datetime}", now.ToString("yyyy-MM-dd-HHmmss"), StringComparison.OrdinalIgnoreCase);
        
        // Replace {year}, {month}, {day}
        result = result.Replace("{year}", now.Year.ToString(), StringComparison.OrdinalIgnoreCase);
        result = result.Replace("{month}", now.Month.ToString("D2"), StringComparison.OrdinalIgnoreCase);
        result = result.Replace("{day}", now.Day.ToString("D2"), StringComparison.OrdinalIgnoreCase);

        return result;
    }

    private async Task<WikiTemplate?> LoadTemplateFromPageAsync(string pageName, CancellationToken cancellationToken)
    {
        // Load the page content (templates have no culture)
        var page = await _pageService.GetPageAsync(pageName, null, cancellationToken);
        if (page == null)
        {
            return null;
        }

        // Parse front matter from content
        var (frontMatter, content) = WikiTemplateFrontMatterParser.Parse(page.Content);

        // Get display name from front matter, or fallback to the page title
        var displayName = frontMatter.Title;
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = await _pageService.GetPageTitleAsync(pageName, null, cancellationToken);
        }

        return new WikiTemplate
        {
            TemplateName = pageName, // Store the full page name for retrieval
            Content = content,
            DefaultLocation = frontMatter.Location,
            NamePattern = frontMatter.Pattern,
            Description = frontMatter.Description,
            DisplayName = displayName ?? pageName // Use page name as final fallback
        };
    }
}
