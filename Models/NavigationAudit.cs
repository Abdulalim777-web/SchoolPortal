using System;

namespace SchoolPortal.Models
{
    public class NavigationAudit
    {
        public int Id { get; set; }

        public string? UserId { get; set; }
        public string? Email { get; set; }

        public string Url { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty; // Accessed, Denied, etc.

        public DateTime EventAt { get; set; } = DateTime.UtcNow;

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
