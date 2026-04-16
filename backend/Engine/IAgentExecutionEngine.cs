using TraceAI.Api.Models;

namespace TraceAI.Api.Engine;

public interface IAgentExecutionEngine
{
    Task<ExecuteTaskResponse> ExecuteTaskAsync(ExecuteTaskRequest request, CancellationToken cancellationToken);
    Task<StepResult> RetryStepAsync(Guid taskRunId, string stepName, CancellationToken cancellationToken);
}
