namespace Pmad.Wiki.Helpers;

internal static class SlideShowHelper
{
    internal static readonly IReadOnlyList<string> SlideShowThemesList =
        ["black", "white", "league", "beige", "sky", "night", "serif", "simple", "solarized", "blood", "moon", "dracula"];

    private static readonly HashSet<string> SlideShowThemesSet =
        new(SlideShowThemesList, StringComparer.OrdinalIgnoreCase);

    internal const string DefaultTheme = "black";

    internal static string GetValidTheme(string? theme, WikiOptions options)
    {
        var allowed = options.SlideShowAllowedThemes;
        var defaultTheme = options.SlideShowDefaultTheme;

        var match = allowed.FirstOrDefault(t => string.Equals(t, theme, StringComparison.OrdinalIgnoreCase));
        if (match != null && WikiInputValidator.IsValidThemeName(match))
        {
            return match.ToLowerInvariant();
        }

        var defaultMatch = allowed.FirstOrDefault(t => string.Equals(t, defaultTheme, StringComparison.OrdinalIgnoreCase));
        if (defaultMatch != null && WikiInputValidator.IsValidThemeName(defaultMatch))
        {
            return defaultMatch.ToLowerInvariant();
        }

        return DefaultTheme;
    }

    internal static string GetThemeUri(string theme)
    {
        if (!WikiInputValidator.IsValidThemeName(theme))
        {
            theme = DefaultTheme;
        }

        var encodedTheme = Uri.EscapeDataString(theme.ToLowerInvariant());
        return SlideShowThemesSet.Contains(theme)
            ? $"/lib/revealjs/theme/{encodedTheme}.css"
            : $"/css/revealjs-custom-theme/{encodedTheme}.css";
    }
}
