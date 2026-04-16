using TraceAI.Api.Data;
using TraceAI.Api.Models;

namespace TraceAI.Api.Services;

public class StepLogService(TraceAiDbContext dbContext) : IStepLogService
{
    public async Task LogAsync(Guid stepId, string message, CancellationToken cancellationToken)
    {
        dbContext.StepLogs.Add(new StepLog
        {
            TaskStepId = stepId,
            Message = message,
            TimestampUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task PersistStepResultAsync(Guid taskRunId, StepResult result, CancellationToken cancellationToken)
    {
        dbContext.Steps.Add(new TaskStep
        {
            TaskRunId = taskRunId,
            StepName = result.StepName,
            Input = result.Input,
            Output = result.Output,
            PromptUsed = result.PromptUsed,
            RawAiResponse = result.RawAiResponse,
            Success = result.Success,
            Error = result.Error,
            RetryCount = result.RetryCount,
            TimestampUtc = result.TimestampUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
