namespace TraceAI.Api.Models;

public class StepResult
{
    public string StepName { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public string PromptUsed { get; set; } = string.Empty;
    public string RawAiResponse { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public DateTime Timestamp => TimestampUtc;
}
