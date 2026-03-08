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

    #region GetValidTheme Tests

    [Theory]
    [InlineData("black")]
    [InlineData("white")]
    [InlineData("league")]
    [InlineData("beige")]
    [InlineData("sky")]
    [InlineData("night")]
    [InlineData("serif")]
    [InlineData("simple")]
    [InlineData("solarized")]
    [InlineData("blood")]
    [InlineData("moon")]
    [InlineData("dracula")]
    public void GetValidTheme_WithValidTheme_ReturnsTheme(string theme)
    {
        // Act
        var result = SlideShowHelper.GetValidTheme(theme);

        // Assert
        Assert.Equal(theme, result);
    }

    [Theory]
    [InlineData("BLACK")]
    [InlineData("White")]
    [InlineData("League")]
    [InlineData("DRACULA")]
    [InlineData("Solarized")]
    public void GetValidTheme_WithValidThemeInDifferentCase_ReturnsLowercase(string theme)
    {
        // Act
        var result = SlideShowHelper.GetValidTheme(theme);

        // Assert
        Assert.Equal(theme.ToLowerInvariant(), result);
    }

    [Fact]
    public void GetValidTheme_WithNull_ReturnsDefaultTheme()
    {
        // Act
        var result = SlideShowHelper.GetValidTheme(null);

        // Assert
        Assert.Equal(SlideShowHelper.DefaultTheme, result);
    }

    [Fact]
    public void GetValidTheme_WithEmptyString_ReturnsDefaultTheme()
    {
        // Act
        var result = SlideShowHelper.GetValidTheme(string.Empty);

        // Assert
        Assert.Equal(SlideShowHelper.DefaultTheme, result);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("invalid")]
    [InlineData("bootstrap")]
    [InlineData("dark")]
    [InlineData("light")]
    public void GetValidTheme_WithInvalidTheme_ReturnsDefaultTheme(string theme)
    {
        // Act
        var result = SlideShowHelper.GetValidTheme(theme);

        // Assert
        Assert.Equal(SlideShowHelper.DefaultTheme, result);
    }

    [Fact]
    public void GetValidTheme_ReturnValueIsAlwaysLowercase()
    {
        foreach (var theme in SlideShowHelper.SlideShowThemesList)
        {
            var result = SlideShowHelper.GetValidTheme(theme.ToUpperInvariant());
            Assert.Equal(theme, result);
        }
    }

    #endregion
}
