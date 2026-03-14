namespace Pmad.Wiki.Models;

public class WikiPagePermissionSummaryViewModel
{
    public bool IsEnabled { get; set; }
    public WikiAccessControlRuleViewModel? EffectiveRule { get; set; }
}
