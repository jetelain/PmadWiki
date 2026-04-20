namespace Pmad.Wiki;

/// <summary>
/// Options commons to all tenants.
/// </summary>
/// <remarks>
/// This values are enforced to each tenant. They cannot be overridden by tenant-specific options.
/// </remarks>
public class WikiGlobalOptions
{
    /// <summary>
    /// Gets or sets the absolute path containing all repositories. (to ensure compatibility with <see cref="Pmad.Git.HttpServer.GitSmartHttpOptions"/>).
    /// </summary>
    public string RepositoryRoot { get; set; } = string.Empty;

    /// <summary>
    /// Copies all global properties onto <paramref name="target"/>, overriding any tenant-specific values
    /// that are reserved for global configuration.
    /// </summary>
    public void ApplyTo(WikiOptions target)
    {
        target.RepositoryRoot = RepositoryRoot;
    }
}
