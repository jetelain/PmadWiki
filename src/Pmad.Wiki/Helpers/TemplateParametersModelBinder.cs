using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Pmad.Wiki.Helpers;

/// <summary>
/// Binds query string parameters prefixed with "p_" into a <see cref="Dictionary{String, String}"/>,
/// stripping the prefix from the resulting keys.
/// </summary>
public sealed class TemplateParametersModelBinder : IModelBinder
{
    internal const string ParameterPrefix = "p_";

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in bindingContext.HttpContext.Request.Query)
        {
            if (key.StartsWith(ParameterPrefix, StringComparison.OrdinalIgnoreCase))
            {
                result[key[ParameterPrefix.Length..]] = value.ToString();
            }
        }
        bindingContext.Result = ModelBindingResult.Success(result);
        return Task.CompletedTask;
    }
}
