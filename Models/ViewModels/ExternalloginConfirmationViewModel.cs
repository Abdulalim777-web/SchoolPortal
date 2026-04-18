using System.ComponentModel.DataAnnotations;

namespace SchoolPortal.Models.ViewModels
{
    /// <summary>
    /// Shown to first-time Google users so they can confirm their name and pick a role.
    /// </summary>
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "I am a")]
        public string SelectedRole { get; set; } = "Student";
    }
}
