using YamlDotNet.Serialization;

namespace Pmad.Wiki.Models;

/// <summary>
/// Represents a custom parameter for a wiki template.
/// </summary>
public class WikiTemplateParameter
{
    /// <summary>
    /// Gets or sets the parameter name (used as placeholder key).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the parameter type.
    /// </summary>
    public WikiTemplateParameterType Type { get; set; } = WikiTemplateParameterType.Text;

    /// <summary>
    /// Gets or sets the display label for the parameter input field.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the default value for the parameter.
    /// </summary>
    [YamlMember(Alias = "default")]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the help text shown below the input field.
    /// </summary>
    [YamlMember(Alias = "help")]
    public string? HelpText { get; set; }

    /// <summary>
    /// Gets or sets whether this parameter is required.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the list of allowed values for an <see cref="WikiTemplateParameterType.Enum"/> parameter.
    /// </summary>
    public List<string>? Options { get; set; }
}
