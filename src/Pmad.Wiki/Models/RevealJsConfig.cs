using System.Text.Json.Serialization;

namespace Pmad.Wiki.Models;

/// <summary>
/// Configuration object for reveal.js.
/// </summary>
/// <remarks>
/// Includes only a subset of reveal.js configuration options that are relevant for our use case.
/// </remarks>
public class RevealJsConfig
{
    /// <summary>
	/// Flags if the presentation is running in an embedded mode,
	/// i.e. contained within a limited portion of the screen
    /// </summary>
    /// <remarks>
    /// Should remains true, as wiki will always embed the presentation within a page.
    /// </remarks>
    [JsonPropertyName("embedded")]
    public bool Embedded { get; set; } = true;

    /// <summary>
    /// Width of the "normal" size of the presentation, aspect ratio will be preserved
    /// when the presentation is scaled to fit different resolutions
    /// </summary>
    /// <remarks>
    /// Wiki style sheet assumes an aspect ratio of 16:9 (e.g. 1920x1080), which is the most common aspect ratio for presentations.
    /// </remarks>
    [JsonPropertyName("width")]
    public int Width { get; set; } = 1920;

    /// <summary>
    /// height of the "normal" size of the presentation, aspect ratio will be preserved
    /// when the presentation is scaled to fit different resolutions
    /// </summary>
    /// <remarks>
    /// Wiki style sheet assumes an aspect ratio of 16:9 (e.g. 1920x1080), which is the most common aspect ratio for presentations.
    /// </remarks>
    [JsonPropertyName("height")]
    public int Height { get; set; } = 1080;
}
