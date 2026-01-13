public class LoginAudit
{
    public int Id { get; set; }

    public string? UserId { get; set; }
    public string Email { get; set; } = null!;

    public bool IsSuccessful { get; set; }

    public string? FailureReason { get; set; }

    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
