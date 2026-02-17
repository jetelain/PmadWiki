using System.Text.RegularExpressions;
using Pmad.Wiki.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Pmad.Wiki.Helpers;

public static partial class WikiTemplateFrontMatterParser
{
    [GeneratedRegex("^\uFEFF?\\s*---\\s*\\r?\\n(.*?)\\r?\\n---\\s*\\r?\\n", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex FrontMatterRegex();

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Parses the front matter from raw content and returns a strongly-typed object.
    /// </summary>
    /// <param name="rawContent">The raw content containing potential front matter.</param>
    /// <returns>A tuple containing the parsed front matter and the remaining content.</returns>
    public static (WikiTemplateFrontMatter FrontMatter, string Content) Parse(string rawContent)
    {
        var frontMatter = new WikiTemplateFrontMatter();
        var content = rawContent;

        var match = FrontMatterRegex().Match(rawContent);
        if (match.Success)
        {
            var frontMatterText = match.Groups[1].Value;
            content = rawContent[match.Length..];

            try
            {
                // Deserialize YAML directly into the strongly-typed model
                var parsedFrontMatter = YamlDeserializer.Deserialize<WikiTemplateFrontMatter>(frontMatterText);
                if (parsedFrontMatter != null)
                {
                    frontMatter = parsedFrontMatter;
                    
                    // Convert empty strings to null for consistency
                    if (string.IsNullOrEmpty(frontMatter.Title)) frontMatter.Title = null;
                    if (string.IsNullOrEmpty(frontMatter.Description)) frontMatter.Description = null;
                    if (string.IsNullOrEmpty(frontMatter.Location)) frontMatter.Location = null;
                    if (string.IsNullOrEmpty(frontMatter.Pattern)) frontMatter.Pattern = null;
                }
            }
            catch
            {
                // If YAML parsing fails, return empty front matter but preserve content
                // This ensures backward compatibility if the front matter is malformed
            }
        }

        return (frontMatter, content);
    }
}
