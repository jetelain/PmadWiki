namespace Pmad.Wiki.Services;

/// <summary>
/// Defines the contract for a user group within a wiki, providing access to the group's name, display label, and
/// description.
/// </summary>
public interface IWikiUserGroup
{
    /// <summary>
    /// Gets the unique name of the user group. This is typically used for internal identification and permission checks.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the label associated with the user group, which may be null if no label is set.
    /// </summary>
    string? Label { get; }

    /// <summary>
    /// Gets the description of the user group, which may be null if no description is set.
    /// </summary>
    /// <remarks>The description provides additional context about the user group, such as its purpose, permissions, or any other relevant 
    /// information that can help users understand the role of the group within the wiki system. This can be particularly useful for 
    /// administrators when managing groups and assigning permissions.
    /// </remarks>
    string? Description { get; }
}