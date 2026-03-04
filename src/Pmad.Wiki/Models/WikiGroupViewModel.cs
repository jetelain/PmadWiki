namespace Pmad.Wiki.Models;

public record WikiGroupViewModel(string Name, string Label, string? Description = null)
{
    public string? Tooltip =>
        Name != Label ? 
        string.IsNullOrEmpty(Description) ? Name : $"{Name}: {Description}"
        : Description;
}
