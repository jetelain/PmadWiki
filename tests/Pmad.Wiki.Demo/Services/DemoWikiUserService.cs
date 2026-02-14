using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Pmad.Wiki.Demo.Entities;
using Pmad.Wiki.Helpers;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Demo.Services;

public class DemoWikiUserService : IWikiUserService
{
    private readonly DemoContext _demoContext;
    private readonly IAuthorizationService _authorizationService;

    public DemoWikiUserService(DemoContext demoContext, IAuthorizationService authorizationService)
    {
        _demoContext = demoContext;
        _authorizationService = authorizationService;
    }

    public async Task<IWikiUserWithPermissions?> GetWikiUser(ClaimsPrincipal principal, bool shouldCreate, CancellationToken cancellationToken)
    {
        var steamId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (steamId == null || !steamId.StartsWith("https://steamcommunity.com/openid/id/", StringComparison.Ordinal))
        {
            return null;
        }

        var user = await _demoContext.Users.FirstOrDefaultAsync(u => u.SteamId == steamId, cancellationToken);
        if (user == null)
        {
            if (!shouldCreate)
            {
                return null;
            }
            user = new DemoUser
            {
                SteamId = steamId,
                GitEmail = WikiUserHelper.GenerateUniqueGitEmail(),
                DisplayName = principal.FindFirstValue(ClaimTypes.Name) ?? "(no name)",
                GitName = WikiUserHelper.SanitizeGitNameOrEmail(principal.FindFirstValue(ClaimTypes.Name) ?? "(no name)")
            };
            _demoContext.Users.Add(user);
            await _demoContext.SaveChangesAsync(cancellationToken);
        }

        var isAdmin = (await _authorizationService.AuthorizeAsync(principal, "Admin")).Succeeded;

        return new DemoWikiUserWithPermissions(user, isAdmin);
    }

    public async Task<IWikiUser?> GetWikiUserFromGitEmail(string gitEmail, CancellationToken cancellationToken)
    {
        return await _demoContext.Users.FirstOrDefaultAsync(u => u.GitEmail == gitEmail, cancellationToken);
    }
}
