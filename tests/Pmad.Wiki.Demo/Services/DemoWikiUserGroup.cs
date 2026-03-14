using Pmad.Wiki.Models;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Demo.Services;

public record DemoWikiUserGroup(string Name, string? Label, string? Description, WikiColor Color) : IWikiUserGroup;
