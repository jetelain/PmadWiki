using Pmad.Wiki.Models;

namespace Pmad.Wiki.Services;

public interface IWikiTemplateService
{
    Task<List<WikiTemplate>> GetAllTemplatesAsync(IWikiUserWithPermissions wikiUser, CancellationToken cancellationToken = default);
    
    Task<WikiTemplate?> GetTemplateAsync(IWikiUserWithPermissions wikiUser, string templateId, CancellationToken cancellationToken = default);
    
    string ResolvePlaceHolders(string pattern);
}
