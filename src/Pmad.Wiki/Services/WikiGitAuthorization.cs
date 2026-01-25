using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Pmad.Wiki.Services;

internal class WikiGitAuthorization : IWikiGitAuthorization
{
    private readonly IWikiUserService _wikiUserService;
    private readonly WikiOptions _wikiOptions;

    public WikiGitAuthorization(IWikiUserService wikiUserService, IOptions<WikiOptions> wikiOptions)
    {
        _wikiUserService = wikiUserService;
        _wikiOptions = wikiOptions.Value;
    }

    public async ValueTask<bool> AuthorizeGitHttpAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var user = await _wikiUserService.GetWikiUser(context.User, false, cancellationToken);
        if (user is null)
        {
            // TODO
            // if AllowAnonymousViewing is true and is UsePageLevelPermissions is false we could allow read only git access
            // but the git http server does not currently support that scenario

            return false;
        }
        return user.CanRemoteGit;
    }
}
