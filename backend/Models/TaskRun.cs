namespace TraceAI.Api.Models;

public class TaskRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Prompt { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    public ICollection<TaskStep> Steps { get; set; } = new List<TaskStep>();
}
