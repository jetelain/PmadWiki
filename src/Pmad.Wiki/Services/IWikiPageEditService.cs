namespace Pmad.Wiki.Services;

/// <summary>
/// Defines the contract for saving wiki page content.
/// </summary>
public interface IWikiPageEditService
{
    /// <summary>
    /// Saves the content of a wiki page as a new Git commit.
    /// </summary>
    /// <param name="pageName">The name of the page to save.</param>
    /// <param name="culture">The optional culture code. Pass <c>null</c> for the neutral culture.</param>
    /// <param name="content">The raw Markdown content of the page.</param>
    /// <param name="commitMessage">The Git commit message describing the change.</param>
    /// <param name="author">The wiki user performing the save.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    Task SavePageAsync(string pageName, string? culture, string content, string commitMessage, IWikiUser author, CancellationToken cancellationToken = default);
}
