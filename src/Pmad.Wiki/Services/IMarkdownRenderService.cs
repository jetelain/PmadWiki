namespace Pmad.Wiki.Services;

public interface IMarkdownRenderService
{
    string ToHtml(string markdown, string? culture = null);
}