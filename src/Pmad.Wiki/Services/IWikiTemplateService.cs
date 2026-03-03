using Pmad.Wiki.Models;

namespace Pmad.Wiki.Services;

/// <summary>
/// Defines the contract for managing wiki page templates.
/// </summary>
public interface IWikiTemplateService
{
    /// <summary>
    /// Gets all templates accessible to the specified user.
    /// </summary>
    /// <param name="wikiUser">The user requesting the templates.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The list of accessible templates.</returns>
    Task<List<WikiTemplate>> GetAllTemplatesAsync(IWikiUserWithPermissions wikiUser, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a specific template by its identifier, if accessible to the user.
    /// </summary>
    /// <param name="wikiUser">The user requesting the template.</param>
    /// <param name="templateId">The template identifier (page name).</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>The template, or <c>null</c> if not found or not accessible.</returns>
    Task<WikiTemplate?> GetTemplateAsync(IWikiUserWithPermissions wikiUser, string templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces date/time and custom parameter placeholder tokens in a pattern string using the provided timestamp.
    /// Supported placeholders: <c>{date}</c>, <c>{year}</c>, <c>{month}</c>, <c>{day}</c>, <c>{datetime}</c>, and custom parameters.
    /// </summary>
    /// <param name="pattern">The pattern string containing placeholders.</param>
    /// <param name="parameterValues">Dictionary of custom parameter values (key = parameter name, value = parameter value).</param>
    /// <param name="timestamp">The timestamp to use for date/time placeholders (e.g. browser-captured time).</param>
    /// <returns>The pattern with all placeholders replaced.</returns>
    string ResolvePlaceholders(string pattern, Dictionary<string, string>? parameterValues = null, DateTimeOffset? timestamp = null);
}
