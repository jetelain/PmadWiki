using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Pmad.Wiki;

public static class WikiMvcBuilderExtensions
{
    internal static Assembly WikiAssembly
        => typeof(WikiMvcBuilderExtensions).Assembly;

    public static IMvcBuilder AddWiki(this IMvcBuilder builder, WikiOptions options)
    {
        builder.Services.AddWiki(options);

        builder.ConfigureApplicationPartManager(apm =>
        {
            apm.ApplicationParts.Add(new AssemblyPart(WikiAssembly));
        });

        return builder;
    }
}
