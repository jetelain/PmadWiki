using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;
using Pmad.Wiki.Helpers;

namespace Pmad.Wiki.Services;

internal class WikiPageEditService : IWikiPageEditService
{
    private readonly IWikiPageService _pageService;
    private readonly ITemporaryMediaStorageService _temporaryMediaStorage;
    private readonly LinkGenerator _linkGenerator;

    private const string IdPlaceholder = "xxxxxxxxxxxx";

    public WikiPageEditService(IWikiPageService pageService, ITemporaryMediaStorageService temporaryMediaStorage, LinkGenerator linkGenerator)
    {
        _pageService = pageService;
        _temporaryMediaStorage = temporaryMediaStorage;
        _linkGenerator = linkGenerator;
    }

    public async Task SavePageAsync(string pageName, string? culture, string content, string commitMessage, IWikiUser author, CancellationToken cancellationToken = default)
    {
        var mediaFiles = new Dictionary<string, byte[]>();
        var updatedContent = content;

        var wikiBaseUrl = _linkGenerator.GetPathByAction("TempMedia", "Wiki", new { id = IdPlaceholder })!;

        var usedTempIdRegex = new Regex(wikiBaseUrl.Replace(IdPlaceholder, "([a-f0-9]+)"));

        var usedTempIds = usedTempIdRegex.Matches(updatedContent)
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();

        if (usedTempIds.Count > 0)
        {
            var tempMedia = await _temporaryMediaStorage.GetUserTemporaryMediaAsync(author, cancellationToken);

            var pageDirectory = WikiFilePathHelper.GetDirectoryName(pageName);

            foreach (var tempId in usedTempIds)
            {
                if (tempMedia.TryGetValue(tempId, out var mediaInfo))
                {
                    var mediaContent = await _temporaryMediaStorage.GetTemporaryMediaAsync(author, tempId, cancellationToken);
                    if (mediaContent != null)
                    {
                        var extension = Path.GetExtension(mediaInfo.OriginalFileName);
                        var markdownRelativePath = $"medias/{tempId}{extension}";
                        var mediaPath =  string.IsNullOrEmpty(pageDirectory) ? markdownRelativePath : $"{pageDirectory}/{markdownRelativePath}";

                        mediaFiles[mediaPath] = mediaContent;

                        var tempMediaUrl = wikiBaseUrl.Replace(IdPlaceholder, tempId);

                        // Update content to replace temporary URLs with permanent relative paths
                        updatedContent = updatedContent.Replace(tempMediaUrl, markdownRelativePath);
                    }
                }
            }
        }

        await _pageService.SavePageWithMediaAsync(
            pageName,
            culture,
            updatedContent,
            commitMessage,
            author,
            mediaFiles,
            cancellationToken);
    }
}
