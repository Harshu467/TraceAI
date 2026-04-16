using TraceAI.Api.Models;
using TraceAI.Api.Services;

namespace TraceAI.Api.Steps;

public class ExplainCodeStep(IAiService aiService, ILogger<ExplainCodeStep> logger) : IAgentStep
{
    public string Name => "ExplainCodeStep";
    public int Order => 4;

    public async Task<StepResult> ExecuteAsync(TraceAI.Api.Models.ExecutionContext context, int retryCount, CancellationToken cancellationToken)
    {
        var reasoning = "Explain generated code so the next fix/improvement decisions are informed.";
        var code = context.StepOutputs.TryGetValue("CodeGenerationStep", out var generated) ? generated : "No generated code found.";
        var prompt = $"""
                     Explain this implementation in practical terms:
                     - what it does
                     - why key choices were made
                     - risks or weak points

                     Implementation:
                     {code}
                     """;

        try
        {
            var (output, raw) = await aiService.CompleteAsync(prompt, context.Model, cancellationToken);
            context.StepOutputs[Name] = output;

            return new StepResult
            {
                StepName = Name,
                Status = "Completed",
                Input = code,
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
                Input = code,
                Output = string.Empty,
                Reasoning = reasoning,
                Decision = "Skip explanation and proceed to validation",
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
