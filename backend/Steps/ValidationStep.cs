using TraceAI.Api.Models;
using TraceAI.Api.Services;

namespace TraceAI.Api.Steps;

public class ValidationStep(IAiService aiService, ILogger<ValidationStep> logger) : IAgentStep
{
    public string Name => "ValidationStep";
    public int Order => 3;

    public async Task<StepResult> ExecuteAsync(TraceAI.Api.Models.ExecutionContext context, int retryCount, CancellationToken cancellationToken)
    {
        var reasoning = "Validate the latest implementation and decide whether to fix or continue improving.";
        var code = context.StepOutputs.TryGetValue("CodeGenerationStep", out var generated) ? generated : "No generated code found.";
        var prompt = $"""
                     Validate this implementation. Return:
                     1) PASS or FAIL
                     2) concrete issues
                     3) recommended next action

                     Implementation:
                     {code}
                     """;

        try
        {
            var (output, raw) = await aiService.CompleteAsync(prompt, context.Model, cancellationToken);
            context.StepOutputs[Name] = output;
            var success = !output.Contains("FAIL", StringComparison.OrdinalIgnoreCase);

            return new StepResult
            {
                StepName = Name,
                Status = "Completed",
                Input = code,
                Output = output,
                Reasoning = reasoning,
                Decision = success ? "Validation passed; continue or finish" : "Validation failed; run FixIssuesStep",
                Success = success,
                RetryCount = retryCount,
                PromptUsed = prompt,
                RawAiResponse = raw,
                TimestampUtc = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{StepName} failed during execution.", Name);
            return new StepResult
            {
                StepName = Name,
                Status = "Failed",
                Input = code,
                Output = string.Empty,
                Reasoning = reasoning,
                Decision = "Retry validation once; fallback to fix step",
                Success = false,
                Error = ex.Message,
                RetryCount = retryCount,
                PromptUsed = prompt,
                RawAiResponse = string.Empty,
                TimestampUtc = DateTime.UtcNow
            };
        }
    }
}
