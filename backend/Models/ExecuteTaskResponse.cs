namespace TraceAI.Api.Models;

public class ExecuteTaskResponse
{
    public Guid TaskRunId { get; set; }
    public List<StepResult> Steps { get; set; } = new();
}
