using System.ComponentModel.DataAnnotations;

namespace Pmad.Wiki.Models;

public class WikiCreatePageRequest
{
    [Required]
    public required string PageName { get; set; }
    
    public string? Culture { get; set; }
    
    public string? TemplateId { get; set; }
}
