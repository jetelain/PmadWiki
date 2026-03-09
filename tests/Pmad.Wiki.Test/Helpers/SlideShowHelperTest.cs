using Pmad.Wiki;
using Pmad.Wiki.Helpers;

namespace Pmad.Wiki.Test.Helpers;

public class SlideShowHelperTest
{
    #region SlideShowThemesList Tests

    [Fact]
    public void SlideShowThemesList_ContainsExpectedThemes()
    {
        // Assert
        Assert.Contains("black", SlideShowHelper.SlideShowThemesList);
        Assert.Contains("white", SlideShowHelper.SlideShowThemesList);
        Assert.Contains("league", SlideShowHelper.SlideShowThemesList);
        Assert.Contains("beige", SlideShowHelper.SlideShowThemesList);
        Assert.Contains("sky", SlideShowHelper.SlideShowThemesList);
        Assert.Contains("night", SlideShowHelper.SlideShowThemesList);
        Assert.Contains("serif", SlideShowHelper.SlideShowThemesList);
        Assert.Contains("simple", SlideShowHelper.SlideShowThemesList);
        Assert.Contains("solarized", SlideShowHelper.SlideShowThemesList);
        Assert.Contains("blood", SlideShowHelper.SlideShowThemesList);
        Assert.Contains("moon", SlideShowHelper.SlideShowThemesList);
        Assert.Contains("dracula", SlideShowHelper.SlideShowThemesList);
    }

    [Fact]
    public void SlideShowThemesList_HasTwelveThemes()
    {
        Assert.Equal(12, SlideShowHelper.SlideShowThemesList.Count);
    }

    #endregion

    #region DefaultTheme Tests

    [Fact]
    public void DefaultTheme_IsBlack()
    {
        Assert.Equal("black", SlideShowHelper.DefaultTheme);
    }

    #endregion

    #region GetValidTheme_WithOptions Tests

    private static WikiOptions DefaultOptions() => new();

    private static WikiOptions OptionsWithAllowed(List<string> allowed, string defaultTheme = SlideShowHelper.DefaultTheme) =>
        new() { SlideShowAllowedThemes = allowed, SlideShowDefaultTheme = defaultTheme };

    [Theory]
    [InlineData("black")]
    [InlineData("white")]
    [InlineData("league")]
    [InlineData("dracula")]
    public void GetValidTheme_WithOptions_WithValidBuiltInTheme_ReturnsTheme(string theme)
    {
        var result = SlideShowHelper.GetValidTheme(theme, DefaultOptions());

        Assert.Equal(theme, result);
    }

    [Theory]
    [InlineData("BLACK", "black")]
    [InlineData("White", "white")]
    [InlineData("DRACULA", "dracula")]
    [InlineData("Solarized", "solarized")]
    public void GetValidTheme_WithOptions_WithValidThemeInDifferentCase_ReturnsLowercase(string theme, string expected)
    {
        var result = SlideShowHelper.GetValidTheme(theme, DefaultOptions());

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetValidTheme_WithOptions_WithThemeNotInAllowedList_ReturnsDefaultTheme()
    {
        var options = OptionsWithAllowed(["black", "white"], "white");

        var result = SlideShowHelper.GetValidTheme("blue", options);

        Assert.Equal("white", result);
    }

    [Fact]
    public void GetValidTheme_WithOptions_WithNull_ReturnsDefaultTheme()
    {
        var options = OptionsWithAllowed(["black", "white"], "white");

        var result = SlideShowHelper.GetValidTheme(null, options);

        Assert.Equal("white", result);
    }

    [Fact]
    public void GetValidTheme_WithOptions_WithEmptyString_ReturnsDefaultTheme()
    {
        var options = OptionsWithAllowed(["black", "white"], "white");

        var result = SlideShowHelper.GetValidTheme(string.Empty, options);

        Assert.Equal("white", result);
    }

    [Fact]
    public void GetValidTheme_WithOptions_WithCustomTheme_ReturnsCustomTheme()
    {
        var options = OptionsWithAllowed(["black", "my-theme"]);

        var result = SlideShowHelper.GetValidTheme("my-theme", options);

        Assert.Equal("my-theme", result);
    }

    [Fact]
    public void GetValidTheme_WithOptions_WithCustomThemeDifferentCase_ReturnsLowercase()
    {
        var options = OptionsWithAllowed(["black", "my-theme"]);

        var result = SlideShowHelper.GetValidTheme("MY-THEME", options);

        Assert.Equal("my-theme", result);
    }

    [Fact]
    public void GetValidTheme_WithOptions_WithRestrictedAllowedList_ReturnsAllowedDefault()
    {
        var options = OptionsWithAllowed(["moon", "sky"], "moon");

        var result = SlideShowHelper.GetValidTheme("black", options);

        Assert.Equal("moon", result);
    }

    [Fact]
    public void GetValidTheme_WithOptions_WithInvalidThemeNameInAllowedList_FallsBackToDefault()
    {
        // "my theme" has a space — invalid name, should be skipped
        var options = OptionsWithAllowed(["black", "my theme"], "black");

        var result = SlideShowHelper.GetValidTheme("my theme", options);

        Assert.Equal("black", result);
    }

    [Fact]
    public void GetValidTheme_WithOptions_WithInvalidDefaultTheme_FallsBackToHardcodedDefault()
    {
        // both the requested theme and default are invalid names
        var options = OptionsWithAllowed(["my theme"], "my theme");

        var result = SlideShowHelper.GetValidTheme("other", options);

        Assert.Equal(SlideShowHelper.DefaultTheme, result);
    }

    [Fact]
    public void GetValidTheme_WithOptions_WithEmptyAllowedList_FallsBackToHardcodedDefault()
    {
        var options = OptionsWithAllowed([], "black");

        var result = SlideShowHelper.GetValidTheme("black", options);

        Assert.Equal(SlideShowHelper.DefaultTheme, result);
    }

    [Fact]
    public void GetValidTheme_WithOptions_ReturnValueIsAlwaysLowercase()
    {
        var options = DefaultOptions();
        foreach (var theme in SlideShowHelper.SlideShowThemesList)
        {
            var result = SlideShowHelper.GetValidTheme(theme.ToUpperInvariant(), options);
            Assert.Equal(theme, result);
        }
    }

    #endregion

    #region GetThemeUri Tests

    [Theory]
    [InlineData("black",     "/lib/revealjs/theme/black.css")]
    [InlineData("white",     "/lib/revealjs/theme/white.css")]
    [InlineData("league",    "/lib/revealjs/theme/league.css")]
    [InlineData("beige",     "/lib/revealjs/theme/beige.css")]
    [InlineData("solarized", "/lib/revealjs/theme/solarized.css")]
    [InlineData("dracula",   "/lib/revealjs/theme/dracula.css")]
    public void GetThemeUri_WithBuiltInTheme_ReturnsCdnUri(string theme, string expected)
    {
        var result = SlideShowHelper.GetThemeUri(theme);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetThemeUri_WithAllBuiltInThemes_ReturnsCdnUris()
    {
        foreach (var theme in SlideShowHelper.SlideShowThemesList)
        {
            var result = SlideShowHelper.GetThemeUri(theme);

            Assert.StartsWith("/lib/revealjs/theme/", result);
            Assert.EndsWith($"{theme}.css", result);
        }
    }

    [Theory]
    [InlineData("my-theme",      "/css/revealjs-custom-theme/my-theme.css")]
    [InlineData("custom_theme",  "/css/revealjs-custom-theme/custom_theme.css")]
    [InlineData("corporate",     "/css/revealjs-custom-theme/corporate.css")]
    [InlineData("dark2",         "/css/revealjs-custom-theme/dark2.css")]
    public void GetThemeUri_WithCustomTheme_ReturnsLocalUri(string theme, string expected)
    {
        var result = SlideShowHelper.GetThemeUri(theme);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("my theme")]    // space
    [InlineData("my.theme")]    // dot
    [InlineData("my/theme")]    // slash
    [InlineData("../evil")]     // path traversal
    [InlineData("<script>")]    // HTML injection
    [InlineData("theme&other")] // ampersand
    [InlineData("theme?x=1")]   // query string characters
    public void GetThemeUri_WithInvalidThemeName_ReturnsDefaultThemeUri(string theme)
    {
        var result = SlideShowHelper.GetThemeUri(theme);

        Assert.Equal("/lib/revealjs/theme/black.css", result);
    }

    #endregion
}
