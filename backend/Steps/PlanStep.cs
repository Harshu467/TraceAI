using TraceAI.Api.Models;
using TraceAI.Api.Services;

namespace TraceAI.Api.Steps;

public class PlanStep(IAiService aiService, ILogger<PlanStep> logger) : IAgentStep
{
    public string Name => "PlanStep";
    public int Order => 1;

    public async Task<StepResult> ExecuteAsync(ExecutionContext context, int retryCount, CancellationToken cancellationToken)
    {
        var prompt = $"Break this user request into a short execution plan with numbered steps:\n{context.UserPrompt}";
        try
        {
            var (output, raw) = await aiService.CompleteAsync(prompt, context.Model, cancellationToken);
            context.StepOutputs[Name] = output;

            return new StepResult
            {
                StepName = Name,
                Input = context.UserPrompt,
                Output = output,
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
                Input = context.UserPrompt,
                Output = string.Empty,
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
