using TraceAI.Api.Models;
using TraceAI.Api.Services;

namespace TraceAI.Api.Steps;

public class ImproveStructureStep(IAiService aiService, ILogger<ImproveStructureStep> logger) : IAgentStep
{
    public string Name => "ImproveStructureStep";
    public int Order => 6;

    public async Task<StepResult> ExecuteAsync(TraceAI.Api.Models.ExecutionContext context, int retryCount, CancellationToken cancellationToken)
    {
        var reasoning = "After a passing validation, improve clarity and structure without changing behavior.";
        var code = context.StepOutputs.TryGetValue("CodeGenerationStep", out var generated) ? generated : "No generated code found.";
        var prompt = $"""
                     Improve the code structure while preserving behavior.
                     Focus on modularity and readability.
                     Return improved code and a short summary of structural changes.

                     Current code:
                     {code}
                     """;

        try
        {
            var (output, raw) = await aiService.CompleteAsync(prompt, context.Model, cancellationToken);
            context.StepOutputs["CodeGenerationStep"] = output;
            context.StepOutputs[Name] = output;
            context.SharedContext["structureImproved"] = "true";

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
                Decision = "Skip structural improvements and complete with current version",
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
