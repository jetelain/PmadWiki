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
    /// <param name="userId">The user identifier.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="fileContent">The file content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A temporary URL that can be used in markdown during editing.</returns>
    Task<string> StoreTemporaryMediaAsync(IWikiUser userId, string fileName, byte[] fileContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the temporary file content.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="temporaryId">The temporary file identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file content or null if not found.</returns>
    Task<byte[]?> GetTemporaryMediaAsync(IWikiUser userId, string temporaryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all temporary media files for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary of temporary IDs and their file information.</returns>
    Task<Dictionary<string, TemporaryMediaInfo>> GetUserTemporaryMediaAsync(IWikiUser userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans specified media files for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="temporaryIds">The temporary files identifiers to clean up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CleanupUserTemporaryMediaAsync(IWikiUser userId, string[] temporaryIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old temporary files that haven't been accessed recently.
    /// </summary>
    /// <param name="olderThan">Files older than this will be removed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CleanupOldTemporaryMediaAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
}
