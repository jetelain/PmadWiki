using Microsoft.AspNetCore.Http;

namespace Pmad.Wiki.Services;

internal interface IWikiGitAuthorization
{
    ValueTask<bool> AuthorizeGitHttpAsync(HttpContext context, CancellationToken cancellationToken);
}