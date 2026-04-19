namespace TraceAI.Api.Models;

public class PhoneOtpCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime ResendAvailableAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; }
    public bool Verified { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
