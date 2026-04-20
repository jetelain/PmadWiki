using Markdig;
using Pmad.Wiki.Models;

namespace Pmad.Wiki;

/// <summary>
/// Options specific to a wiki tenant.
/// </summary>
/// <remarks>
/// In a multi-tenant scenario, options from WikiGlobalOptions are copied to each tenant's WikiOptions instance. Values cannot be overriden by a tenant.
/// </remarks>
public class WikiOptions : WikiGlobalOptions
{
    /// <summary>
    /// Gets or sets the name of the repository used for wiki content.
    /// </summary>
    public string WikiRepositoryName { get; set; } = "wiki";

    /// <summary>
    /// Gets or sets the name of the git branch.
    /// </summary>
    public string BranchName { get; set; } = "main";

    /// <summary>
    /// Gets or sets a value indicating whether content can be viewed without authentication.
    /// </summary>
    public bool AllowAnonymousViewing { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether page-level permissions are supported.
    /// </summary>
    public bool UsePageLevelPermissions { get; set; } = true;

    /// <summary>
    /// Gets or sets the culture code of base markdown pages.
    /// </summary>
    public string NeutralMarkdownPageCulture { get; set; } = "en";

    /// <summary>
    /// Gets or sets the name of the wiki's home page.
    /// </summary>
    public string HomePageName { get; set; } = "Home";

    /// <summary>
    /// Gets or sets a delegate that customize the Markdown pipeline before processing content.
    /// </summary>
    public Action<MarkdownPipelineBuilder>? ConfigureMarkdown { get; set; }

    /// <summary>
    /// Gets or sets the layout page to use for wiki views.
    /// </summary>
    /// <remarks>
    /// If not set, the default layout from the host application's _ViewStart.cshtml will be used.
    /// </remarks>
    public string? Layout { get; set; }

    /// <summary>
    /// Gets or sets the default reveal.js theme for slide show pages.
    /// </summary>
    public string SlideShowDefaultTheme { get; set; } = Helpers.SlideShowHelper.DefaultTheme;

    /// <summary>
    /// Gets or sets the list of allowed reveal.js themes for slide show pages.
    /// Built-in reveal.js themes are served from <c>/lib/revealjs/theme/</c>. Custom themes must be placed under <c>/css/revealjs-custom-theme/</c>.
    /// </summary>
    public List<string> SlideShowAllowedThemes { get; set; } = [.. Helpers.SlideShowHelper.SlideShowThemesList];

    /// <summary>
    /// Gets or sets the list of allowed file extensions for media.
    /// </summary>
    /// <remarks>This property defines which file types are permitted for media content. Extensions should include the leading period (e.g., ".jpg").</remarks>
    public List<string> AllowedMediaExtensions { get; set; } = new()
    {
        ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp",
        ".mp4", ".webm", ".ogg",
        ".pdf"
    };

    /// <summary>
    /// Gets or sets the default configuration options for slideshow presentations using reveal.js.
    /// </summary>
    public RevealJsConfig SlideShowDefaultOptions { get; set; } = new RevealJsConfig();

    /// <summary>
    /// Checks if the given media path has an allowed file extension based on the <see cref="AllowedMediaExtensions"/> list.
    /// </summary>
    /// <param name="mediaPath"></param>
    /// <returns></returns>
    internal bool IsMediaPathExtensionAllowed(string mediaPath)
    {
        return AllowedMediaExtensions.Any(ext => mediaPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }
}
