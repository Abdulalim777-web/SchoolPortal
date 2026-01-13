using System.ComponentModel.DataAnnotations;

namespace SchoolPortal.Models
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        // Role is not required for standard login; removed to avoid validation failures

        public bool RememberMe { get; set; }
    }
}
