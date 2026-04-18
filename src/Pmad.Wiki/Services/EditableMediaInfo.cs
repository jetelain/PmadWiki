namespace Pmad.Wiki.Services;

public class EditableMediaInfo
{
    /// <summary>Gets or sets a value indicating whether the temporary file is editable.</summary>
    public bool IsEditable { get; set; } = true;

    /// <summary>Gets or sets the initial Git path of the temporary file (if media is editable and was previously tracked in Git).</summary>
    public string? InitialGitPath { get; set; }
}
