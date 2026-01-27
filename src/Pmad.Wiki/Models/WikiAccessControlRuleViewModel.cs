namespace Pmad.Wiki.Models;

public class WikiAccessControlRuleViewModel
{
    public string Pattern { get; set; } = string.Empty;
    public string ReadGroups { get; set; } = string.Empty;
    public string WriteGroups { get; set; } = string.Empty;
    public int Order { get; set; }
}
