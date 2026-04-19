namespace TraceAI.Api.Options;

public class OtpOptions
{
    public const string SectionName = "Otp";
    public int CodeLength { get; set; } = 6;
    public int ExpiryMinutes { get; set; } = 10;
    public int ResendCooldownSeconds { get; set; } = 60;
    public int MaxAttempts { get; set; } = 5;
}
