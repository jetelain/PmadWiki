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
        
    /// <summary>
    /// Comma-separated list of temporary media IDs that were uploaded during editing.
    /// These will be cleared from the temporary storage after the page is saved, and 
    /// any media files that are still referenced in the content will be moved to permanent storage.
    /// </summary>
    public string? TemporaryMediaIds { get; set; }
}
