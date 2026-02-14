using System.Text;
using Pmad.Git.LocalRepositories;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Helpers;

/// <summary>
/// Helpers methods to implement <see cref="Pmad.Wiki.Services.IWikiUserService"/> and <see cref="Pmad.Wiki.Services.IWikiUser"/>.
/// </summary>
public static class WikiUserHelper
{
    /// <summary>
    /// Generates a unique email address in the format required for Git user configuration.
    /// </summary>
    /// <remarks>Each call returns a different email address using a new GUID.</remarks>
    /// <returns>A string containing a newly generated email address in the form "{guid}@pmadwiki.local".</returns>
    public static string GenerateUniqueGitEmail()
    {
        return $"{Guid.NewGuid():N}@pmadwiki.local";
    }

    /// <summary>
    /// Generates a unique email address based on an external identifier.
    /// </summary>
    /// <param name="externalIdentifier">The external identifier to generate the email from.</param>
    /// <remarks>This allows to avoid persisting a mapping database between an external system and the local system.</remarks>
    /// <returns>A string containing a unique email address derived from the external identifier.</returns>
    public static string GenerateGitEmailFromExternalIdentifier(string externalIdentifier)
    {
        var sha256Hash = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(externalIdentifier));
        var name = Convert.ToHexString(sha256Hash).ToLowerInvariant();
        return $"{name}@pmadwiki.local";
    }

    /// <summary>
    /// Replace invalid characters for Git user name or email by an underscore to avoid Git errors when using external identifiers as user name or email.
    /// </summary>
    /// <param name="userName">The user name or email to sanitize.</param>
    /// <returns>A string with invalid characters replaced by underscores.</returns>
    public static string SanitizeGitNameOrEmail(string userName)
    {
        var sanitized = new StringBuilder(userName);
        foreach (var c in GitCommitSignature.GetInvalidCharacters())
        {
            sanitized.Replace(c, '_');
        }
        return sanitized.ToString();
    }

    internal static GitCommitSignature CreateGitCommitSignature(IWikiUser author)
    {
        // Certain characters in user names and emails could break Git signature, so we sanitize
        // them to avoid errors when using external identifiers as user name or email.

        // We do not trust IWikiUser implementations to sanitize the user name and email,
        // so we sanitize them here to ensure that Git operations will not fail.

        return new GitCommitSignature(
            SanitizeGitNameOrEmail(author.GitName),
            SanitizeGitNameOrEmail(author.GitEmail),
            DateTimeOffset.UtcNow);
    }
}
