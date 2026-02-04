using Markdig;

namespace Pmad.Wiki;

public class WikiOptions
{
    /// <summary>
    /// Gets or sets the absolute path containing all repositories. (to ensure compatibility with <see cref="Pmad.Git.HttpServer.GitSmartHttpOptions"/>).
    /// </summary>
    public string RepositoryRoot { get; set; } = string.Empty;

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
    /// Gets or sets the list of allowed file extensions for media.
    /// </summary>
    /// <remarks>This property defines which file types are permitted for media content. Extensions should include the leading period (e.g., ".jpg").</remarks>
    public List<string> AllowedMediaExtensions { get; set; } = new()
    {
        ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".mp4", ".webm", ".ogg", ".pdf"
    };
}
