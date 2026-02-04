using Pmad.Wiki.Helpers;

namespace Pmad.Wiki.Test.Helpers;

public class WikiInputValidatorTest
{
    #region IsValidPageName Tests

    [Theory]
    [InlineData("validname")]
    [InlineData("valid-name")]
    [InlineData("valid_name")]
    [InlineData("valid123")]
    [InlineData("VALID")]
    [InlineData("Valid/Page")]
    [InlineData("path/to/page")]
    [InlineData("a")]
    [InlineData("123")]
    [InlineData("path/to/deep/nested/page")]
    [InlineData("CamelCase")]
    [InlineData("snake_case")]
    [InlineData("kebab-case")]
    [InlineData("mixed-Case_123")]
    public void IsValidPageName_WithValidName_ReturnsTrue(string pageName)
    {
        // Act
        var result = WikiInputValidator.IsValidPageName(pageName, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void IsValidPageName_WithNull_ReturnsFalse()
    {
        // Act
        var result = WikiInputValidator.IsValidPageName(null!, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Page name cannot be null or empty.", errorMessage);
    }

    [Fact]
    public void IsValidPageName_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = WikiInputValidator.IsValidPageName("", out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Page name cannot be null or empty.", errorMessage);
    }

    [Fact]
    public void IsValidPageName_WithWhitespace_ReturnsFalse()
    {
        // Act
        var result = WikiInputValidator.IsValidPageName("   ", out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Page name cannot be null or empty.", errorMessage);
    }

    [Theory]
    [InlineData("name with spaces")]
    [InlineData("name@special")]
    [InlineData("name#hash")]
    [InlineData("name$dollar")]
    [InlineData("name%percent")]
    [InlineData("name&ampersand")]
    [InlineData("name*asterisk")]
    [InlineData("name(paren")]
    [InlineData("name)paren")]
    [InlineData("name+plus")]
    [InlineData("name=equals")]
    [InlineData("name[bracket")]
    [InlineData("name]bracket")]
    [InlineData("name{brace")]
    [InlineData("name}brace")]
    [InlineData("name|pipe")]
    [InlineData("name\\backslash")]
    [InlineData("name:colon")]
    [InlineData("name;semicolon")]
    [InlineData("name'quote")]
    [InlineData("name\"doublequote")]
    [InlineData("name<less")]
    [InlineData("name>greater")]
    [InlineData("name,comma")]
    [InlineData("name.dot")]
    [InlineData("name?question")]
    [InlineData("name!exclamation")]
    [InlineData("name~tilde")]
    [InlineData("name`backtick")]
    public void IsValidPageName_WithInvalidCharacters_ReturnsFalse(string pageName)
    {
        // Act
        var result = WikiInputValidator.IsValidPageName(pageName, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Invalid name.", errorMessage);
    }

    [Theory]
    [InlineData("..")]
    [InlineData("path/../other")]
    [InlineData("../parent")]
    [InlineData("path/to/../page")]
    public void IsValidPageName_WithDoubleDot_ReturnsFalse(string pageName)
    {
        // Act
        var result = WikiInputValidator.IsValidPageName(pageName, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Invalid name.", errorMessage);
    }

    [Theory]
    [InlineData("//")]
    [InlineData("path//page")]
    [InlineData("path//to//page")]
    public void IsValidPageName_WithDoubleSlash_ReturnsFalse(string pageName)
    {
        // Act
        var result = WikiInputValidator.IsValidPageName(pageName, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Invalid name.", errorMessage);
    }

    [Theory]
    [InlineData("/path")]
    [InlineData("/page")]
    [InlineData("/path/to/page")]
    public void IsValidPageName_StartingWithSlash_ReturnsFalse(string pageName)
    {
        // Act
        var result = WikiInputValidator.IsValidPageName(pageName, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Invalid name.", errorMessage);
    }

    [Theory]
    [InlineData("path/")]
    [InlineData("page/")]
    [InlineData("path/to/page/")]
    public void IsValidPageName_EndingWithSlash_ReturnsFalse(string pageName)
    {
        // Act
        var result = WikiInputValidator.IsValidPageName(pageName, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Invalid name.", errorMessage);
    }

    #endregion

    #region IsValidCulture Tests

    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    [InlineData("en-GB")]
    [InlineData("pt-BR")]
    [InlineData("zh-CN")]
    public void IsValidCulture_WithValidCulture_ReturnsTrue(string culture)
    {
        // Act
        var result = WikiInputValidator.IsValidCulture(culture, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void IsValidCulture_WithNull_ReturnsFalse()
    {
        // Act
        var result = WikiInputValidator.IsValidCulture(null!, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Culture cannot be null or empty.", errorMessage);
    }

    [Fact]
    public void IsValidCulture_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = WikiInputValidator.IsValidCulture("", out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Culture cannot be null or empty.", errorMessage);
    }

    [Fact]
    public void IsValidCulture_WithWhitespace_ReturnsFalse()
    {
        // Act
        var result = WikiInputValidator.IsValidCulture("   ", out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Culture cannot be null or empty.", errorMessage);
    }

    [Theory]
    [InlineData("en-us")] // lowercase country code
    [InlineData("EN")] // uppercase language code
    [InlineData("EN-US")] // uppercase language code
    [InlineData("en-us-extra")] // too many parts
    [InlineData("e")] // too short language code
    [InlineData("eng")] // too long language code
    [InlineData("en-U")] // too short country code
    [InlineData("en-USA")] // too long country code
    [InlineData("en_US")] // wrong separator
    [InlineData("12")] // numbers instead of letters
    [InlineData("en-12")] // numbers in country code
    [InlineData("e1")] // number in language code
    public void IsValidCulture_WithInvalidFormat_ReturnsFalse(string culture)
    {
        // Act
        var result = WikiInputValidator.IsValidCulture(culture, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Invalid culture identifier.", errorMessage);
    }

    #endregion

    #region ValidatePageName Tests

    [Theory]
    [InlineData("validname")]
    [InlineData("path/to/page")]
    public void ValidatePageName_WithValidName_DoesNotThrow(string pageName)
    {
        // Act & Assert
        var exception = Record.Exception(() => WikiInputValidator.ValidatePageName(pageName));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidatePageName_WithNull_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            WikiInputValidator.ValidatePageName(null!));
        Assert.Equal("pageName", exception.ParamName);
        Assert.Contains("Page name cannot be null or empty.", exception.Message);
    }

    [Fact]
    public void ValidatePageName_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            WikiInputValidator.ValidatePageName(""));
        Assert.Equal("pageName", exception.ParamName);
        Assert.Contains("Page name cannot be null or empty.", exception.Message);
    }

    [Theory]
    [InlineData("invalid name")]
    [InlineData("path/../other")]
    [InlineData("/path")]
    [InlineData("path/")]
    public void ValidatePageName_WithInvalidName_ThrowsArgumentException(string pageName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            WikiInputValidator.ValidatePageName(pageName));
        Assert.Equal("pageName", exception.ParamName);
        Assert.Contains("Invalid name.", exception.Message);
    }

    #endregion

    #region ValidateCulture Tests

    [Theory]
    [InlineData("en")]
    [InlineData("fr-FR")]
    public void ValidateCulture_WithValidCulture_DoesNotThrow(string culture)
    {
        // Act & Assert
        var exception = Record.Exception(() => WikiInputValidator.ValidateCulture(culture));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateCulture_WithNull_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            WikiInputValidator.ValidateCulture(null!));
        Assert.Equal("culture", exception.ParamName);
        Assert.Contains("Culture cannot be null or empty.", exception.Message);
    }

    [Fact]
    public void ValidateCulture_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            WikiInputValidator.ValidateCulture(""));
        Assert.Equal("culture", exception.ParamName);
        Assert.Contains("Culture cannot be null or empty.", exception.Message);
    }

    [Theory]
    [InlineData("EN")]
    [InlineData("en-us")]
    [InlineData("invalid")]
    public void ValidateCulture_WithInvalidCulture_ThrowsArgumentException(string culture)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            WikiInputValidator.ValidateCulture(culture));
        Assert.Equal("culture", exception.ParamName);
        Assert.Contains("Invalid culture identifier.", exception.Message);
    }

    #endregion

    #region IsValidMediaPath Tests

    [Theory]
    [InlineData("image.jpg")]
    [InlineData("document.pdf")]
    [InlineData("valid-file.png")]
    [InlineData("valid_file.gif")]
    [InlineData("file123.txt")]
    [InlineData("VALID.PNG")]
    [InlineData("path/to/file.jpg")]
    [InlineData("a.b")]
    [InlineData("file.tar.gz")]
    [InlineData("path/to/deep/nested/file.pdf")]
    [InlineData("CamelCase.jpg")]
    [InlineData("snake_case.png")]
    [InlineData("kebab-case.gif")]
    [InlineData("mixed-Case_123.pdf")]
    [InlineData("file.with.multiple.dots.txt")]
    [InlineData("path/file.ext")]
    [InlineData("no-extension")]
    [InlineData("file")]
    [InlineData(".hidden")]
    [InlineData("path/.hidden")]
    public void IsValidMediaPath_WithValidPath_ReturnsTrue(string mediaPath)
    {
        // Act
        var result = WikiInputValidator.IsValidMediaPath(mediaPath, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void IsValidMediaPath_WithNull_ReturnsFalse()
    {
        // Act
        var result = WikiInputValidator.IsValidMediaPath(null!, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Media path cannot be null or empty.", errorMessage);
    }

    [Fact]
    public void IsValidMediaPath_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = WikiInputValidator.IsValidMediaPath("", out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Media path cannot be null or empty.", errorMessage);
    }

    [Fact]
    public void IsValidMediaPath_WithWhitespace_ReturnsFalse()
    {
        // Act
        var result = WikiInputValidator.IsValidMediaPath("   ", out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Media path cannot be null or empty.", errorMessage);
    }

    [Theory]
    [InlineData("file with spaces.jpg")]
    [InlineData("file@special.jpg")]
    [InlineData("file#hash.jpg")]
    [InlineData("file$dollar.jpg")]
    [InlineData("file%percent.jpg")]
    [InlineData("file&ampersand.jpg")]
    [InlineData("file*asterisk.jpg")]
    [InlineData("file(paren.jpg")]
    [InlineData("file)paren.jpg")]
    [InlineData("file+plus.jpg")]
    [InlineData("file=equals.jpg")]
    [InlineData("file[bracket.jpg")]
    [InlineData("file]bracket.jpg")]
    [InlineData("file{brace.jpg")]
    [InlineData("file}brace.jpg")]
    [InlineData("file|pipe.jpg")]
    [InlineData("file\\backslash.jpg")]
    [InlineData("file:colon.jpg")]
    [InlineData("file;semicolon.jpg")]
    [InlineData("file'quote.jpg")]
    [InlineData("file\"doublequote.jpg")]
    [InlineData("file<less.jpg")]
    [InlineData("file>greater.jpg")]
    [InlineData("file,comma.jpg")]
    [InlineData("file?question.jpg")]
    [InlineData("file!exclamation.jpg")]
    [InlineData("file~tilde.jpg")]
    [InlineData("file`backtick.jpg")]
    public void IsValidMediaPath_WithInvalidCharacters_ReturnsFalse(string mediaPath)
    {
        // Act
        var result = WikiInputValidator.IsValidMediaPath(mediaPath, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Invalid path.", errorMessage);
    }

    [Theory]
    [InlineData("..")]
    [InlineData("path/../other.jpg")]
    [InlineData("../parent.jpg")]
    [InlineData("path/to/../file.jpg")]
    [InlineData("file..jpg")]
    public void IsValidMediaPath_WithDoubleDot_ReturnsFalse(string mediaPath)
    {
        // Act
        var result = WikiInputValidator.IsValidMediaPath(mediaPath, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Invalid path.", errorMessage);
    }

    [Theory]
    [InlineData("//")]
    [InlineData("path//file.jpg")]
    [InlineData("path//to//file.jpg")]
    public void IsValidMediaPath_WithDoubleSlash_ReturnsFalse(string mediaPath)
    {
        // Act
        var result = WikiInputValidator.IsValidMediaPath(mediaPath, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Invalid path.", errorMessage);
    }

    [Theory]
    [InlineData("/path/file.jpg")]
    [InlineData("/file.jpg")]
    [InlineData("/path/to/file.jpg")]
    public void IsValidMediaPath_StartingWithSlash_ReturnsFalse(string mediaPath)
    {
        // Act
        var result = WikiInputValidator.IsValidMediaPath(mediaPath, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Invalid path.", errorMessage);
    }

    [Theory]
    [InlineData("path/")]
    [InlineData("file.jpg/")]
    [InlineData("path/to/file.jpg/")]
    public void IsValidMediaPath_EndingWithSlash_ReturnsFalse(string mediaPath)
    {
        // Act
        var result = WikiInputValidator.IsValidMediaPath(mediaPath, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal("Invalid path.", errorMessage);
    }

    #endregion

    #region ValidateMediaPath Tests

    [Theory]
    [InlineData("image.jpg")]
    [InlineData("path/to/file.pdf")]
    [InlineData("file.with.dots.txt")]
    public void ValidateMediaPath_WithValidPath_DoesNotThrow(string mediaPath)
    {
        // Act & Assert
        var exception = Record.Exception(() => WikiInputValidator.ValidateMediaPath(mediaPath));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateMediaPath_WithNull_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            WikiInputValidator.ValidateMediaPath(null!));
        Assert.Equal("mediaPath", exception.ParamName);
        Assert.Contains("Media path cannot be null or empty.", exception.Message);
    }

    [Fact]
    public void ValidateMediaPath_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            WikiInputValidator.ValidateMediaPath(""));
        Assert.Equal("mediaPath", exception.ParamName);
        Assert.Contains("Media path cannot be null or empty.", exception.Message);
    }

    [Theory]
    [InlineData("invalid file.jpg")]
    [InlineData("path/../other.jpg")]
    [InlineData("/path/file.jpg")]
    [InlineData("path/file.jpg/")]
    public void ValidateMediaPath_WithInvalidPath_ThrowsArgumentException(string mediaPath)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            WikiInputValidator.ValidateMediaPath(mediaPath));
        Assert.Equal("mediaPath", exception.ParamName);
        Assert.Contains("Invalid path.", exception.Message);
    }

    #endregion
}

