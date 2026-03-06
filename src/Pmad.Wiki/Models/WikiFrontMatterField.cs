namespace Pmad.Wiki.Models;

public enum WikiFrontMatterFieldType
{
    Text,
    Number,
    Checkbox
}

public class WikiFrontMatterField
{
    public required string Key { get; set; }

    public required string Label { get; set; }

    public WikiFrontMatterFieldType Type { get; set; }

    public string? HelpText { get; set; }

    /// <summary>
    /// Key of another <see cref="WikiFrontMatterFieldType.Checkbox"/> field that must be checked
    /// for this field to be enabled.
    /// </summary>
    public string? DependsOn { get; set; }
}
