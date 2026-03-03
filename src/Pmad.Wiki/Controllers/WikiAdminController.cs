using Microsoft.AspNetCore.Authorization;
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
    public class WikiAdminController : Controller
    {
        private readonly IWikiUserService _userService;
        private readonly IPageAccessControlService _accessControlService;
        private readonly WikiOptions _options;
        private readonly ILogger<WikiAdminController> _logger;
        private readonly IStringLocalizer<WikiResources> _localizer;

        public WikiAdminController(
            IWikiUserService userService,
            IPageAccessControlService accessControlService,
            IOptions<WikiOptions> options,
            ILogger<WikiAdminController> logger,
            IStringLocalizer<WikiResources> localizer)
        {
            _userService = userService;
            _accessControlService = accessControlService;
            _options = options.Value;
            _logger = logger;
            _localizer = localizer;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> AccessControl(CancellationToken cancellationToken)
        {
            var wikiUser = await _userService.GetWikiUserAsync(User, false, cancellationToken);
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
            var wikiUser = await _userService.GetWikiUserAsync(User, false, cancellationToken);
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

            var wikiUser = await _userService.GetWikiUserAsync(User, true, cancellationToken);
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
                _logger.LogError(ex, "Error saving access control rules");
                ModelState.AddModelError(string.Empty, _localizer["Error saving rules: {0}", ex.Message]);
                return View(model);
            }
        }
    }
}
