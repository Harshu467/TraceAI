namespace TraceAI.Api.Models;

public class TaskStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskRunId { get; set; }
    public TaskRun? TaskRun { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public string PromptUsed { get; set; } = string.Empty;
    public string RawAiResponse { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public ICollection<StepLog> Logs { get; set; } = new List<StepLog>();
}
