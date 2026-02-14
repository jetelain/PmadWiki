using Pmad.Wiki.Helpers;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Test.Helpers;

public class WikiUserHelperTest
{
    #region GenerateUniqueGitEmail Tests

    [Fact]
    public void GenerateUniqueGitEmail_ReturnsValidEmailFormat()
    {
        // Act
        var email = WikiUserHelper.GenerateUniqueGitEmail();

        // Assert
        Assert.NotNull(email);
        Assert.EndsWith("@pmadwiki.local", email);
    }

    [Fact]
    public void GenerateUniqueGitEmail_ReturnsEmailWithGuidFormat()
    {
        // Act
        var email = WikiUserHelper.GenerateUniqueGitEmail();
        var localPart = email.Split('@')[0];

        // Assert
        Assert.Equal(32, localPart.Length); // GUID without hyphens is 32 characters
        Assert.All(localPart, c => Assert.True(char.IsLetterOrDigit(c)));
    }

    [Fact]
    public void GenerateUniqueGitEmail_GeneratesDifferentEmailsOnEachCall()
    {
        // Act
        var email1 = WikiUserHelper.GenerateUniqueGitEmail();
        var email2 = WikiUserHelper.GenerateUniqueGitEmail();
        var email3 = WikiUserHelper.GenerateUniqueGitEmail();

        // Assert
        Assert.NotEqual(email1, email2);
        Assert.NotEqual(email2, email3);
        Assert.NotEqual(email1, email3);
    }

    [Fact]
    public void GenerateUniqueGitEmail_ReturnsLowercaseEmail()
    {
        // Act
        var email = WikiUserHelper.GenerateUniqueGitEmail();

        // Assert
        Assert.Equal(email.ToLowerInvariant(), email);
    }

    #endregion

    #region GenerateGitEmailFromExternalIdentifier Tests

    [Fact]
    public void GenerateGitEmailFromExternalIdentifier_ReturnsValidEmailFormat()
    {
        // Arrange
        var externalId = "user123";

        // Act
        var email = WikiUserHelper.GenerateGitEmailFromExternalIdentifier(externalId);

        // Assert
        Assert.NotNull(email);
        Assert.EndsWith("@pmadwiki.local", email);
    }

    [Fact]
    public void GenerateGitEmailFromExternalIdentifier_ReturnsDeterministicResult()
    {
        // Arrange
        var externalId = "user123";

        // Act
        var email1 = WikiUserHelper.GenerateGitEmailFromExternalIdentifier(externalId);
        var email2 = WikiUserHelper.GenerateGitEmailFromExternalIdentifier(externalId);
        var email3 = WikiUserHelper.GenerateGitEmailFromExternalIdentifier(externalId);

        // Assert
        Assert.Equal(email1, email2);
        Assert.Equal(email2, email3);
    }

    [Fact]
    public void GenerateGitEmailFromExternalIdentifier_ReturnsDifferentEmailsForDifferentIdentifiers()
    {
        // Arrange
        var externalId1 = "user123";
        var externalId2 = "user456";
        var externalId3 = "admin";

        // Act
        var email1 = WikiUserHelper.GenerateGitEmailFromExternalIdentifier(externalId1);
        var email2 = WikiUserHelper.GenerateGitEmailFromExternalIdentifier(externalId2);
        var email3 = WikiUserHelper.GenerateGitEmailFromExternalIdentifier(externalId3);

        // Assert
        Assert.NotEqual(email1, email2);
        Assert.NotEqual(email2, email3);
        Assert.NotEqual(email1, email3);
    }

    [Fact]
    public void GenerateGitEmailFromExternalIdentifier_ReturnsLowercaseEmail()
    {
        // Arrange
        var externalId = "USER123";

        // Act
        var email = WikiUserHelper.GenerateGitEmailFromExternalIdentifier(externalId);

        // Assert
        Assert.Equal(email.ToLowerInvariant(), email);
    }

    [Fact]
    public void GenerateGitEmailFromExternalIdentifier_ReturnsHexadecimalLocalPart()
    {
        // Arrange
        var externalId = "user123";

        // Act
        var email = WikiUserHelper.GenerateGitEmailFromExternalIdentifier(externalId);
        var localPart = email.Split('@')[0];

        // Assert
        Assert.All(localPart, c => Assert.True(char.IsDigit(c) || (c >= 'a' && c <= 'f')));
    }

    [Fact]
    public void GenerateGitEmailFromExternalIdentifier_ReturnsSHA256Length()
    {
        // Arrange
        var externalId = "user123";

        // Act
        var email = WikiUserHelper.GenerateGitEmailFromExternalIdentifier(externalId);
        var localPart = email.Split('@')[0];

        // Assert
        Assert.Equal(64, localPart.Length); // SHA256 hex string is 64 characters
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("user@example.com")]
    [InlineData("special!@#$%")]
    [InlineData("unicode-émoji-🎉")]
    public void GenerateGitEmailFromExternalIdentifier_HandlesVariousInputs(string externalId)
    {
        // Act
        var email = WikiUserHelper.GenerateGitEmailFromExternalIdentifier(externalId);

        // Assert
        Assert.NotNull(email);
        Assert.EndsWith("@pmadwiki.local", email);
    }

    #endregion

    #region SanitizeGitNameOrEmail Tests

    [Theory]
    [InlineData("validname", "validname")]
    [InlineData("valid-name", "valid-name")]
    [InlineData("valid_name", "valid_name")]
    [InlineData("valid@example.com", "valid@example.com")]
    [InlineData("John Doe", "John Doe")]
    [InlineData("user123", "user123")]
    public void SanitizeGitNameOrEmail_WithValidInput_ReturnsUnchanged(string input, string expected)
    {
        // Act
        var result = WikiUserHelper.SanitizeGitNameOrEmail(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("name<tag>", "name_tag_")]
    [InlineData("<user>", "_user_")]
    [InlineData("user<", "user_")]
    [InlineData(">user", "_user")]
    public void SanitizeGitNameOrEmail_WithAngleBrackets_ReplacesWithUnderscore(string input, string expected)
    {
        // Act
        var result = WikiUserHelper.SanitizeGitNameOrEmail(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("name\nwith\nnewlines", "name_with_newlines")]
    [InlineData("line1\nline2", "line1_line2")]
    [InlineData("text\n", "text_")]
    [InlineData("\ntext", "_text")]
    public void SanitizeGitNameOrEmail_WithNewlines_ReplacesWithUnderscore(string input, string expected)
    {
        // Act
        var result = WikiUserHelper.SanitizeGitNameOrEmail(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("name\rwith\rcarriagereturn", "name_with_carriagereturn")]
    [InlineData("text\r\n", "text__")]
    [InlineData("\rtext", "_text")]
    public void SanitizeGitNameOrEmail_WithCarriageReturns_ReplacesWithUnderscore(string input, string expected)
    {
        // Act
        var result = WikiUserHelper.SanitizeGitNameOrEmail(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("name\0with\0null", "name_with_null")]
    [InlineData("text\0", "text_")]
    [InlineData("\0text", "_text")]
    public void SanitizeGitNameOrEmail_WithNullCharacters_ReplacesWithUnderscore(string input, string expected)
    {
        // Act
        var result = WikiUserHelper.SanitizeGitNameOrEmail(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeGitNameOrEmail_WithMultipleInvalidCharacters_ReplacesAllWithUnderscore()
    {
        // Arrange
        var input = "bad<name>\nwith\rmany\0problems";
        var expected = "bad_name__with_many_problems";

        // Act
        var result = WikiUserHelper.SanitizeGitNameOrEmail(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeGitNameOrEmail_WithEmptyString_ReturnsEmptyString()
    {
        // Act
        var result = WikiUserHelper.SanitizeGitNameOrEmail("");

        // Assert
        Assert.Equal("", result);
    }

    [Theory]
    [InlineData("Name With Spaces")]
    [InlineData("user@example.com")]
    [InlineData("Name-With-Dashes")]
    [InlineData("Name_With_Underscores")]
    [InlineData("Name.With.Dots")]
    public void SanitizeGitNameOrEmail_WithCommonValidPatterns_ReturnsUnchanged(string input)
    {
        // Act
        var result = WikiUserHelper.SanitizeGitNameOrEmail(input);

        // Assert
        Assert.Equal(input, result);
    }

    #endregion

    #region CreateGitCommitSignature Tests

    [Fact]
    public void CreateGitCommitSignature_WithValidUser_ReturnsSignature()
    {
        // Arrange
        var user = new TestWikiUser("John Doe", "john@example.com");

        // Act
        var signature = WikiUserHelper.CreateGitCommitSignature(user);

        // Assert
        Assert.NotNull(signature);
        Assert.Equal("John Doe", signature.Name);
        Assert.Equal("john@example.com", signature.Email);
    }

    [Fact]
    public void CreateGitCommitSignature_SanitizesUserName()
    {
        // Arrange
        var user = new TestWikiUser("John<Doe>", "john@example.com");

        // Act
        var signature = WikiUserHelper.CreateGitCommitSignature(user);

        // Assert
        Assert.Equal("John_Doe_", signature.Name);
        Assert.Equal("john@example.com", signature.Email);
    }

    [Fact]
    public void CreateGitCommitSignature_SanitizesEmail()
    {
        // Arrange
        var user = new TestWikiUser("John Doe", "john<test>@example.com");

        // Act
        var signature = WikiUserHelper.CreateGitCommitSignature(user);

        // Assert
        Assert.Equal("John Doe", signature.Name);
        Assert.Equal("john_test_@example.com", signature.Email);
    }

    [Fact]
    public void CreateGitCommitSignature_SanitizesBothNameAndEmail()
    {
        // Arrange
        var user = new TestWikiUser("Bad\nName", "bad\remail@example.com");

        // Act
        var signature = WikiUserHelper.CreateGitCommitSignature(user);

        // Assert
        Assert.Equal("Bad_Name", signature.Name);
        Assert.Equal("bad_email@example.com", signature.Email);
    }

    [Fact]
    public void CreateGitCommitSignature_WithMultipleInvalidCharacters_SanitizesAll()
    {
        // Arrange
        var user = new TestWikiUser("User<>\n\r\0", "email<>\n\r\0@test.com");

        // Act
        var signature = WikiUserHelper.CreateGitCommitSignature(user);

        // Assert
        Assert.Equal("User_____", signature.Name);
        Assert.Equal("email_____@test.com", signature.Email);
    }

    [Fact]
    public void CreateGitCommitSignature_SetsDateTimeToUtcNow()
    {
        // Arrange
        var user = new TestWikiUser("John Doe", "john@example.com");
        var beforeCall = DateTimeOffset.UtcNow;

        // Act
        var signature = WikiUserHelper.CreateGitCommitSignature(user);
        var afterCall = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(signature.Timestamp >= beforeCall);
        Assert.True(signature.Timestamp <= afterCall);
        Assert.Equal(DateTimeOffset.UtcNow.Offset, signature.Timestamp.Offset);
    }

    #endregion

    #region Test Helper Class

    private class TestWikiUser : IWikiUser
    {
        public TestWikiUser(string gitName, string gitEmail)
        {
            GitName = gitName;
            GitEmail = gitEmail;
            DisplayName = gitName;
        }

        public string GitEmail { get; }
        public string GitName { get; }
        public string DisplayName { get; }
    }

    #endregion
}
