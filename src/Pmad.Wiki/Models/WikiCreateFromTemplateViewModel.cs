namespace Pmad.Wiki.Models;

public class WikiCreateFromTemplateViewModel
{
    public List<WikiTemplate> Templates { get; set; } = new();
    
    public string? Culture { get; set; }

    /// <summary>
    /// Page from which the "Create Page" button has been clicked. 
    /// This can be used to suggest a location for the new page if the template has not specified one. 
    /// Allows redirecting to the inital page in case of cancellation of page creation.
    /// </summary>
    public string? FromPage { get; set; }
}
