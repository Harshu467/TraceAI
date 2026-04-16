using TraceAI.Api.Models;

namespace TraceAI.Api.Services;

public interface IStepLogService
{
    Task LogAsync(Guid stepId, string message, CancellationToken cancellationToken);
    Task PersistStepResultAsync(Guid taskRunId, StepResult result, CancellationToken cancellationToken);
}
