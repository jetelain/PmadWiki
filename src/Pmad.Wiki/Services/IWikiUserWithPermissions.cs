namespace Pmad.Wiki.Services
{
    public interface IWikiUserWithPermissions
    {
        /// <summary>
        /// Gets the user associated with the current wiki context.
        /// </summary>
        IWikiUser User { get; }

        /// <summary>
        /// Gets the list of group names associated with the current user.
        /// </summary>
        string[] Groups { get; }

        /// <summary>
        /// Gets a value indicating whether the current user has permission to edit the wiki.
        /// </summary>
        bool CanEdit { get; }

        /// <summary>
        /// Gets a value indicating whether the current user has permission to view the wiki.
        /// Used only if the wiki has no anonymous viewing permission.
        /// </summary>
        bool CanView { get; }

        /// <summary>
        /// Gets a value indicating whether the current user has administrative permissions.
        /// </summary>
        bool CanAdmin { get; }

        /// <summary>
        /// Gets a value indicating whether remote git operations are allowed for the current user.
        /// This means that this user can see or edit any page of the wiki, regardless of page-level permissions.
        /// </summary>
        bool CanRemoteGit { get; }
    }
}