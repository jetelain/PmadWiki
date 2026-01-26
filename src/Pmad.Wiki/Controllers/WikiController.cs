using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Pmad.Wiki.Helpers;
using Pmad.Wiki.Models;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Controllers
{
    public class WikiController : Controller
    {
        private readonly IWikiPageService _pageService;
        private readonly IWikiUserService _userService;
        private readonly WikiOptions _options;

        public WikiController(
            IWikiPageService pageService,
            IWikiUserService userService,
            IOptions<WikiOptions> options)
        {
            _pageService = pageService;
            _userService = userService;
            _options = options.Value;
        }

        [HttpGet]
        public async Task<IActionResult> View(string id, string? culture, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                id = "Home";
            }

            if (!WikiInputValidator.IsValidPageName(id, out var pageNameError))
            {
                return BadRequest(pageNameError);
            }

            if (!string.IsNullOrEmpty(culture) && !WikiInputValidator.IsValidCulture(culture, out var cultureError))
            {
                return BadRequest(cultureError);
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

            var page = await _pageService.GetPageAsync(id, culture, cancellationToken);
            
            if (page == null)
            {
                if (wikiUser?.CanEdit == true)
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
                Title = id,
                CanEdit = wikiUser?.CanEdit == true,
                Culture = culture,
                AvailableCultures = availableCultures,
                LastModifiedBy = page.LastModifiedBy,
                LastModified = page.LastModified
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> History(string id, string? culture, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Page name is required.");
            }

            if (!WikiInputValidator.IsValidPageName(id, out var pageNameError))
            {
                return BadRequest(pageNameError);
            }

            if (!string.IsNullOrEmpty(culture) && !WikiInputValidator.IsValidCulture(culture, out var cultureError))
            {
                return BadRequest(cultureError);
            }

            if (!_options.AllowAnonymousViewing && !User.Identity?.IsAuthenticated == true)
            {
                return Challenge();
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                var wikiUser = await _userService.GetWikiUser(User, false, cancellationToken);
                if (wikiUser != null && !wikiUser.CanView && !_options.AllowAnonymousViewing)
                {
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
        [Authorize]
        public async Task<IActionResult> Edit(string id, string? culture, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Page name is required.");
            }

            if (!WikiInputValidator.IsValidPageName(id, out var pageNameError))
            {
                return BadRequest(pageNameError);
            }

            if (!string.IsNullOrEmpty(culture) && !WikiInputValidator.IsValidCulture(culture, out var cultureError))
            {
                return BadRequest(cultureError);
            }

            var wikiUser = await _userService.GetWikiUser(User, true, cancellationToken);
            if (wikiUser == null || !wikiUser.CanEdit)
            {
                return Forbid();
            }

            var page = await _pageService.GetPageAsync(id, culture, cancellationToken);
            
            var viewModel = new WikiPageEditViewModel
            {
                PageName = id,
                Content = page?.Content ?? string.Empty,
                CommitMessage = page == null ? $"Create page {id}" : $"Update page {id}",
                Culture = culture,
                IsNew = page == null,
                OriginalContentHash = page?.ContentHash
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(WikiPageEditViewModel model, CancellationToken cancellationToken)
        {
            if (!WikiInputValidator.IsValidPageName(model.PageName, out var pageNameError))
            {
                ModelState.AddModelError(nameof(model.PageName), pageNameError);
            }

            if (!string.IsNullOrEmpty(model.Culture) && !WikiInputValidator.IsValidCulture(model.Culture, out var cultureError))
            {
                ModelState.AddModelError(nameof(model.Culture), cultureError);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var wikiUser = await _userService.GetWikiUser(User, true, cancellationToken);
            if (wikiUser == null || !wikiUser.CanEdit)
            {
                return Forbid();
            }

            // Check if the page has been modified since the user started editing
            if (!model.IsNew && !string.IsNullOrEmpty(model.OriginalContentHash))
            {
                var currentPage = await _pageService.GetPageAsync(model.PageName, model.Culture, cancellationToken);
                if (currentPage != null && currentPage.ContentHash != model.OriginalContentHash)
                {
                    ModelState.AddModelError(string.Empty, 
                        $"Warning: This page has been modified by {currentPage.LastModifiedBy ?? "another user"} since you started editing. " +
                        "Your changes will overwrite those changes. Please review the current version before saving.");
                    model.OriginalContentHash = currentPage.ContentHash;
                    return View(model);
                }
            }

            await _pageService.SavePageAsync(
                model.PageName,
                model.Culture,
                model.Content,
                model.CommitMessage,
                wikiUser.User,
                cancellationToken);

            return RedirectToAction(nameof(View), new { id = model.PageName, culture = model.Culture });
        }
    }
}
