using System.ComponentModel.DataAnnotations;

namespace Pmad.Wiki.Models;

public class WikiAccessControlEditViewModel
{
    [Required]
    public string Content { get; set; } = string.Empty;

    [Required]
    public string CommitMessage { get; set; } = "Update access control rules";
}
