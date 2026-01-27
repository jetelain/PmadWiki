namespace Pmad.Wiki.Models;

public class WikiAccessControlViewModel
{
    public List<WikiAccessControlRuleViewModel> Rules { get; set; } = [];
    public string Content { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
