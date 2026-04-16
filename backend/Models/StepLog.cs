namespace TraceAI.Api.Models;

public class StepLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskStepId { get; set; }
    public TaskStep? TaskStep { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
