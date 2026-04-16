using TraceAI.Api.Models;
using TraceAI.Api.Services;

namespace TraceAI.Api.Steps;

public class CodeGenerationStep(IAiService aiService) : IAgentStep
{
    public string Name => "CodeGenerationStep";

    public async Task<StepResult> ExecuteAsync(ExecutionContext context, int retryCount, CancellationToken cancellationToken)
    {
        var plan = context.StepOutputs.TryGetValue("PlanStep", out var planned) ? planned : "No prior plan available.";
        var prompt = $"Using this plan:\n{plan}\n\nGenerate improved code and explain key changes for this request:\n{context.UserPrompt}";

        try
        {
            var (output, raw) = await aiService.CompleteAsync(prompt, context.Model, cancellationToken);
            context.StepOutputs[Name] = output;

            return new StepResult
            {
                StepName = Name,
                Input = plan,
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
                Input = plan,
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
