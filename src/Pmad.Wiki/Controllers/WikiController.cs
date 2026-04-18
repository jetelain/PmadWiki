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

            var wikiUser = await _userService.GetWikiUserAsync(User, false, cancellationToken);

            if (!await _pagePermissionHelper.CanView(wikiUser, id, cancellationToken))
            {
                if (User.Identity?.IsAuthenticated != true)
                {
                    return Challenge();
                }
                return Forbid();
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

            var slideShowTheme = SlideShowHelper.GetValidTheme(page.FrontMatter.SlideShowTheme, _options);

            var viewModel = new WikiPageViewModel
            {
                PageName = id,
                HtmlContent = page.FrontMatter.SlideShow
                    ? _markdownRenderService.ToHtmlSlideShow(page.ContentWithoutFrontMatter, culture, id)
                    : _markdownRenderService.ToHtml(page.ContentWithoutFrontMatter, culture, id),
                Title = page.Title,
                CanEdit = canEdit,
                Culture = culture,
                AvailableCultures = availableCultures,
                LastModifiedBy = page.LastModifiedBy,
                LastModified = page.LastModified,
                IsSlideShow = page.FrontMatter.SlideShow,
                SlideShowThemeUri = SlideShowHelper.GetThemeUri(slideShowTheme),
                SlideShowConfig = _options.SlideShowDefaultOptions,
                IsTemplate = WikiFilePathHelper.IsTemplatePageName(id)
            };

            if (page.FrontMatter.ShowSubPages)
            {
                var subPages = await _pagePermissionHelper.GetAccessibleSubPagesAsync(wikiUser, id, page.FrontMatter.SubPagesRecursive, cancellationToken);

                viewModel.SubPages = WikiSiteMapNodeHelper.BuildSubPages(subPages, culture ?? _options.NeutralMarkdownPageCulture, id);
            }

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

            var wikiUser = await _userService.GetWikiUserAsync(User, false, cancellationToken);

            if (!await _pagePermissionHelper.CanView(wikiUser, id, cancellationToken))
            {
                if (User.Identity?.IsAuthenticated != true)
                {
                    return Challenge();
                }
                return Forbid();
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

            if (wikiUser?.CanAdmin == true)
            {
                viewModel.PermissionSummary = await _pagePermissionHelper.GetPagePermissionSummaryAsync(id, cancellationToken);
            }

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

            var wikiUser = await _userService.GetWikiUserAsync(User, false, cancellationToken);

            if (!await _pagePermissionHelper.CanView(wikiUser, id, cancellationToken))
            {
                if (User.Identity?.IsAuthenticated != true)
                {
                    return Challenge();
                }
                return Forbid();
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
                HtmlContent = _markdownRenderService.ToHtml(page.ContentWithoutFrontMatter, culture, id),
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

            var wikiUser = await _userService.GetWikiUserAsync(User, false, cancellationToken);

            if (!await _pagePermissionHelper.CanView(wikiUser, id, cancellationToken))
            {
                if (User.Identity?.IsAuthenticated != true)
                {
                    return Challenge();
                }
                return Forbid();
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
                FromContent = fromPage.RawContent,
                ToContent = toPage.RawContent,
                CanEdit = wikiUser?.CanEdit == true
            };

            await GenerateBreadcrumbAsync(id, culture, viewModel.Breadcrumb, cancellationToken);

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> SiteMap(string? culture, CancellationToken cancellationToken)
        {
            if (!_options.AllowAnonymousViewing && !User.Identity?.IsAuthenticated == true)
            {
                return Challenge();
            }

            IWikiUserWithPermissions? wikiUser = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                wikiUser = await _userService.GetWikiUserAsync(User, false, cancellationToken);
                if (wikiUser != null && !wikiUser.CanView && !_options.AllowAnonymousViewing)
                {
                    return Forbid();
                }
            }

            var allPages = await _pagePermissionHelper.GetAllAccessiblePagesAsync(wikiUser, cancellationToken);

            var rootNodes = WikiSiteMapNodeHelper.Build(allPages, culture ?? _options.NeutralMarkdownPageCulture);

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
        public async Task<IActionResult> Edit(string id, string? culture, string? restoreFromCommit, string? templateId, DateTimeOffset? browserTimestamp,
            [ModelBinder(typeof(TemplateParametersModelBinder))] Dictionary<string, string>? templateParameters,
            CancellationToken cancellationToken)
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

            var wikiUser = await _userService.GetWikiUserAsync(User, true, cancellationToken);
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
                content = page?.RawContent ?? string.Empty;
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
                        var template = await _templateService.GetTemplateAsync(wikiUser!, templateId, cancellationToken);
                        content = _templateService.ResolvePlaceholders(template?.Content ?? string.Empty, templateParameters, browserTimestamp);
                    }
                    else
                    {
                        content = string.Empty;
                    }
                }
                else
                {
                    commitMessage = _localizer["Update page {0}", id];
                    content = page.RawContent;
                }
            }

            var viewModel = new WikiPageEditViewModel
            {
                PageName = id,
                Content = content,
                CommitMessage = commitMessage,
                Culture = culture,
                IsNew = page == null,
                OriginalContentHash = page?.ContentHash,
                SlideShowConfig = _options.SlideShowDefaultOptions
            };

            GenerateFrontMatterFields(id, viewModel);

            await GenerateBreadcrumbAsync(id, culture, viewModel.Breadcrumb, cancellationToken);

            return View(viewModel);
        }

        private void GenerateFrontMatterFields(string id, WikiPageEditViewModel viewModel)
        {
            if (WikiFilePathHelper.IsTemplatePageName(id))
            {
                viewModel.FrontMatterFields.AddRange([
                    new WikiFrontMatterField { Key = "title", Label = _localizer["Title"], HelpText = _localizer["Override the page title (leave empty to use the first H1 heading)."] },
                    new WikiFrontMatterField { Key = "description", Label = _localizer["Description"] },
                    new WikiFrontMatterField { Key = "location", Label = _localizer["Default Location"], HelpText = _localizer["Default location for pages created from this template. Supports placeholders: {date}, {year}, {month}, {day}, {datetime} and custom properties."] },
                    new WikiFrontMatterField { Key = "pattern", Label = _localizer["Name Pattern"], HelpText = _localizer["Name pattern for pages created from this template. Supports placeholders: {date}, {year}, {month}, {day}, {datetime} and custom properties."] }
                ]);
            }
            else
            {
                viewModel.FrontMatterFields.AddRange([
                    new WikiFrontMatterField { Key = "title", Label = _localizer["Title"], HelpText = _localizer["Override the page title (leave empty to use the first H1 heading)."] },
                    new WikiFrontMatterField { Key = "sortOrder", Label = _localizer["Sort Order"], Type = WikiFrontMatterFieldType.Number, HelpText = _localizer["Order of this page among its siblings in the site map. Lower values appear first. Defaults to 0."] },
                    new WikiFrontMatterField { Key = "slideShow", Label = _localizer["Slide Show"], Type = WikiFrontMatterFieldType.Checkbox, HelpText = _localizer["Display the page content as a reveal.js slide show. Use --- to separate slides."] },
                    new WikiFrontMatterField { Key = "slideShowTheme", Label = _localizer["Slide Show Theme"], Type = WikiFrontMatterFieldType.Select, Options = _options.SlideShowAllowedThemes, DependsOn = "slideShow" },
                    new WikiFrontMatterField { Key = "showSubPages", Label = _localizer["Show sub-pages"], Type = WikiFrontMatterFieldType.Checkbox, HelpText = _localizer["Display a list of direct sub-pages below the page content."] },
                    new WikiFrontMatterField { Key = "subPagesRecursive", Label = _localizer["Sub-pages recursive"], Type = WikiFrontMatterFieldType.Checkbox, HelpText = _localizer["Include all descendants recursively. Has no effect when Show sub-pages is disabled."], DependsOn = "showSubPages" }
                ]);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAccessiblePages(string currentPageName, CancellationToken cancellationToken)
        {
            var wikiUser = await _userService.GetWikiUserAsync(User, false, cancellationToken);
            if (wikiUser == null || !wikiUser.CanEdit)
            {
                return Forbid();
            }

            if (!WikiInputValidator.IsValidPageName(currentPageName))
            {
                return BadRequest("Invalid page name.");
            }

            var pages = (await _pagePermissionHelper.GetAllAccessiblePagesAsync(wikiUser, cancellationToken))
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

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMediaGallery(string currentPageName, CancellationToken cancellationToken)
        {
            var wikiUser = await _userService.GetWikiUserAsync(User, false, cancellationToken);
            if (wikiUser == null || !wikiUser.CanEdit)
            {
                return Forbid();
            }

            var mediaFiles = await _pageService.GetAllMediaFilesAsync(cancellationToken);

            var accessibleMedia = new List<Models.MediaGalleryItem>();
            foreach (var mediaFile in mediaFiles)
            {
                if (await _pagePermissionHelper.CanView(wikiUser, mediaFile.AbsolutePath, cancellationToken))
                {
                    accessibleMedia.Add(new Models.MediaGalleryItem
                    {
                        AbsolutePath = mediaFile.AbsolutePath,
                        FileName = mediaFile.FileName,
                        MediaType = mediaFile.MediaType,
                        Url = Url.Action("Media", "Wiki", new { id = mediaFile.AbsolutePath }) ?? string.Empty,
                        Path = WikiFilePathHelper.GetRelativePath(currentPageName, mediaFile.AbsolutePath)
                    });
                }
            }

            return PartialView("_MediaGalleryList", accessibleMedia);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewMarkdown([FromBody] PreviewMarkdownRequest request, CancellationToken cancellationToken)
        {
            var wikiUser = await _userService.GetWikiUserAsync(User, false, cancellationToken);
            if (wikiUser == null || !wikiUser.CanEdit)
            {
                return Forbid();
            }

            if (string.IsNullOrEmpty(request?.Markdown))
            {
                return Content(string.Empty);
            }

            var (frontMatter, _) = WikiFrontMatterParser.Parse<WikiPageFrontMatter>(request.Markdown);

            string html;
            if (frontMatter.SlideShow)
            {
                var theme = SlideShowHelper.GetValidTheme(frontMatter.SlideShowTheme, _options);
                html = _markdownRenderService.ToHtmlSlideShow(request.Markdown, request.Culture, request.PageName, theme);
            }
            else
            {
                html = _markdownRenderService.ToHtml(request.Markdown, request.Culture, request.PageName);
            }

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

            model.SlideShowConfig = _options.SlideShowDefaultOptions;

            GenerateFrontMatterFields(model.PageName, model);

            if (!ModelState.IsValid)
            {
                await GenerateBreadcrumbAsync(model.PageName, model.Culture, model.Breadcrumb, cancellationToken);
                return View(model);
            }

            var wikiUser = await _userService.GetWikiUserAsync(User, true, cancellationToken);
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
                    if (currentPage.RawContent == model.Content)
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
                    wikiUser!.User,
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
                    model.PageName, model.Culture, wikiUser!.User);
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
            var wikiUser = await _userService.GetWikiUserAsync(User, false, cancellationToken);
            if (wikiUser == null || !wikiUser.CanEdit)
            {
                return Forbid();
            }

            if (!IsValidUpload(file, out var message))
            {
                return BadRequest(new UploadMediaErrorResponse { Error = message });
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);
            var fileContent = memoryStream.ToArray();

            string temporaryId;

            if (WikiPathHelper.IsEditableMedia(file.FileName))
            {
                temporaryId = await _temporaryMediaStorage.StoreEditableTemporaryMediaAsync(wikiUser.User, file.FileName, fileContent, new EditableMediaInfo() { IsEditable = true }, cancellationToken);
            }
            else 
            {
                temporaryId = await _temporaryMediaStorage.StoreTemporaryMediaAsync(wikiUser.User, file.FileName, fileContent, cancellationToken);
            }

            return Ok(new UploadMediaResponse
            { 
                TemporaryId = temporaryId,
                FileName = file.FileName,
                Url = Url.Action("TempMedia", "Wiki", new { id = temporaryId }) ?? string.Empty,
                Size = file.Length,
                IsEditable = WikiPathHelper.IsEditableMedia(file.FileName)
            });
        }

        private bool IsValidUpload(IFormFile file, out string message)
        {
            if (file == null || file.Length == 0)
            {
                message = _localizer["No file uploaded."];
                return false;
            }

            if (!_options.IsMediaPathExtensionAllowed(file.FileName))
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                message = _localizer["File type {0} is not allowed.", extension];
                return false;
            }

            // Check file size (limit to 10MB)
            if (file.Length > 10 * 1024 * 1024)
            {
                message = _localizer["File size exceeds 10MB limit."];
                return false;
            }

            message = string.Empty;
            return true;
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMedia(string existingMediaPath, CancellationToken cancellationToken)
        {
            var wikiUser = await _userService.GetWikiUserAsync(User, false, cancellationToken);
            if (wikiUser == null || !wikiUser.CanEdit)
            {
                return Forbid();
            }

            if (!WikiInputValidator.IsValidMediaPath(existingMediaPath))
            {
                return BadRequest("Invalid existing media path.");
            }

            if (!_options.IsMediaPathExtensionAllowed(existingMediaPath) || !WikiInputValidator.IsValidEditableMedia(existingMediaPath))
            {
                return BadRequest("Unsupported media file type.");
            }

            if (!await _pagePermissionHelper.CanEdit(wikiUser, existingMediaPath, cancellationToken))
            {
                return Forbid();
            }

            var temporaryId = await _wikiPageEditService.CreateEditableMediaFromExisting(wikiUser!.User, existingMediaPath, cancellationToken);
            if (string.IsNullOrEmpty(temporaryId))
            {
                return NotFound();
            }

            return Ok(new EditMediaResponse
            {
                TemporaryId = temporaryId,
                Url = Url.Action("TempMedia", "Wiki", new { id = temporaryId }) ?? string.Empty,
            });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> TempMedia(string id, CancellationToken cancellationToken)
        {
            if (!WikiInputValidator.IsValidTempMediaId(id))
            {
                return BadRequest("Invalid temporary media ID.");
            }

            var wikiUser = await _userService.GetWikiUserAsync(User, false, cancellationToken);
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
                var contentType = ContentTypeHelper.GetContentType(mediaInfo.OriginalFileName);
                if (mediaInfo.EditableMediaInfo?.IsEditable == true)
                {
                    var etag = GetEtag(fileContent);
                    if (Request.Headers.IfNoneMatch == etag)
                    {
                        return StatusCode(StatusCodes.Status304NotModified);
                    }
                    Response.Headers.ETag = etag;
                    Response.Headers.CacheControl = "no-cache";
                    return File(fileContent, contentType);
                }
                Response.Headers.CacheControl = $"private, max-age={CacheDurationSeconds}";
                return File(fileContent, contentType);
            }

            Response.Headers.CacheControl = $"private, max-age={CacheDurationSeconds}";
            return File(fileContent, "application/octet-stream");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TempMedia(string id, IFormFile file, CancellationToken cancellationToken)
        {
            if (!WikiInputValidator.IsValidTempMediaId(id))
            {
                return BadRequest("Invalid temporary media ID.");
            }

            var wikiUser = await _userService.GetWikiUserAsync(User, false, cancellationToken);
            if (wikiUser == null || !wikiUser.CanEdit)
            {
                return Forbid();
            }

            if (!IsValidUpload(file, out var message))
            {
                return BadRequest(new UploadMediaErrorResponse { Error = message });
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);
            var fileContent = memoryStream.ToArray();

            if (await _temporaryMediaStorage.UpdateEditableTemporaryMediaAsync(wikiUser.User, id, fileContent, cancellationToken))
            {
                return Ok();
            }

            return NotFound();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Create(string? fromPage, string? culture, CancellationToken cancellationToken)
        {
            var wikiUser = await _userService.GetWikiUserAsync(User, false, cancellationToken);
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
        public async Task<IActionResult> CreatePage(string? templateId, string? fromPage, string? culture, DateTimeOffset? browserTimestamp, CancellationToken cancellationToken)
        {
            var wikiUser = await _userService.GetWikiUserAsync(User, false, cancellationToken);
            if (wikiUser == null || !wikiUser.CanEdit)
            {
                return Forbid();
            }

            WikiTemplate? template = null;
            string? suggestedName = null;
            string? defaultLocation = null;
            string? locationPattern = null;
            string? namePattern = null;
            var parameters = new List<WikiTemplateParameter>();
            var parameterValues = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(templateId))
            {
                template = await _templateService.GetTemplateAsync(wikiUser, templateId, cancellationToken);
                if (template == null)
                {
                    return NotFound();
                }

                parameters = template.Parameters;
                locationPattern = template.DefaultLocation;
                namePattern = template.NamePattern;

                // Initialize default parameter values
                foreach (var param in parameters)
                {
                    var defaultValue = param.DefaultValue ?? string.Empty;

                    // Resolve date placeholders in default values
                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        defaultValue = _templateService.ResolvePlaceholders(defaultValue, null, browserTimestamp);
                    }

                    parameterValues[param.Name] = defaultValue;
                }

                // Generate suggested name from pattern if available
                if (!string.IsNullOrEmpty(template.NamePattern))
                {
                    suggestedName = _templateService.ResolvePlaceholders(template.NamePattern, parameterValues, browserTimestamp);
                    suggestedName = WikiInputSanitizer.Sanitize(suggestedName);
                }

                // Generate default location from template if available
                if (!string.IsNullOrEmpty(template.DefaultLocation))
                {
                    defaultLocation = _templateService.ResolvePlaceholders(template.DefaultLocation, parameterValues, browserTimestamp);
                    defaultLocation = WikiInputSanitizer.SanitizeLocation(defaultLocation);
                }
            }

            var viewModel = new WikiCreatePageViewModel
            {
                TemplateId = templateId,
                TemplateName = template?.DisplayName ?? template?.TemplateName,
                Culture = culture,
                BrowserTimestamp = browserTimestamp?.ToString("O"),
                FromPage = fromPage,
                Location = defaultLocation ?? WikiFilePathHelper.GetDirectoryName(fromPage),
                PageName = suggestedName ?? _localizer["NewPage"],
                Parameters = parameters,
                ParameterValues = parameterValues,
                LocationPattern = locationPattern,
                PageNamePattern = namePattern,
                InvalidParameterNames = template?.InvalidParameterNames ?? new List<string>()
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

            var wikiUser = await _userService.GetWikiUserAsync(User, false, cancellationToken);

            if (!await _pagePermissionHelper.CanEdit(wikiUser, pageName, cancellationToken))
            {
                return Forbid();
            }

            WikiTemplate? template = null;
            if (!string.IsNullOrEmpty(model.TemplateId))
            {
                template = await _templateService.GetTemplateAsync(wikiUser!, model.TemplateId, cancellationToken);
                if (template == null)
                {
                    return NotFound();
                }
                model.InvalidParameterNames = template.InvalidParameterNames;
                model.Parameters = template.Parameters;
            }

            if (!ModelState.IsValid)
            {
                return View("CreatePage", model);
            }

            // Check if page already exists
            var pageExists = await _pageService.PageExistsAsync(pageName, model.Culture, cancellationToken);
            if (pageExists)
            {
                ModelState.AddModelError(string.Empty, _localizer["A page with this name already exists."]);
                return View("CreatePage", model);
            }

            // Build route values with template parameters as query parameters (prefixed with "p_")
            var routeValues = new Dictionary<string, object?>
            { 
                { "id", pageName }, 
                { "culture", model.Culture },
                { "templateId", model.TemplateId },
                { "browserTimestamp", model.BrowserTimestamp }
            };

            // Add parameter values as individual query parameters
            if (model.ParameterValues != null && template != null && template.Parameters != null)
            {
                foreach (var kvp in model.ParameterValues)
                {
                    if (!string.IsNullOrEmpty(kvp.Value) && template.Parameters.Any(p => p.Name == kvp.Key))
                    {
                        routeValues[$"{TemplateParametersModelBinder.ParameterPrefix}{kvp.Key}"] = kvp.Value;
                    }
                }
            }

            // Redirect to Edit with template if specified
            return RedirectToAction(nameof(Edit), routeValues);
        }

        [HttpGet]
        [Authorize]
        public IActionResult RelativeMedia(string pageName, string relativePath)
        {
            if (!WikiInputValidator.IsValidPageName(pageName))
            {
                return BadRequest("Invalid page name.");
            }
            if (!WikiInputValidator.MediaPathMarkdownRegex().IsMatch(relativePath)) // This endpoint is used to resolve media paths in markdown
            {
                return BadRequest("Invalid media path.");
            }
            if (!_options.IsMediaPathExtensionAllowed(relativePath))
            {
                return BadRequest("Unsupported media file type.");
            }
            return RedirectToAction(nameof(Media), new { id = WikiPathHelper.ResolveRelativePath(WikiPathHelper.GetDirectoryParts(pageName), relativePath) });
        }

        [HttpGet]
        public async Task<IActionResult> Media(string id, CancellationToken cancellationToken)
        {
            if (!WikiInputValidator.IsValidMediaPath(id))
            {
                return BadRequest("Invalid media path.");
            }

            if (!_options.IsMediaPathExtensionAllowed(id))
            {
                return BadRequest("Unsupported media file type.");
            }

            if (!_options.AllowAnonymousViewing && User.Identity?.IsAuthenticated != true)
            {
                return Challenge();
            }

            var wikiUser = await _userService.GetWikiUserAsync(User, false, cancellationToken);

            if (!await _pagePermissionHelper.CanView(wikiUser, id, cancellationToken))
            {
                if (User.Identity?.IsAuthenticated != true)
                {
                    return Challenge();
                }
                return Forbid();
            }

            var fileContent = await _pageService.GetMediaFileAsync(id, cancellationToken);

            if (fileContent == null)
            {
                return NotFound();
            }

            var contentType = ContentTypeHelper.GetContentType(id);

            if (WikiPathHelper.IsEditableMedia(id))
            {
                var etag = GetEtag(fileContent);
                if (Request.Headers.IfNoneMatch == etag)
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }
                Response.Headers.ETag = etag;
                Response.Headers.CacheControl = "no-cache";
                return File(fileContent, contentType);
            }

            Response.Headers.CacheControl = $"private, max-age={CacheDurationSeconds}";
            return File(fileContent, contentType);
        }

        private static string GetEtag(byte[] fileContent)
        {
            return $"\"{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(fileContent))[..16]}\"";
        }
    }
}
