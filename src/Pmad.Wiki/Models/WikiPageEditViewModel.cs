using System.ComponentModel.DataAnnotations;

namespace Pmad.Wiki.Models;

public class WikiPageEditViewModel
{
    public List<WikiPageLink> Breadcrumb { get; } = new();

    public required string PageName { get; set; }
    
    [Required]
    public required string Content { get; set; }
    
    [Required]
    public required string CommitMessage { get; set; }
    
    public string? Culture { get; set; }
    
    public bool IsNew { get; set; }
    
    public string? OriginalContentHash { get; set; }
}
