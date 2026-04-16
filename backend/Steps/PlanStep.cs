using TraceAI.Api.Models;
using TraceAI.Api.Services;

namespace TraceAI.Api.Steps;

public class PlanStep(IAiService aiService, ILogger<PlanStep> logger) : IAgentStep
{
    public string Name => "PlanStep";
    public int Order => 1;

    public async Task<StepResult> ExecuteAsync(TraceAI.Api.Models.ExecutionContext context, int retryCount, CancellationToken cancellationToken)
    {
        var reasoning = "I need an actionable plan before code work starts.";
        var prompt = $"""
                     Create a concise plan for this request.
                     Use 4-6 numbered steps.
                     Focus on generation, explanation, fixing issues, improvements, and testing.

                     User request:
                     {context.UserPrompt}
                     """;
        try
        {
            var (output, raw) = await aiService.CompleteAsync(prompt, context.Model, cancellationToken);
            context.StepOutputs[Name] = output;
            context.SharedContext["currentPlan"] = output;

            return new StepResult
            {
                StepName = Name,
                Status = "Completed",
                Input = context.UserPrompt,
                Output = output,
                Reasoning = reasoning,
                Decision = "Next: CodeGenerationStep",
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
                Input = context.UserPrompt,
                Output = string.Empty,
                Reasoning = reasoning,
                Decision = "Fallback to direct generation if planning keeps failing",
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
