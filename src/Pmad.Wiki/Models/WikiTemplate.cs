namespace Pmad.Wiki.Models;

/// <summary>
/// Represents a wiki page template that can be used as a starting point when creating new pages.
/// </summary>
public class WikiTemplate
{
    /// <summary>Gets or sets the internal page name (path) of the template within the wiki.</summary>
    public required string TemplateName { get; set; }
    
    /// <summary>Gets or sets the raw Markdown content of the template.</summary>
    public required string Content { get; set; }

    /// <summary>
    /// Gets or sets the default location prefix for pages created from this template, or <c>null</c> if none.
    /// Supports date/time placeholders such as <c>{date}</c>, <c>{year}</c>, <c>{month}</c>, <c>{day}</c>, and <c>{datetime}</c>.
    /// </summary>
    public string? DefaultLocation { get; set; }
    
    /// <summary>
    /// Gets or sets the name pattern for pages created from this template, or <c>null</c> if none.
    /// Supports date/time placeholders such as <c>{date}</c>, <c>{year}</c>, <c>{month}</c>, <c>{day}</c>, and <c>{datetime}</c>.
    /// </summary>
    public string? NamePattern { get; set; }
    
    /// <summary>Gets or sets a short description of the template shown in the UI, or <c>null</c> if none.</summary>
    public string? Description { get; set; }
    
    /// <summary>Gets or sets the human-readable display name of the template shown in the UI, or <c>null</c> to fall back to <see cref="TemplateName"/>.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the custom parameters for this template.</summary>
    public List<WikiTemplateParameter> Parameters { get; set; } = new List<WikiTemplateParameter>();
}
