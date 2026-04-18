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

    /// <summary>
    /// Creates an editable version of an existing media file. If used in the owner page, file will be edited, otherwise a copy will be created and edited.
    /// The method returns a temporary file id that can be used to reference the editable media in the page content. After the page is saved, the temporary file will be moved to its final location if it was a copy, or the original file will be updated if it was edited directly.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="mediaPath">The path to the existing media file.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>Temporary file id.</returns>
    Task<string?> CreateEditableMediaFromExisting(IWikiUser user, string mediaPath, CancellationToken cancellationToken = default);
}
