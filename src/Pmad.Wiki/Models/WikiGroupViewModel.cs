namespace Pmad.Wiki.Models;

public record WikiGroupViewModel(string Name, string? Label = null, string? Description = null)
{
    public string? Tooltip 
    {
        get 
        {
            if (!string.IsNullOrEmpty(Name))
            {
                if (!string.IsNullOrEmpty(Description))
                {
                    return $"{Name}: {Description}";
                }
                return Name;
            }
            return Description;
        }
    }

    public string ActualLabel => !string.IsNullOrEmpty(Label) ? Label : Name;
}
