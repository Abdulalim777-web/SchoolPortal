using Microsoft.AspNetCore.Identity;

namespace SchoolPortal.Models
{
    public class User : IdentityUser
    {
        public string? FullName { get; set; }
        
        // Store the role selected during registration
        // public string? Role { get; set; }
    }
}
