using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Pmad.Wiki;

/// <summary>
/// Extension methods for <see cref="IMvcBuilder"/> to register wiki services and Razor views.
/// </summary>
public static class WikiMvcBuilderExtensions
{
    internal static Assembly WikiAssembly
        => typeof(WikiMvcBuilderExtensions).Assembly;

    /// <summary>
    /// Adds the wiki to the MVC builder, registering all required services and the compiled Razor views.
    /// </summary>
    /// <param name="builder">The MVC builder.</param>
    /// <param name="options">A delegate to configure <see cref="WikiOptions"/>.</param>
    /// <returns>The MVC builder for chaining.</returns>
    public static IMvcBuilder AddWiki(this IMvcBuilder builder, Action<WikiOptions> options)
    {
        builder.Services.AddWiki(options);

        builder.AddViewLocalization();

        builder.ConfigureApplicationPartManager(apm =>
        {
            apm.ApplicationParts.Add(new CompiledRazorAssemblyPart(WikiAssembly));
        });

        return builder;
    }

    /// <summary>
    /// Adds the Git HTTP server to the MVC builder, enabling remote Git access to the wiki repository.
    /// </summary>
    /// <param name="builder">The MVC builder.</param>
    /// <returns>The MVC builder for chaining.</returns>
    public static IMvcBuilder AddWikiGitHttpServer(this IMvcBuilder builder)
    {
        builder.Services.AddWikiGitHttpServer();

        return builder;
    }
}
