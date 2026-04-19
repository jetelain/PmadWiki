namespace Pmad.Wiki.Services;

/// <summary>Represents a media file to be committed to the wiki repository.</summary>
/// <param name="Content">The binary content of the media file.</param>
/// <param name="IsUpdate">
/// When <c>true</c>, the file already exists in the repository and should be updated;
/// when <c>false</c>, the file is new and should be added.
/// </param>
public record WikiMediaFile(byte[] Content, bool IsUpdate = false);
