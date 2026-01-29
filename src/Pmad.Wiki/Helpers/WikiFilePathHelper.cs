using System.Globalization;

namespace Pmad.Wiki.Helpers;

public static class WikiFilePathHelper
{
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

    private static bool IsValidCulture(string culture)
    {
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

