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
}
