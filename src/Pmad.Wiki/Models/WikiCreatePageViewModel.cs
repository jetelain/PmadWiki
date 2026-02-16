using System.ComponentModel.DataAnnotations;

namespace Pmad.Wiki.Models;

public class WikiCreatePageViewModel
{
    public string? TemplateId { get; set; }
    
    public string? TemplateName { get; set; }
    
    public string? Culture { get; set; }

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
}

