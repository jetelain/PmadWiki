using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pmad.Wiki.Helpers;
using Pmad.Wiki.Models;
using Pmad.Wiki.Resources;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Controllers
{
    public class WikiController : Controller
    {
        private const int CacheDurationSeconds = 14400; // 4 hours

        private readonly IWikiPageService _pageService;
        private readonly IWikiUserService _userService;
        private readonly IPageAccessControlService _accessControlService;
        private readonly IMarkdownRenderService _markdownRenderService;
        private readonly ITemporaryMediaStorageService _temporaryMediaStorage;
        private readonly IWikiPageEditService _wikiPageEditService;
        private readonly IWikiTemplateService _templateService;
        private readonly WikiOptions _options;
        private readonly ILogger<WikiController> _logger;
        private readonly IStringLocalizer<WikiResources> _localizer;
        private readonly IWikiPagePermissionHelper _pagePermissionHelper;

        public WikiController(
            IWikiPageService pageService,
            IWikiUserService userService,
            IPageAccessControlService accessControlService,
            IMarkdownRenderService markdownRenderService,
            ITemporaryMediaStorageService temporaryMediaStorage,
            IWikiPageEditService wikiPageEditService,
            IWikiTemplateService templateService,
            IOptions<WikiOptions> options,
            ILogger<WikiController> logger,
            IStringLocalizer<WikiResources> localizer,
            IWikiPagePermissionHelper pagePermissionHelper)
        {
            _pageService = pageService;
            _userService = userService;
            _accessControlService = accessControlService;
            _markdownRenderService = markdownRenderService;
            _temporaryMediaStorage = temporaryMediaStorage;
            _wikiPageEditService = wikiPageEditService;
            _templateService = templateService;
            _options = options.Value;
            _logger = logger;
            _localizer = localizer;
            _pagePermissionHelper = pagePermissionHelper;
        }

        [HttpGet]
        public async Task<IActionResult> View(string id, string? culture, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                id = _options.HomePageName;
            }

            if (!WikiInputValidator.IsValidPageName(id))
            {
                return BadRequest("Invalid page name.");
            }

            if (!string.IsNullOrEmpty(culture) && !WikiInputValidator.IsValidCulture(culture))
            {
                return BadRequest("Invalid culture identifier.");
            }

            if (!_options.AllowAnonymousViewing && !User.Identity?.IsAuthenticated == true)
            {
                return Challenge();
            }

            IWikiUserWithPermissions? wikiUser = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                wikiUser = await _userService.GetWikiUser(User, false, cancellationToken);
                if (wikiUser != null && !wikiUser.CanView && !_options.AllowAnonymousViewing)
                {
                    return Forbid();
                }
            }

            // Check page-level permissions
            if (_options.UsePageLevelPermissions)
            {
                var userGroups = wikiUser?.Groups ?? [];
                var pageAccess = await _accessControlService.CheckPageAccessAsync(id, userGroups, cancellationToken);
                if (!pageAccess.CanRead)
                {
                    if (User.Identity?.IsAuthenticated != true)
                    {
                        return Challenge();
                    }
                    return Forbid();
                }
            }

            var page = await _pageService.GetPageAsync(id, culture, cancellationToken);

            var canEdit = await _pagePermissionHelper.CanEdit(wikiUser, id, cancellationToken);

            if (page == null)
            {
                if (canEdit)
                {
                    return RedirectToAction(nameof(Edit), new { id, culture });
                }
                return NotFound();
            }

            var availableCultures = await _pageService.GetAvailableCulturesForPageAsync(id, cancellationToken);

            var viewModel = new WikiPageViewModel
            {
                PageName = id,
                HtmlContent = page.HtmlContent,
                Title = page.Title,
                CanEdit = canEdit,
                Culture = culture,
                AvailableCultures = availableCultures,
                LastModifiedBy = page.LastModifiedBy,
                LastModified = page.LastModified
            };

            await GenerateBreadcrumbAsync(id, culture, viewModel.Breadcrumb, cancellationToken);

            return View(viewModel);
        }

        private async Task GenerateBreadcrumbAsync(string id, string? culture, List<WikiPageLink> breadcrumb, CancellationToken cancellationToken)
        {
            var accumulatedPath = new StringBuilder();
            foreach (var part in id.Split('/'))
            {
                if (accumulatedPath.Length > 0)
                {
                    accumulatedPath.Append('/');
                }
                accumulatedPath.Append(part);

                var currentPath = accumulatedPath.ToString();
                var title = await _pageService.GetPageTitleAsync(currentPath, culture, cancellationToken);

                breadcrumb.Add(new WikiPageLink
                {
                    PageName = currentPath,
                    PageTitle = title ?? part
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> History(string id, string? culture, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Page name is required.");
            }

            if (!WikiInputValidator.IsValidPageName(id))
            {
                return BadRequest("Invalid page name.");
            }

            if (!string.IsNullOrEmpty(culture) && !WikiInputValidator.IsValidCulture(culture))
            {
                return BadRequest("Invalid culture identifier.");
            }

            if (!_options.AllowAnonymousViewing && !User.Identity?.IsAuthenticated == true)
            {
                return Challenge();
            }

            IWikiUserWithPermissions? wikiUser = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                wikiUser = await _userService.GetWikiUser(User, false, cancellationToken);
                if (wikiUser != null && !wikiUser.CanView && !_options.AllowAnonymousViewing)
                {
                    return Forbid();
                }
            }

            // Check page-level permissions
            if (_options.UsePageLevelPermissions)
            {
                var userGroups = wikiUser?.Groups ?? [];
                var pageAccess = await _accessControlService.CheckPageAccessAsync(id, userGroups, cancellationToken);
                if (!pageAccess.CanRead)
                {
                    if (User.Identity?.IsAuthenticated != true)
                    {
                        return Challenge();
                    }
                    return Forbid();
                }
            }

            var history = await _pageService.GetPageHistoryAsync(id, culture, cancellationToken);
            
            var viewModel = new WikiHistoryViewModel
            {
                PageName = id,
                Culture = culture,
                Entries = history.Select(h => new WikiHistoryEntry
                {
                    CommitId = h.CommitId,
                    Message = h.Message,
                    AuthorName = h.AuthorName,
                    Timestamp = h.Timestamp
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Revision(string id, string commitId, string? culture, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Page name is required.");
            }

            if (string.IsNullOrEmpty(commitId))
            {
                return BadRequest("Commit ID is required.");
            }

            if (!WikiInputValidator.IsValidPageName(id))
            {
                return BadRequest("Invalid page name.");
            }

            if (!string.IsNullOrEmpty(culture) && !WikiInputValidator.IsValidCulture(culture))
            {
                return BadRequest("Invalid culture identifier.");
            }

            if (!_options.AllowAnonymousViewing && !User.Identity?.IsAuthenticated == true)
            {
                return Challenge();
            }

            IWikiUserWithPermissions? wikiUser = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                wikiUser = await _userService.GetWikiUser(User, false, cancellationToken);
                if (wikiUser != null && !wikiUser.CanView && !_options.AllowAnonymousViewing)
                {
                    return Forbid();
                }
            }

            // Check page-level permissions
            if (_options.UsePageLevelPermissions)
            {
                var userGroups = wikiUser?.Groups ?? [];
                var pageAccess = await _accessControlService.CheckPageAccessAsync(id, userGroups, cancellationToken);
                if (!pageAccess.CanRead)
                {
                    if (User.Identity?.IsAuthenticated != true)
                    {
                        return Challenge();
                    }
                    return Forbid();
                }
            }

            var page = await _pageService.GetPageAtRevisionAsync(id, culture, commitId, cancellationToken);

            if (page == null)
            {
                return NotFound();
            }

            var history = await _pageService.GetPageHistoryAsync(id, culture, cancellationToken);
            var historyEntry = history.FirstOrDefault(h => h.CommitId == commitId);

            var viewModel = new WikiPageRevisionViewModel
            {
                PageName = id,
                HtmlContent = page.HtmlContent,
                Title = page.Title,
                Culture = culture,
                CommitId = commitId,
                AuthorName = historyEntry?.AuthorName ?? page.LastModifiedBy ?? "Unknown",
                Timestamp = historyEntry?.Timestamp ?? page.LastModified ?? DateTimeOffset.MinValue,
                Message = historyEntry?.Message ?? "",
                CanEdit = wikiUser?.CanEdit == true
            };

            await GenerateBreadcrumbAsync(id, culture, viewModel.Breadcrumb, cancellationToken);

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Diff(string id, string fromCommit, string toCommit, string? culture, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Page name is required.");
            }

            if (string.IsNullOrEmpty(fromCommit))
            {
                return BadRequest("From commit ID is required.");
            }

            if (string.IsNullOrEmpty(toCommit))
            {
                return BadRequest("To commit ID is required.");
            }

            if (!WikiInputValidator.IsValidPageName(id))
            {
                return BadRequest("Invalid page name.");
            }

            if (!string.IsNullOrEmpty(culture) && !WikiInputValidator.IsValidCulture(culture))
            {
                return BadRequest("Invalid culture identifier.");
            }

            if (!_options.AllowAnonymousViewing && !User.Identity?.IsAuthenticated == true)
            {
                return Challenge();
            }

            IWikiUserWithPermissions? wikiUser = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                wikiUser = await _userService.GetWikiUser(User, false, cancellationToken);
                if (wikiUser != null && !wikiUser.CanView && !_options.AllowAnonymousViewing)
                {
                    return Forbid();
                }
            }

            // Check page-level permissions
            if (_options.UsePageLevelPermissions)
            {
                var userGroups = wikiUser?.Groups ?? [];
                var pageAccess = await _accessControlService.CheckPageAccessAsync(id, userGroups, cancellationToken);
                if (!pageAccess.CanRead)
                {
                    if (User.Identity?.IsAuthenticated != true)
                    {
                        return Challenge();
                    }
                    return Forbid();
                }
            }

            var fromPage = await _pageService.GetPageAtRevisionAsync(id, culture, fromCommit, cancellationToken);
            var toPage = await _pageService.GetPageAtRevisionAsync(id, culture, toCommit, cancellationToken);

            if (fromPage == null || toPage == null)
            {
                return NotFound();
            }

            var history = await _pageService.GetPageHistoryAsync(id, culture, cancellationToken);
            var fromEntry = history.FirstOrDefault(h => h.CommitId == fromCommit);
            var toEntry = history.FirstOrDefault(h => h.CommitId == toCommit);

            var viewModel = new WikiPageDiffViewModel
            {
                PageName = id,
                Culture = culture,
                FromCommitId = fromCommit,
                ToCommitId = toCommit,
                FromAuthorName = fromEntry?.AuthorName ?? fromPage.LastModifiedBy ?? "Unknown",
                ToAuthorName = toEntry?.AuthorName ?? toPage.LastModifiedBy ?? "Unknown",
                FromTimestamp = fromEntry?.Timestamp ?? fromPage.LastModified ?? DateTimeOffset.MinValue,
                ToTimestamp = toEntry?.Timestamp ?? toPage.LastModified ?? DateTimeOffset.MinValue,
                FromMessage = fromEntry?.Message ?? "",
                ToMessage = toEntry?.Message ?? "",
                FromContent = fromPage.Content,
                ToContent = toPage.Content,
                CanEdit = wikiUser?.CanEdit == true
            };

            await GenerateBreadcrumbAsync(id, culture, viewModel.Breadcrumb, cancellationToken);

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> SiteMap(CancellationToken cancellationToken)
        {
            if (!_options.AllowAnonymousViewing && !User.Identity?.IsAuthenticated == true)
            {
                return Challenge();
            }

            IWikiUserWithPermissions? wikiUser = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                wikiUser = await _userService.GetWikiUser(User, false, cancellationToken);
                if (wikiUser != null && !wikiUser.CanView && !_options.AllowAnonymousViewing)
                {
                    return Forbid();
                }
            }

            var allPages = await _pagePermissionHelper.GetAllAccessiblePages(wikiUser, cancellationToken);

            var rootNodes = WikiSiteMapNodeHelper.Build(allPages, _options.NeutralMarkdownPageCulture);

            var viewModel = new WikiSiteMapViewModel
            {
                RootNodes = rootNodes,
                CanEdit = wikiUser?.CanEdit == true,
                CanAdmin = wikiUser?.CanAdmin == true,
                HomePageName = _options.HomePageName
            };

            return View(viewModel);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(string id, string? culture, string? restoreFromCommit, string? templateId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Page name is required.");
            }

            if (!WikiInputValidator.IsValidPageName(id))
            {
                return BadRequest("Invalid page name.");
            }

            if (!string.IsNullOrEmpty(culture) && !WikiInputValidator.IsValidCulture(culture))
            {
                return BadRequest("Invalid culture identifier.");
            }

            var wikiUser = await _userService.GetWikiUser(User, true, cancellationToken);
            if (!await _pagePermissionHelper.CanEdit(wikiUser, id, cancellationToken))
            {
                return Forbid();
            }

            WikiPage? page;
            string commitMessage;
            string content;

            if (!string.IsNullOrEmpty(restoreFromCommit))
            {
                page = await _pageService.GetPageAtRevisionAsync(id, culture, restoreFromCommit, cancellationToken);
                commitMessage = _localizer["Restore page {0} to revision {1}", id, restoreFromCommit?.Substring(0, Math.Min(8, restoreFromCommit.Length)) ?? string.Empty];
                content = page?.Content ?? string.Empty;
            }
            else
            {
                page = await _pageService.GetPageAsync(id, culture, cancellationToken);
                
                if (page == null)
                {
                    commitMessage = _localizer["Create page {0}", id];
                    
                    // Try to load content from template if specified
                    if (!string.IsNullOrEmpty(templateId))
                    {
                        var template = await _templateService.GetTemplateAsync(wikiUser, templateId, cancellationToken);
                        content = _templateService.ResolvePlaceHolders(template?.Content ?? string.Empty);
                    }
                    else
                    {
                        content = string.Empty;
                    }
                }
                else
                {
                    commitMessage = _localizer["Update page {0}", id];
                    content = page.Content;
                }
            }
            
            var viewModel = new WikiPageEditViewModel
            {
                PageName = id,
                Content = content,
                CommitMessage = commitMessage,
                Culture = culture,
                IsNew = page == null,
                OriginalContentHash = page?.ContentHash
            };

            await GenerateBreadcrumbAsync(id, culture, viewModel.Breadcrumb, cancellationToken);

            return View(viewModel);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAccessiblePages(string currentPageName, CancellationToken cancellationToken)
        {
            var wikiUser = await _userService.GetWikiUser(User, false, cancellationToken);
            if (wikiUser == null || !wikiUser.CanEdit)
            {
                return Forbid();
            }

            if (!WikiInputValidator.IsValidPageName(currentPageName))
            {
                return BadRequest("Invalid page name.");
            }

            var pages = (await _pagePermissionHelper.GetAllAccessiblePages(wikiUser, cancellationToken))
                .Select(p => new WikiPageLinkInfo
                {
                    PageName = p.PageName,
                    Title = p.Title,
                    RelativePath = WikiFilePathHelper.GetRelativePath(currentPageName, p.PageName)
                })
                .OrderBy(p => p.PageName)
                .ToList();

            return PartialView("_PageLinkList", pages);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewMarkdown([FromBody] PreviewMarkdownRequest request, CancellationToken cancellationToken)
        {
            var wikiUser = await _userService.GetWikiUser(User, false, cancellationToken);
            if (wikiUser == null || !wikiUser.CanEdit)
            {
                return Forbid();
            }

            if (string.IsNullOrEmpty(request?.Markdown))
            {
                return Content(string.Empty);
            }

            var html = _markdownRenderService.ToHtml(request.Markdown, request.Culture, request.PageName);
            return Content(html);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(WikiPageEditViewModel model, CancellationToken cancellationToken)
        {
            if (!WikiInputValidator.IsValidPageName(model.PageName))
            {
                ModelState.AddModelError(nameof(model.PageName), _localizer["Invalid page name."]);
            }

            if (!string.IsNullOrEmpty(model.Culture) && !WikiInputValidator.IsValidCulture(model.Culture))
            {
                ModelState.AddModelError(nameof(model.Culture), _localizer["Invalid culture identifier."]);
            }

            if (!ModelState.IsValid)
            {
                await GenerateBreadcrumbAsync(model.PageName, model.Culture, model.Breadcrumb, cancellationToken);
                return View(model);
            }

            var wikiUser = await _userService.GetWikiUser(User, true, cancellationToken);
            if (!await _pagePermissionHelper.CanEdit(wikiUser, model.PageName, cancellationToken))
            {
                return Forbid();
            }

            // Check if the page has been modified since the user started editing
            if (!model.IsNew && !string.IsNullOrEmpty(model.OriginalContentHash))
            {
                var currentPage = await _pageService.GetPageAsync(model.PageName, model.Culture, cancellationToken);
                if (currentPage != null)
                {
                    if (currentPage.ContentHash != model.OriginalContentHash)
                    {
                        ModelState.AddModelError(string.Empty,
                            _localizer["Warning: This page has been modified by {0} since you started editing. Your changes will overwrite those changes. Please review the current version before saving.", currentPage.LastModifiedBy ?? _localizer["another user"]]);
                        model.OriginalContentHash = currentPage.ContentHash;
                        await GenerateBreadcrumbAsync(model.PageName, model.Culture, model.Breadcrumb, cancellationToken);
                        return View(model);
                    }
                    if (currentPage.Content == model.Content)
                    {
                        // No-op if content is unchanged. Commit would fail due to identical content.
                        return RedirectToAction(nameof(View), new { id = model.PageName, culture = model.Culture });
                    }
                }
            }

            try
            {
                await _wikiPageEditService.SavePageAsync(
                    model.PageName,
                    model.Culture,
                    model.Content,
                    model.CommitMessage,
                    wikiUser.User,
                    cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // If the save operation was cancelled (e.g. due to a timeout), re-throw to let it propagate and be handled by middleware
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving page {PageName} (culture: {Culture}) by user {UserName}", 
                    model.PageName, model.Culture, wikiUser.User);
                ModelState.AddModelError(string.Empty, _localizer["An error occurred while saving the page. Please try again."]); 
                await GenerateBreadcrumbAsync(model.PageName, model.Culture, model.Breadcrumb, cancellationToken);
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.TemporaryMediaIds))
            {                
                // Cleanup temporary files
                var tempMediaIds = model.TemporaryMediaIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                await _temporaryMediaStorage.CleanupUserTemporaryMediaAsync(wikiUser.User, tempMediaIds, cancellationToken);
            }

            return RedirectToAction(nameof(View), new { id = model.PageName, culture = model.Culture });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadMedia(IFormFile file, CancellationToken cancellationToken)
        {
            var wikiUser = await _userService.GetWikiUser(User, false, cancellationToken);
            if (wikiUser == null || !wikiUser.CanEdit)
            {
                return Forbid();
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new UploadMediaErrorResponse { Error = _localizer["No file uploaded."] });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_options.AllowedMediaExtensions.Contains(extension))
            {
                return BadRequest(new UploadMediaErrorResponse { Error = _localizer["File type {0} is not allowed.", extension] });
            }

            // Check file size (limit to 10MB)
            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new UploadMediaErrorResponse { Error = _localizer["File size exceeds 10MB limit."] });
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);
            var fileContent = memoryStream.ToArray();

            var temporaryId = await _temporaryMediaStorage.StoreTemporaryMediaAsync(wikiUser.User, file.FileName, fileContent, cancellationToken);

            return Ok(new UploadMediaResponse
            { 
                TemporaryId = temporaryId,
                FileName = file.FileName,
                Url = Url.Action("TempMedia", "Wiki", new { id = temporaryId }) ?? string.Empty,
                Size = file.Length
            });
        }

        [HttpGet]
        [Authorize]
        [ResponseCache(Duration = CacheDurationSeconds, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> TempMedia(string id, CancellationToken cancellationToken)
        {
            if (!WikiInputValidator.IsValidTempMediaId(id))
            {
                return BadRequest("Invalid temporary media ID.");
            }

            var wikiUser = await _userService.GetWikiUser(User, false, cancellationToken);
            if (wikiUser == null || !wikiUser.CanEdit)
            {
                return Forbid();
            }

            var fileContent = await _temporaryMediaStorage.GetTemporaryMediaAsync(wikiUser.User, id, cancellationToken);
            if (fileContent == null)
            {
                return NotFound();
            }

            var tempMedia = await _temporaryMediaStorage.GetUserTemporaryMediaAsync(wikiUser.User, cancellationToken);
            if (tempMedia.TryGetValue(id, out var mediaInfo))
            {
                return File(fileContent, ContentTypeHelper.GetContentType(mediaInfo.OriginalFileName));
            }

            return File(fileContent, "application/octet-stream");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> AccessControl(CancellationToken cancellationToken)
        {
            var wikiUser = await _userService.GetWikiUser(User, false, cancellationToken);
            if (wikiUser == null || !wikiUser.CanAdmin)
            {
                return Forbid();
            }

            var rules = await _accessControlService.GetRulesAsync(cancellationToken);

            var viewModel = new WikiAccessControlViewModel
            {
                Rules = rules.Select(r => new WikiAccessControlRuleViewModel
                {
                    Pattern = r.Pattern,
                    ReadGroups = string.Join(", ", r.ReadGroups),
                    WriteGroups = string.Join(", ", r.WriteGroups),
                    Order = r.Order
                }).ToList(),
                IsEnabled = _options.UsePageLevelPermissions
            };

            return View(viewModel);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditAccessControl(CancellationToken cancellationToken)
        {
            var wikiUser = await _userService.GetWikiUser(User, false, cancellationToken);
            if (wikiUser == null || !wikiUser.CanAdmin)
            {
                return Forbid();
            }

            if (!_options.UsePageLevelPermissions)
            {
                return BadRequest("Page-level permissions are not enabled.");
            }

            var rules = await _accessControlService.GetRulesAsync(cancellationToken);
            var content = AccessControlRuleSerializer.SerializeRules(rules, includeExamples: true);

            var viewModel = new WikiAccessControlEditViewModel
            {
                Content = content
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAccessControl(WikiAccessControlEditViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var wikiUser = await _userService.GetWikiUser(User, true, cancellationToken);
            if (wikiUser == null || !wikiUser.CanAdmin)
            {
                return Forbid();
            }

            if (!_options.UsePageLevelPermissions)
            {
                return BadRequest("Page-level permissions are not enabled.");
            }

            try
            {
                var rules = AccessControlRuleSerializer.ParseRules(model.Content);
                await _accessControlService.SaveRulesAsync(rules, model.CommitMessage, wikiUser.User, cancellationToken);
                return RedirectToAction(nameof(AccessControl));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, _localizer["Error saving rules: {0}", ex.Message]);
                return View(model);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Create(string? fromPage, string? culture, CancellationToken cancellationToken)
        {
            var wikiUser = await _userService.GetWikiUser(User, false, cancellationToken);
            if (wikiUser == null || !wikiUser.CanEdit)
            {
                return Forbid();
            }

            var templates = await _templateService.GetAllTemplatesAsync(wikiUser, cancellationToken);

            var viewModel = new WikiCreateFromTemplateViewModel
            {
                Templates = templates,
                Culture = culture,
                FromPage = fromPage
            };

            return View(viewModel);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CreatePage(string? templateId, string? fromPage, string? culture, CancellationToken cancellationToken)
        {
            var wikiUser = await _userService.GetWikiUser(User, false, cancellationToken);
            if (wikiUser == null || !wikiUser.CanEdit)
            {
                return Forbid();
            }

            WikiTemplate? template = null;
            string? suggestedName = null;
            string? defaultLocation = null;

            if (!string.IsNullOrEmpty(templateId))
            {
                template = await _templateService.GetTemplateAsync(wikiUser, templateId, cancellationToken);
                if (template == null)
                {
                    return NotFound();
                }

                // Generate suggested name from pattern if available
                if (!string.IsNullOrEmpty(template.NamePattern))
                {
                    suggestedName = _templateService.ResolvePlaceHolders(template.NamePattern);
                }

                // Generate default location from template if available
                if (!string.IsNullOrEmpty(template.DefaultLocation))
                {
                    defaultLocation = _templateService.ResolvePlaceHolders(template.DefaultLocation);
                }
            }

            var viewModel = new WikiCreatePageViewModel
            {
                TemplateId = templateId,
                TemplateName = template?.DisplayName ?? template?.TemplateName,
                Culture = culture,
                FromPage = fromPage,
                Location = defaultLocation ?? WikiFilePathHelper.GetDirectoryName(fromPage),
                PageName = suggestedName ?? "NewPage"
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePageConfirm(WikiCreatePageViewModel model, CancellationToken cancellationToken)
        {
            // Build full page name
            var pageName = string.IsNullOrWhiteSpace(model.Location) 
                ? model.PageName 
                : $"{model.Location.Trim()}/{model.PageName.Trim()}";

            if (!WikiInputValidator.IsValidPageName(pageName))
            {
                ModelState.AddModelError(nameof(model.PageName), _localizer["Invalid page name."]);
            }

            if (!string.IsNullOrEmpty(model.Culture) && !WikiInputValidator.IsValidCulture(model.Culture))
            {
                ModelState.AddModelError(nameof(model.Culture), _localizer["Invalid culture identifier."]);
            }

            if (!ModelState.IsValid)
            {
                return View("CreatePage", model);
            }

            var wikiUser = await _userService.GetWikiUser(User, false, cancellationToken);

            if (!await _pagePermissionHelper.CanEdit(wikiUser, pageName, cancellationToken))
            {
                return Forbid();
            }

            // Check if page already exists
            var pageExists = await _pageService.PageExistsAsync(pageName, model.Culture, cancellationToken);
            if (pageExists)
            {
                ModelState.AddModelError(string.Empty, _localizer["A page with this name already exists."]);
                return View("CreatePage", model);
            }

            // Redirect to Edit with template if specified
            return RedirectToAction(nameof(Edit), new 
            { 
                id = pageName, 
                culture = model.Culture,
                templateId = model.TemplateId 
            });
        }

        [HttpGet]
        [ResponseCache(Duration = CacheDurationSeconds, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> Media(string id, CancellationToken cancellationToken)
        {
            if (!WikiInputValidator.IsValidMediaPath(id))
            {
                return BadRequest("Invalid media path.");
            }

            if (!_options.AllowedMediaExtensions.Any(ext => id.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest("Unsupported media file type.");
            }

            if (!_options.AllowAnonymousViewing && User.Identity?.IsAuthenticated != true)
            {
                return Challenge();
            }

            IWikiUserWithPermissions? wikiUser = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                wikiUser = await _userService.GetWikiUser(User, false, cancellationToken);
                if (wikiUser != null && !wikiUser.CanView && !_options.AllowAnonymousViewing)
                {
                    return Forbid();
                }
            }

            // For media files, check access based on the full media file path
            if (_options.UsePageLevelPermissions)
            {
                var userGroups = wikiUser?.Groups ?? [];
                var pageAccess = await _accessControlService.CheckPageAccessAsync(id, userGroups, cancellationToken);
                if (!pageAccess.CanRead)
                {
                    if (User.Identity?.IsAuthenticated != true)
                    {
                        return Challenge();
                    }
                    return Forbid();
                }
            }

            var fileContent = await _pageService.GetMediaFileAsync(id, cancellationToken);

            if (fileContent == null)
            {
                return NotFound();
            }

            return File(fileContent, ContentTypeHelper.GetContentType(id));
        }

    }
}
