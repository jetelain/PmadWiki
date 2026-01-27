using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Pmad.Git.HttpServer;

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

    public async ValueTask<bool> AuthorizeGitHttpAsync(HttpContext context, GitOperation operation, CancellationToken cancellationToken)
    {
        var user = await _wikiUserService.GetWikiUser(context.User, false, cancellationToken);
        if (user is null)
        {
            if (_wikiOptions.AllowAnonymousViewing && !_wikiOptions.UsePageLevelPermissions && operation == GitOperation.Read)
            {
                return true;
            }
            return false;
        }
        return user.CanRemoteGit;
    }
}
