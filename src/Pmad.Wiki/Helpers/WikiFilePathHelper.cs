using System.Globalization;

namespace Pmad.Wiki.Helpers;

public static class WikiFilePathHelper
{
    public static string GetDirectoryName(string pageName)
    {
        var lastSlashIndex = pageName.LastIndexOf('/');

        return lastSlashIndex >= 0 ? pageName[..lastSlashIndex] : string.Empty;
    }

    public static string GetFilePath(string pageName, string? culture, string neutralCulture)
    {
        WikiInputValidator.ValidatePageName(pageName);
        
        if (!string.IsNullOrEmpty(culture) && culture != neutralCulture)
        {
            WikiInputValidator.ValidateCulture(culture);
        }

        var baseFileName = GetBaseFileName(pageName);

        if (string.IsNullOrEmpty(culture) || culture == neutralCulture)
        {
            return baseFileName + ".md";
        }

        var directory = Path.GetDirectoryName(baseFileName);
        var fileName = Path.GetFileNameWithoutExtension(baseFileName);
        var localizedFileName = $"{fileName}.{culture}.md";
        
        return string.IsNullOrEmpty(directory) 
            ? localizedFileName 
            : Path.Combine(directory, localizedFileName).Replace('\\', '/');
    }

    public static string GetBaseFileName(string pageName)
    {
        return pageName.Replace('\\', '/').Trim('/');
    }

    public static (string pageName, string? culture) ParsePagePath(string filePath)
    {
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        var directory = Path.GetDirectoryName(filePath)?.Replace('\\', '/');
        
        // Check if the file has a culture suffix
        var parts = fileNameWithoutExt.Split('.');
        if (parts.Length > 1 && IsValidCulture(parts[^1]))
        {
            var culture = parts[^1];
            var baseName = string.Join(".", parts.Take(parts.Length - 1));
            var pageName = string.IsNullOrEmpty(directory) ? baseName : $"{directory}/{baseName}";
            return (pageName, culture);
        }
        
        // No culture suffix
        var pageNameWithoutCulture = string.IsNullOrEmpty(directory) ? fileNameWithoutExt : $"{directory}/{fileNameWithoutExt}";
        return (pageNameWithoutCulture, null);
    }

    public static bool IsLocalizedVersionOfPage(string fileName, string pageName, string neutralCulture, out string? culture)
    {
        culture = null;
        var basePageName = Path.GetFileNameWithoutExtension(GetBaseFileName(pageName));
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

        if (fileNameWithoutExt == basePageName)
        {
            culture = neutralCulture;
            return true;
        }

        if (fileNameWithoutExt.StartsWith(basePageName + ".", StringComparison.OrdinalIgnoreCase))
        {
            var potentialCulture = fileNameWithoutExt.Substring(basePageName.Length + 1);
            if (IsValidCulture(potentialCulture))
            {
                culture = potentialCulture;
                return true;
            }
        }

        return false;
    }

    public static string GetRelativePath(string fromPage, string toPage)
    {
        var fromParts = fromPage.Split('/');
        var toParts = toPage.Split('/');
        
        // Find common prefix length (only considering directory parts, not the page itself)
        int commonPrefixLength = 0;
        int maxCommonLength = Math.Min(fromParts.Length - 1, toParts.Length - 1);
        while (commonPrefixLength < maxCommonLength &&
               fromParts[commonPrefixLength] == toParts[commonPrefixLength])
        {
            commonPrefixLength++;
        }
        
        // Calculate how many levels to go up from the current page
        // We always go up one level from the current page, then add more for each directory level
        int levelsUp = fromParts.Length - 1 - commonPrefixLength;
        
        // Build the relative path
        var relativeParts = new List<string>();
        
        // Add "../" for each level up
        for (int i = 0; i < levelsUp; i++)
        {
            relativeParts.Add("..");
        }
        
        // Add the remaining parts of the target path
        for (int i = commonPrefixLength; i < toParts.Length; i++)
        {
            relativeParts.Add(toParts[i]);
        }
        
        // If we're linking to a page in the same directory, just use the page name
        if (relativeParts.Count == 0)
        {
            return toParts[^1];
        }
        
        return string.Join("/", relativeParts);
    }

    private static bool IsValidCulture(string culture)
    {
        if (!WikiInputValidator.IsValidCulture(culture))
        {
            return false;
        }
        try
        {
            CultureInfo.GetCultureInfo(culture);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

