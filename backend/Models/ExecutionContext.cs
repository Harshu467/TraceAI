namespace TraceAI.Api.Models;

public class ExecutionContext
{
    public Guid TaskRunId { get; set; }
    public string UserPrompt { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public Dictionary<string, string> StepOutputs { get; set; } = new();
}
