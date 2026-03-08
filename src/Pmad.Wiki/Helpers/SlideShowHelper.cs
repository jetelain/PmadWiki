namespace Pmad.Wiki.Helpers;

internal static class SlideShowHelper
{
    internal static readonly IReadOnlyList<string> SlideShowThemesList =
        ["black", "white", "league", "beige", "sky", "night", "serif", "simple", "solarized", "blood", "moon", "dracula"];

    private static readonly HashSet<string> SlideShowThemesSet =
        new(SlideShowThemesList, StringComparer.OrdinalIgnoreCase);

    internal const string DefaultTheme = "black";

    internal static string GetValidTheme(string? theme)
    {
        return SlideShowThemesSet.Contains(theme ?? string.Empty)
                    ? theme!.ToLowerInvariant()
                    : DefaultTheme;
    }
}
