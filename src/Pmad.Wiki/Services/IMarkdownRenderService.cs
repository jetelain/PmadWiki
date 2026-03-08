namespace Pmad.Wiki.Services;

/// <summary>
/// Defines the contract for rendering Markdown content to HTML.
/// </summary>
public interface IMarkdownRenderService
{
    /// <summary>
    /// Converts Markdown text to an HTML string.
    /// </summary>
    /// <param name="markdown">The Markdown source text to render.</param>
    /// <param name="culture">The optional culture code used for localisation of the rendered output.</param>
    /// <param name="currentPageName">The optional name of the current page, used to resolve relative links.</param>
    /// <returns>The rendered HTML string.</returns>
    string ToHtml(string markdown, string? culture = null, string? currentPageName = null);

    /// <summary>
    /// Converts Markdown text to a reveal.js slide show HTML structure, using <c>---</c> as slide separators.
    /// </summary>
    /// <param name="markdown">The Markdown source text to render.</param>
    /// <param name="culture">The optional culture code used for localisation of the rendered output.</param>
    /// <param name="currentPageName">The optional name of the current page, used to resolve relative links.</param>
    /// <returns>The rendered HTML string wrapping slides in a reveal.js-compatible structure.</returns>
    string ToHtmlSlideShow(string markdown, string? culture = null, string? currentPageName = null);
}