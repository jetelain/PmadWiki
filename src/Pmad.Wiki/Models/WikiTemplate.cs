namespace Pmad.Wiki.Models;

public class WikiTemplate
{
    public required string TemplateName { get; set; }
    
    public required string Content { get; set; }
    
    public string? DefaultLocation { get; set; }
    
    public string? NamePattern { get; set; }
    
    public string? Description { get; set; }
    
    public string? DisplayName { get; set; }
}
