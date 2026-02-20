using Microsoft.AspNetCore.StaticFiles;

namespace Pmad.Wiki.Helpers;

internal static class ContentTypeHelper
{
    private static readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    internal static string GetContentType(string path)
    {
        if (!_contentTypeProvider.TryGetContentType(path, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return contentType;
    }

    internal static Models.MediaType GetMediaType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".svg" or ".webp" => Models.MediaType.Image,
            ".mp4" or ".webm" or ".ogg" => Models.MediaType.Video,
            ".pdf" => Models.MediaType.Document,
            _ => Models.MediaType.File
        };
    }
}
