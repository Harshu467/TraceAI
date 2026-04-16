namespace TraceAI.Api.Models;

public class ExecuteTaskRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
}
