namespace Pmad.Wiki.Helpers;

internal static class SlideShowHelper
{
    internal static readonly IReadOnlyList<string> SlideShowThemesList =
        ["black", "white", "league", "beige", "sky", "night", "serif", "simple", "solarized", "blood", "moon", "dracula"];

    private static readonly HashSet<string> SlideShowThemesSet =
        new(SlideShowThemesList, StringComparer.OrdinalIgnoreCase);

    internal static string DefaultTheme = "black";

    public static string GetValidTheme(string? theme)
    {
        return SlideShowThemesSet.Contains(theme ?? string.Empty)
                    ? theme!.ToLowerInvariant()
                    : DefaultTheme;
    }
}
