using System;

namespace SchoolPortal.Models
{
    public class AuditEntryViewModel
    {
        public int? Id { get; set; }
        // Source indicates whether this entry comes from LoginAudits or NavigationAudits
        public string? Source { get; set; }
        public DateTime EventAt { get; set; }
        public string Email { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Reason { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
