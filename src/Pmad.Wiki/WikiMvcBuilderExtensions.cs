using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Pmad.Wiki;

public static class WikiMvcBuilderExtensions
{
    internal static Assembly WikiAssembly
        => typeof(WikiMvcBuilderExtensions).Assembly;

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

    public static IMvcBuilder AddWikiGitHttpServer(this IMvcBuilder builder)
    {
        builder.Services.AddWikiGitHttpServer();

        return builder;
    }
}
