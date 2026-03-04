namespace Pmad.Wiki.Models;

public class WikiAccessControlRuleViewModel
{
    public string Pattern { get; set; } = string.Empty;
    public List<WikiGroupViewModel> ReadGroups { get; set; } = [];
    public List<WikiGroupViewModel> WriteGroups { get; set; } = [];
    public int Order { get; set; }
}
