using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Moq;
using Pmad.Wiki.Helpers;

namespace Pmad.Wiki.Test.Helpers;

public class TemplateParametersModelBinderTest
{
    private static ModelBindingContext CreateBindingContext(Dictionary<string, StringValues> queryParams)
    {
        var query = new QueryCollection(queryParams);
        var request = new Mock<HttpRequest>();
        request.Setup(r => r.Query).Returns(query);
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(c => c.Request).Returns(request.Object);
        var bindingContext = new Mock<ModelBindingContext>();
        bindingContext.Setup(c => c.HttpContext).Returns(httpContext.Object);
        bindingContext.SetupProperty(c => c.Result);
        return bindingContext.Object;
    }

    [Fact]
    public async Task BindModelAsync_WithPrefixedParameters_StripsPrefixAndReturnsValues()
    {
        var context = CreateBindingContext(new Dictionary<string, StringValues>
        {
            ["p_name"] = "Alice",
            ["p_age"] = "30"
        });

        await new TemplateParametersModelBinder().BindModelAsync(context);

        var result = Assert.IsType<Dictionary<string, string>>(context.Result.Model);
        Assert.Equal(2, result.Count);
        Assert.Equal("Alice", result["name"]);
        Assert.Equal("30", result["age"]);
    }

    [Fact]
    public async Task BindModelAsync_WithNoMatchingParameters_ReturnsEmptyDictionary()
    {
        var context = CreateBindingContext(new Dictionary<string, StringValues>
        {
            ["page"] = "1",
            ["sort"] = "asc"
        });

        await new TemplateParametersModelBinder().BindModelAsync(context);

        var result = Assert.IsType<Dictionary<string, string>>(context.Result.Model);
        Assert.Empty(result);
    }

    [Fact]
    public async Task BindModelAsync_WithMixedParameters_OnlyIncludesPrefixedOnes()
    {
        var context = CreateBindingContext(new Dictionary<string, StringValues>
        {
            ["p_title"] = "Hello",
            ["page"] = "2"
        });

        await new TemplateParametersModelBinder().BindModelAsync(context);

        var result = Assert.IsType<Dictionary<string, string>>(context.Result.Model);
        Assert.Single(result);
        Assert.Equal("Hello", result["title"]);
    }

    [Fact]
    public async Task BindModelAsync_WithEmptyQueryString_ReturnsEmptyDictionary()
    {
        var context = CreateBindingContext(new Dictionary<string, StringValues>());

        await new TemplateParametersModelBinder().BindModelAsync(context);

        var result = Assert.IsType<Dictionary<string, string>>(context.Result.Model);
        Assert.Empty(result);
    }

    [Fact]
    public async Task BindModelAsync_PrefixMatchIsCaseInsensitive()
    {
        var context = CreateBindingContext(new Dictionary<string, StringValues>
        {
            ["P_Title"] = "World",
            ["P_COUNT"] = "5"
        });

        await new TemplateParametersModelBinder().BindModelAsync(context);

        var result = Assert.IsType<Dictionary<string, string>>(context.Result.Model);
        Assert.Equal(2, result.Count);
        Assert.Equal("World", result["Title"]);
        Assert.Equal("5", result["COUNT"]);
    }

    [Fact]
    public async Task BindModelAsync_ResultDictionaryKeyLookupIsCaseInsensitive()
    {
        var context = CreateBindingContext(new Dictionary<string, StringValues>
        {
            ["p_Name"] = "Bob"
        });

        await new TemplateParametersModelBinder().BindModelAsync(context);

        var result = Assert.IsType<Dictionary<string, string>>(context.Result.Model);
        Assert.Equal("Bob", result["name"]);
        Assert.Equal("Bob", result["NAME"]);
        Assert.Equal("Bob", result["Name"]);
    }

    [Fact]
    public async Task BindModelAsync_SetsSuccessResult()
    {
        var context = CreateBindingContext(new Dictionary<string, StringValues>
        {
            ["p_x"] = "1"
        });

        await new TemplateParametersModelBinder().BindModelAsync(context);

        Assert.True(context.Result.IsModelSet);
    }
}
