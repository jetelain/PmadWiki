using Pmad.Wiki.Services;

namespace Pmad.Wiki.Models;

internal static class WikiGroupViewModelHelper
{
    internal static WikiGroupViewModel ResolveGroup(this Dictionary<string, IWikiUserGroup> groups, string name) =>
            groups.TryGetValue(name, out var g)
                ? new WikiGroupViewModel(name, g.Label, g.Description, g.Color)
                : new WikiGroupViewModel(name);
}
