namespace Pmad.Wiki
{
    public class WikiOptions
    {
        /// <summary>
        /// Gets or sets the absolute path containing all repositories. (to ensure compatibility with <see cref="Pmad.Git.HttpServer.GitSmartHttpOptions"/>).
        /// </summary>
        public string RepositoryRoot { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the repository used for wiki content.
        /// </summary>
        public string WikiRepositoryName { get; set; } = "wiki";

        /// <summary>
        /// Gets or sets the name of the git branch.
        /// </summary>
        public string BranchName { get; set; } = "main";

        /// <summary>
        /// Gets or sets a value indicating whether content can be viewed without authentication.
        /// </summary>
        public bool AllowAnonymousViewing { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether page-level permissions are supported.
        /// </summary>
        public bool UsePageLevelPermissions { get; set; } = true;

        /// <summary>
        /// Gets or sets the culture code of base markdown pages.
        /// </summary>
        public string NeutralMarkdownPageCulture { get; set; } = "en";
    }
}
