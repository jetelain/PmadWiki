using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;

namespace Pmad.Wiki
{
    public static class WikiApplicationBuilderExtensions
    {
        public static ManifestEmbeddedFileProvider WikiStaticFiles
            => new ManifestEmbeddedFileProvider(WikiMvcBuilderExtensions.WikiAssembly, "wwwroot");

        public static void UseWikiStaticFiles(this IApplicationBuilder app)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = WikiStaticFiles
            });
        }
    }
}
