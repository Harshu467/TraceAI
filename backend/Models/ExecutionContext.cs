namespace TraceAI.Api.Models;

public class ExecutionContext
{
    public Guid TaskRunId { get; set; }
    public string UserPrompt { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public Dictionary<string, string> StepOutputs { get; set; } = new();
    public Dictionary<string, string> SharedContext { get; set; } = new();
    public List<StepResult> History { get; set; } = new();

    public string BuildHistorySummary()
    {
        if (History.Count == 0)
        {
            return "No previous steps.";
        }

        return string.Join(
            "\n\n",
            History.Select(s =>
                $"Step: {s.StepName}\nStatus: {(s.Success ? "Success" : "Failed")}\nReasoning: {s.Reasoning}\nDecision: {s.Decision}\nOutput: {s.Output}"));
    }
}
