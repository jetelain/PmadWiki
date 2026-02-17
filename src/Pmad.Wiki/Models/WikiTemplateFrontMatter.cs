namespace Pmad.Wiki.Models;

/// <summary>
/// Represents the front matter metadata for a wiki template page.
/// </summary>
public class WikiTemplateFrontMatter
{
    /// <summary>
    /// Gets or sets the display title of the template.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the description of the template.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the default location pattern for pages created from this template.
    /// Supports placeholders like {date}, {year}, {month}, {day}, {datetime}.
    /// Example: "blog/posts"
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the name pattern for pages created from this template.
    /// Supports placeholders like {date}, {year}, {month}, {day}, {datetime}.
    /// Example: "blog/{year}/{month}/{day}"
    /// </summary>
    public string? Pattern { get; set; }
}


