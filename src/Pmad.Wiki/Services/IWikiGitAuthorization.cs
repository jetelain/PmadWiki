using Microsoft.AspNetCore.Http;
using Pmad.Git.HttpServer;

namespace Pmad.Wiki.Services;

internal interface IWikiGitAuthorization
{
    ValueTask<bool> AuthorizeGitHttpAsync(HttpContext context, GitOperation operation, CancellationToken cancellationToken);
}