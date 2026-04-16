using TraceAI.Api.Models;
using TraceAI.Api.Services;

namespace TraceAI.Api.Steps;

public class CodeGenerationStep(IAiService aiService, ILogger<CodeGenerationStep> logger) : IAgentStep
{
    public string Name => "CodeGenerationStep";
    public int Order => 2;

    public async Task<StepResult> ExecuteAsync(ExecutionContext context, int retryCount, CancellationToken cancellationToken)
    {
        var reasoning = "Generate a first working version using all known context.";
        var plan = context.StepOutputs.TryGetValue("PlanStep", out var planned) ? planned : "No prior plan available.";
        var retryHint = context.SharedContext.TryGetValue("retryHint", out var hint) ? hint : string.Empty;
        var prompt = $"""
                     Use the following plan and history to generate or revise code.
                     
                     Plan:
                     {plan}
                     
                     Previous execution history:
                     {context.BuildHistorySummary()}
                     
                     Additional retry hint:
                     {retryHint}
                     
                     Request:
                     {context.UserPrompt}
                     """;

        try
        {
            var (output, raw) = await aiService.CompleteAsync(prompt, context.Model, cancellationToken);
            context.StepOutputs[Name] = output;

            return new StepResult
            {
                StepName = Name,
                Status = "Completed",
                Input = plan,
                Output = output,
                Reasoning = reasoning,
                Decision = "Next: ExplainCodeStep",
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
                Input = plan,
                Output = string.Empty,
                Reasoning = reasoning,
                Decision = "Retry generation with a narrower prompt",
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
