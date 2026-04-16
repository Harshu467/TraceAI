using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TraceAI.Api.Data;
using TraceAI.Api.Hubs;
using TraceAI.Api.Models;
using TraceAI.Api.Services;
using TraceAI.Api.Steps;

namespace TraceAI.Api.Engine;

public class AgentExecutionEngine(
    IEnumerable<IAgentStep> steps,
    TraceAiDbContext dbContext,
    IStepLogService logService,
    IHubContext<ExecutionHub> hubContext,
    ILogger<AgentExecutionEngine> logger) : IAgentExecutionEngine
{
    private readonly List<IAgentStep> _orderedSteps = steps.OrderBy(s => s.Order).ToList();

    public async Task<ExecuteTaskResponse> ExecuteTaskAsync(ExecuteTaskRequest request, CancellationToken cancellationToken)
    {
        var task = new TaskRun
        {
            Prompt = request.Prompt,
            Status = "Running",
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        var context = new ExecutionContext
        {
            TaskRunId = task.Id,
            UserPrompt = request.Prompt,
            Model = request.Model
        };

        var results = new List<StepResult>();

        foreach (var step in _orderedSteps)
        {
            logger.LogInformation("Executing step {StepName} for task {TaskRunId}.", step.Name, task.Id);

            var result = await ExecuteStepSafelyAsync(step, context, retryCount: 0, cancellationToken);
            results.Add(result);
            await logService.PersistStepResultAsync(task.Id, result, cancellationToken);
            await hubContext.Clients.Group(task.Id.ToString()).SendAsync("stepUpdated", result, cancellationToken);

            if (!result.Success)
            {
                logger.LogWarning("Execution stopped at step {StepName} for task {TaskRunId}. Error: {Error}", step.Name, task.Id, result.Error);
                task.Status = "Failed";
                await dbContext.SaveChangesAsync(cancellationToken);
                return new ExecuteTaskResponse { TaskRunId = task.Id, Steps = results };
            }
        }

        task.Status = "Completed";
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ExecuteTaskResponse
        {
            TaskRunId = task.Id,
            Steps = results
        };
    }

    public async Task<StepResult> RetryStepAsync(Guid taskRunId, string stepName, CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks.FindAsync([taskRunId], cancellationToken)
                   ?? throw new InvalidOperationException("Task not found.");

        var existingSteps = await dbContext.Steps.Where(s => s.TaskRunId == taskRunId).ToListAsync(cancellationToken);
        var step = _orderedSteps.FirstOrDefault(s => s.Name.Equals(stepName, StringComparison.OrdinalIgnoreCase))
                   ?? throw new InvalidOperationException($"Step '{stepName}' not found.");

        var context = new ExecutionContext
        {
            TaskRunId = taskRunId,
            UserPrompt = task.Prompt
        };

        foreach (var existing in existingSteps.Where(s => s.Success))
        {
            context.StepOutputs[existing.StepName] = existing.Output;
        }

        var retryCount = existingSteps.Count(s => s.StepName == step.Name);
        logger.LogInformation("Retrying step {StepName} for task {TaskRunId}. Retry count: {RetryCount}.", step.Name, taskRunId, retryCount);
        var retryResult = await ExecuteStepSafelyAsync(step, context, retryCount, cancellationToken);
        await logService.PersistStepResultAsync(taskRunId, retryResult, cancellationToken);
        await hubContext.Clients.Group(taskRunId.ToString()).SendAsync("stepUpdated", retryResult, cancellationToken);

        return retryResult;
    }

    private async Task<StepResult> ExecuteStepSafelyAsync(
        IAgentStep step,
        ExecutionContext context,
        int retryCount,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await step.ExecuteAsync(context, retryCount, cancellationToken);
            return NormalizeResult(result, step.Name, retryCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception while executing step {StepName}.", step.Name);
            return NormalizeResult(new StepResult
            {
                StepName = step.Name,
                Input = context.UserPrompt,
                Output = string.Empty,
                Success = false,
                Error = ex.Message,
                RetryCount = retryCount,
                TimestampUtc = DateTime.UtcNow
            }, step.Name, retryCount);
        }
    }

    private static StepResult NormalizeResult(StepResult result, string stepName, int retryCount)
    {
        result.StepName = string.IsNullOrWhiteSpace(result.StepName) ? stepName : result.StepName;
        result.Input ??= string.Empty;
        result.Output ??= string.Empty;
        result.Error ??= string.Empty;
        result.RetryCount = retryCount;
        result.TimestampUtc = result.TimestampUtc == default ? DateTime.UtcNow : result.TimestampUtc;
        return result;
    }
}
