using System.ComponentModel.DataAnnotations;

namespace Pmad.Wiki.Models;

public class WikiCreatePageViewModel
{
    public string? TemplateId { get; set; }

    public string? TemplateName { get; set; }

    public string? Culture { get; set; }

    /// <summary>
    /// Browser-captured ISO 8601 timestamp at the moment the user initiated page creation.
    /// Used to resolve date/time placeholders with the user's local time rather than server time.
    /// </summary>
    public string? BrowserTimestamp { get; set; }

    /// <summary>
    /// Page from which the "Create Page" button has been clicked. 
    /// This can be used to suggest a location for the new page if the template has not specified one. 
    /// Allows redirecting to the inital page in case of cancellation of page creation.
    /// </summary>
    public string? FromPage { get; set; }
            
    [RegularExpression(@"^[a-zA-Z0-9_/-]*$", ErrorMessage = "Location can only contain letters, numbers, hyphens, underscores, and forward slashes.")]
    public string? Location { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Page name is required.")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Page name can only contain letters, numbers, hyphens, and underscores.")]
    public required string PageName { get; set; }

    /// <summary>
    /// The values for custom template parameters (key = parameter name, value = user input).
    /// </summary>
    public Dictionary<string, string> ParameterValues { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// The template parameters definition (used to render the form).
    /// </summary>
    public List<WikiTemplateParameter> Parameters { get; set; } = new List<WikiTemplateParameter>();

    /// <summary>
    /// The raw location pattern from the template (with placeholders).
    /// </summary>
    public string? LocationPattern { get; set; }

    /// <summary>
    /// The raw page name pattern from the template (with placeholders).
    /// </summary>
    public string? PageNamePattern { get; set; }
}

