using TraceAI.Api.Models;
using TraceAI.Api.Services;

namespace TraceAI.Api.Steps;

public class PlanStep(IAiService aiService) : IAgentStep
{
    public string Name => "PlanStep";

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
