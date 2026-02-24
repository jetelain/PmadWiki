using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Pmad.Wiki.Helpers;

internal static partial class WikiFrontMatterParser
{
    [GeneratedRegex("^\uFEFF?\\s*---\\s*\\r?\\n(.*?)\\r?\\n---\\s*\\r?\\n", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex FrontMatterRegex();

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    internal static (T FrontMatter, string Content) Parse<T>(string rawContent) where T : new()
    {
        var frontMatter = new T();
        var content = rawContent;

        var match = FrontMatterRegex().Match(rawContent);
        if (match.Success)
        {
            var frontMatterText = match.Groups[1].Value;
            content = rawContent[match.Length..];

            try
            {
                var parsed = YamlDeserializer.Deserialize<T>(frontMatterText);
                if (parsed != null)
                {
                    frontMatter = parsed;
                }
            }
            catch
            {
                // If YAML parsing fails, return empty front matter but preserve content
            }
        }

        return (frontMatter, content);
    }
}
