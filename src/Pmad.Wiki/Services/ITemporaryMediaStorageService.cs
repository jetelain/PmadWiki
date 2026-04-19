namespace Pmad.Wiki.Services;

/// <summary>
/// Service for managing temporary media uploads during page editing.
/// Files are stored in a user-specific temporary location until the page is saved.
/// </summary>
public interface ITemporaryMediaStorageService
{
    /// <summary>
    /// Stores an uploaded media file in temporary storage for the user.
    /// </summary>
    /// <param name="user">The wiki user.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="fileContent">The file content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Temporary file id.</returns>
    Task<string> StoreTemporaryMediaAsync(IWikiUser user, string fileName, byte[] fileContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores an uploaded media file in temporary storage for the user.
    /// </summary>
    /// <param name="user">The wiki user.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="fileContent">The file content.</param>
    /// <param name="editableMediaInfo">Edit permission information of this file</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Temporary file id.</returns>
    Task<string> StoreEditableTemporaryMediaAsync(IWikiUser user, string fileName, byte[] fileContent, EditableMediaInfo editableMediaInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the content of an existing temporary media file for the user.
    /// </summary>
    /// <param name="user">The wiki user.</param>
    /// <param name="temporaryId">The temporary file identifier.</param>
    /// <param name="newContent">The file content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> UpdateEditableTemporaryMediaAsync(IWikiUser user, string temporaryId, byte[] newContent, CancellationToken cancellationToken = default);

    /// <summary>   
    /// Gets the temporary file content.
    /// </summary>
    /// <param name="user">The wiki user.</param>
    /// <param name="temporaryId">The temporary file identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file content or null if not found.</returns>
    Task<byte[]?> GetTemporaryMediaAsync(IWikiUser user, string temporaryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all temporary media files for a user.
    /// </summary>
    /// <param name="user">The wiki user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary of temporary IDs and their file information.</returns>
    Task<Dictionary<string, TemporaryMediaInfo>> GetUserTemporaryMediaAsync(IWikiUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans specified media files for a user.
    /// </summary>
    /// <param name="user">The wiki user.</param>
    /// <param name="temporaryIds">The temporary files identifiers to clean up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CleanupUserTemporaryMediaAsync(IWikiUser user, string[] temporaryIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old temporary files that haven't been accessed recently.
    /// </summary>
    /// <param name="olderThan">Files older than this will be removed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CleanupOldTemporaryMediaAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
}
