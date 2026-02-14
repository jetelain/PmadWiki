using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Pmad.Wiki.Test.Infrastructure;

/// <summary>
/// Tests to ensure TestUrlHelper and TestLinkGenerator have consistent behavior.
/// Both classes should generate the same URLs for the same inputs.
/// </summary>
public class TestUrlHelpersConsistencyTest
{
    [Theory]
    [InlineData("View", "Wiki", "homepage", null, "/wiki/view/homepage")]
    [InlineData("View", "Wiki", "docs/guide", null, "/wiki/view/docs/guide")]
    [InlineData("View", "Wiki", "test", "fr", "/wiki/view/test?culture=fr")]
    [InlineData("View", "Wiki", "admin/settings", "de", "/wiki/view/admin/settings?culture=de")]
    [InlineData("Media", "Wiki", "images/logo.png", null, "/wiki/media/images/logo.png")]
    [InlineData("Media", "Wiki", "docs/manual.pdf", null, "/wiki/media/docs/manual.pdf")]
    [InlineData("TempMedia", "Wiki", "abc123", null, "/wiki/tempmedia/abc123")]
    public void UrlHelper_And_LinkGenerator_Generate_Same_Urls(
        string action, 
        string controller, 
        string id, 
        string? culture, 
        string expectedUrl)
    {
        // Arrange
        var urlHelper = new TestUrlHelper();
        var linkGenerator = new TestLinkGenerator();
        
        var values = new RouteValueDictionary
        {
            ["action"] = action,
            ["controller"] = controller,
            ["id"] = id
        };

        if (culture != null)
        {
            values["culture"] = culture;
        }

        // Act
        var urlFromHelper = urlHelper.Action(new UrlActionContext
        {
            Action = action,
            Controller = controller,
            Values = values
        });

        var urlFromGenerator = linkGenerator.GetPathByAddress(
            address: (object?)null,
            values: values);

        // Assert
        Assert.Equal(expectedUrl, urlFromHelper);
        Assert.Equal(expectedUrl, urlFromGenerator);
        Assert.Equal(urlFromHelper, urlFromGenerator);
    }

    [Theory]
    [InlineData("View", "test", "/custom/view/test")]
    [InlineData("Media", "image.png", "/custom/media/image.png")]
    [InlineData("TempMedia", "temp123", "/custom/tempmedia/temp123")]
    public void UrlHelper_And_LinkGenerator_Support_Custom_BasePath(
        string action,
        string id,
        string expectedUrl)
    {
        // Arrange
        var customBasePath = "custom";
        var urlHelper = new TestUrlHelper(customBasePath);
        var linkGenerator = new TestLinkGenerator(customBasePath);
        
        var values = new RouteValueDictionary
        {
            ["action"] = action,
            ["controller"] = "Wiki",
            ["id"] = id
        };

        // Act
        var urlFromHelper = urlHelper.Action(new UrlActionContext
        {
            Action = action,
            Controller = "Wiki",
            Values = values
        });

        var urlFromGenerator = linkGenerator.GetPathByAddress(
            address: (object?)null,
            values: values);

        // Assert
        Assert.Equal(expectedUrl, urlFromHelper);
        Assert.Equal(expectedUrl, urlFromGenerator);
    }

    [Fact]
    public void UrlHelper_And_LinkGenerator_Handle_Empty_Id()
    {
        // Arrange
        var urlHelper = new TestUrlHelper();
        var linkGenerator = new TestLinkGenerator();
        
        var values = new RouteValueDictionary
        {
            ["action"] = "View",
            ["controller"] = "Wiki",
            ["id"] = ""
        };

        // Act
        var urlFromHelper = urlHelper.Action(new UrlActionContext
        {
            Action = "View",
            Controller = "Wiki",
            Values = values
        });

        var urlFromGenerator = linkGenerator.GetPathByAddress(
            address: (object?)null,
            values: values);

        // Assert
        Assert.Equal("/wiki/view/", urlFromHelper);
        Assert.Equal("/wiki/view/", urlFromGenerator);
    }

    [Fact]
    public void LinkGenerator_Returns_Null_For_Null_Values()
    {
        // Arrange
        var linkGenerator = new TestLinkGenerator();

        // Act
        var url = linkGenerator.GetPathByAddress(
            address: (object?)null,
            values: null!);

        // Assert
        Assert.Null(url);
    }

    [Theory]
    [InlineData("History", "page123", "/wiki/history/page123")]
    [InlineData("Edit", "docs/guide", "/wiki/edit/docs/guide")]
    [InlineData("Revision", "test", "/wiki/revision/test")]
    public void UrlHelper_Handles_Other_Actions_With_Fallback(
        string action,
        string id,
        string expectedUrl)
    {
        // Arrange
        var urlHelper = new TestUrlHelper();
        
        var values = new RouteValueDictionary
        {
            ["action"] = action,
            ["controller"] = "Wiki",
            ["id"] = id
        };

        // Act
        var url = urlHelper.Action(new UrlActionContext
        {
            Action = action,
            Controller = "Wiki",
            Values = values
        });

        // Assert
        Assert.Equal(expectedUrl, url);
    }

    [Fact]
    public void Both_Classes_Use_Default_BasePath()
    {
        // Arrange & Act
        var urlHelper = new TestUrlHelper();
        var linkGenerator = new TestLinkGenerator();

        // Assert - Both should use "wiki" as default base path
        var values = new RouteValueDictionary
        {
            ["action"] = "View",
            ["controller"] = "Wiki",
            ["id"] = "test"
        };

        var urlFromHelper = urlHelper.Action(new UrlActionContext
        {
            Action = "View",
            Controller = "Wiki",
            Values = values
        });

        var urlFromGenerator = linkGenerator.GetPathByAddress(
            address: (object?)null,
            values: values);

        Assert.StartsWith("/wiki/", urlFromHelper);
        Assert.StartsWith("/wiki/", urlFromGenerator);
    }
}
