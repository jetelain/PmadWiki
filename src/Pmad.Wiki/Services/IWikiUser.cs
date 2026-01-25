namespace Pmad.Wiki.Services;

public interface IWikiUser
{
    /// <summary>
    /// Email address associated with the wiki user, used for Git commits.
    /// </summary>
    /// <remarks>
    /// This should be a randomly generated email but fixed value per user to ensure that Git commits are properly attributed and keep real email addresses private.
    /// Wiki will not display this value, it is only used for Git commit metadata.
    /// </remarks>
    string GitEmail { get; }

    /// <summary>
    /// User full name associated with the wiki user, used for Git commits.
    /// </summary>
    /// <remarks>
    /// This value is used to attribute Git commits made by this user. Changes will not affect previous commits.
    /// Wiki will not display this value, it is only used for Git commit metadata.
    /// </remarks>
    string GitName { get; }

    /// <summary>
    /// Name used for displaying the user in the wiki UI.
    /// </summary>
    string DisplayName { get; }   
}