using TraceAI.Api.Models;

namespace TraceAI.Api.Steps;

public interface IAgentStep
{
    string Name { get; }
    Task<StepResult> ExecuteAsync(ExecutionContext context, int retryCount, CancellationToken cancellationToken);
}
