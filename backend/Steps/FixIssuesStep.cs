using TraceAI.Api.Models;
using TraceAI.Api.Services;

namespace TraceAI.Api.Steps;

public class FixIssuesStep(IAiService aiService, ILogger<FixIssuesStep> logger) : IAgentStep
{
    public string Name => "FixIssuesStep";
    public int Order => 5;

    public async Task<StepResult> ExecuteAsync(TraceAI.Api.Models.ExecutionContext context, int retryCount, CancellationToken cancellationToken)
    {
        var reasoning = "Validation or generation found issues; attempt targeted fixes using prior outputs.";
        var currentCode = context.StepOutputs.TryGetValue("CodeGenerationStep", out var generated) ? generated : "No generated code found.";
        var validation = context.StepOutputs.TryGetValue("ValidationStep", out var validationText) ? validationText : "No validation feedback available.";
        var prompt = $"""
                     Fix the implementation using the validation findings.
                     Keep good parts, patch weak parts, and return revised code + concise change log.

                     Current implementation:
                     {currentCode}

                     Validation findings:
                     {validation}
                     """;

        try
        {
            var (output, raw) = await aiService.CompleteAsync(prompt, context.Model, cancellationToken);
            context.StepOutputs["CodeGenerationStep"] = output;
            context.StepOutputs[Name] = output;

            return new StepResult
            {
                StepName = Name,
                Status = "Completed",
                Input = validation,
                Output = output,
                Reasoning = reasoning,
                Decision = "Next: ValidationStep",
                Success = true,
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
                Input = validation,
                Output = string.Empty,
                Reasoning = reasoning,
                Decision = "Retry fix with stricter patch-only prompt",
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
