using Pmad.Wiki.Helpers;

namespace Pmad.Wiki.Test.Helpers;

public class WikiInputSanitizerTest
{
    [Theory]
    [InlineData("Hello World", "Hello-World")]
    [InlineData("Test-Page", "Test-Page")]
    [InlineData("Test_Page", "Test_Page")]
    [InlineData("Café", "Cafe")]
    [InlineData("Über", "Uber")]
    [InlineData("Test@Page!", "TestPage")]
    [InlineData("Test  Multiple  Spaces", "Test-Multiple-Spaces")]
    [InlineData("Test--Multiple--Hyphens", "Test-Multiple-Hyphens")]
    [InlineData("Test__Multiple__Underscores", "Test_Multiple_Underscores")]
    [InlineData("-Leading-Hyphen", "Leading-Hyphen")]
    [InlineData("Trailing-Hyphen-", "Trailing-Hyphen")]
    [InlineData("_Leading_Underscore", "Leading_Underscore")]
    [InlineData("Trailing_Underscore_", "Trailing_Underscore")]
    [InlineData("123-Numbers-456", "123-Numbers-456")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Sanitize_ReturnsExpectedResult(string input, string expected)
    {
        var result = WikiInputSanitizer.Sanitize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("path/to/page", "path/to/page")]
    [InlineData("path / to / page", "path/to/page")]
    [InlineData("Projects/My Project/2024", "Projects/My-Project/2024")]
    [InlineData("Test@Path!/Sub Folder", "TestPath/Sub-Folder")]
    [InlineData("/leading/slash", "leading/slash")]
    [InlineData("trailing/slash/", "trailing/slash")]
    [InlineData("multiple///slashes", "multiple/slashes")]
    [InlineData("", "")]
    public void SanitizeLocation_ReturnsExpectedResult(string input, string expected)
    {
        var result = WikiInputSanitizer.SanitizeLocation(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("User Login Feature", "User-Login-Feature")]
    [InlineData("UI & API Updates", "UI-API-Updates")]
    [InlineData("Bug Fix #123", "Bug-Fix-123")]
    [InlineData("v1.2.3 Release", "v1-2-3-Release")]
    [InlineData("JIRA-456: New Feature", "JIRA-456-New-Feature")]
    public void Sanitize_RealWorldExamples_ReturnsValidPageName(string input, string expected)
    {
        var result = WikiInputSanitizer.Sanitize(input);
        Assert.Equal(expected, result);
        
        // Verify result matches page name validation pattern
        Assert.Matches("^[a-zA-Z0-9_-]+$", result);
    }

    [Theory]
    [InlineData("Projects/My App/Features", "Projects/My-App/Features")]
    [InlineData("Docs/API v2/Endpoints", "Docs/API-v2/Endpoints")]
    public void SanitizeLocation_RealWorldExamples_ReturnsValidLocation(string input, string expected)
    {
        var result = WikiInputSanitizer.SanitizeLocation(input);
        Assert.Equal(expected, result);
        
        // Verify result matches location validation pattern
        Assert.Matches("^[a-zA-Z0-9_/-]+$", result);
    }

    #region SanitizeFileName Tests

    [Theory]
    [InlineData("screenshot", "screenshot")]
    [InlineData("MyDocument", "MyDocument")]
    [InlineData("report2024", "report2024")]
    [InlineData("my_file_name", "my_file_name")]
    [InlineData("my-file-name", "my-file-name")]
    [InlineData("my document file", "my-document-file")]
    [InlineData("backup.tar", "backup-tar")]
    [InlineData("file.name.with.dots", "file-name-with-dots")]
    [InlineData("file@name#with$special%chars", "filenamewithspecialchars")]
    [InlineData("???", "file")]
    [InlineData("My Photo (2024).jpg", "My-Photo-2024-jpg")]
    [InlineData("", "file")]
    [InlineData("@#$%^&*()", "file")]
    [InlineData("   ", "file")]
    [InlineData(" file name ", "file-name")]
    [InlineData("file\tname\twith\ttabs", "file-name-with-tabs")]
    [InlineData("file\nname", "file-name")]
    [InlineData("Screenshot 2024-01-15 at 10.30.45", "Screenshot-2024-01-15-at-10-30-45")]
    [InlineData("Annual Report (Final Version)", "Annual-Report-Final-Version")]
    [InlineData("database_backup.2024.01.15", "database_backup-2024-01-15")]
    public void SanitizeMediaFileName_WithVariousInputs_ReturnsExpectedOutput(string input, string expected)
    {
        // Act
        var result = WikiInputSanitizer.SanitizeMediaFileName(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(150, 100)]
    [InlineData(100, 100)]
    [InlineData(99, 99)]
    [InlineData(50, 50)]
    [InlineData(1, 1)]
    public void SanitizeMediaFileName_WithVariousLengths_HandlesCorrectly(int inputLength, int expectedLength)
    {
        // Arrange
        var input = new string('a', inputLength);

        // Act
        var result = WikiInputSanitizer.SanitizeMediaFileName(input);

        // Assert
        Assert.Equal(expectedLength, result.Length);
        Assert.Equal(new string('a', expectedLength), result);
    }

    #endregion
}
