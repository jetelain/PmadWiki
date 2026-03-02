using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;

namespace Pmad.Wiki;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/> to configure the wiki middleware.
/// </summary>
public static class WikiApplicationBuilderExtensions
{
    /// <summary>
    /// Gets a <see cref="ManifestEmbeddedFileProvider"/> that serves the wiki's embedded static files from its <c>wwwroot</c> directory.
    /// </summary>
    public static ManifestEmbeddedFileProvider WikiStaticFiles
        => new ManifestEmbeddedFileProvider(WikiMvcBuilderExtensions.WikiAssembly, "wwwroot");

    /// <summary>
    /// Adds middleware to serve the wiki's embedded static files (JavaScript, CSS, and other assets).
    /// </summary>
    /// <param name="app">The application builder.</param>
    public static void UseWikiStaticFiles(this IApplicationBuilder app)
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = WikiStaticFiles
        });
    }
}
