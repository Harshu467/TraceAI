using TraceAI.Api.Models;
using TraceAI.Api.Services;

namespace TraceAI.Api.Steps;

public class ValidationStep(IAiService aiService) : IAgentStep
{
    public string Name => "ValidationStep";

    public async Task<StepResult> ExecuteAsync(ExecutionContext context, int retryCount, CancellationToken cancellationToken)
    {
        var code = context.StepOutputs.TryGetValue("CodeGenerationStep", out var generated) ? generated : "No generated code found.";
        var prompt = $"Validate the following output for correctness and possible issues. Return PASS/FAIL and reasons.\n\n{code}";

        try
        {
            var (output, raw) = await aiService.CompleteAsync(prompt, context.Model, cancellationToken);
            context.StepOutputs[Name] = output;

            return new StepResult
            {
                StepName = Name,
                Input = code,
                Output = output,
                Success = !output.Contains("FAIL", StringComparison.OrdinalIgnoreCase),
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
                Input = code,
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
