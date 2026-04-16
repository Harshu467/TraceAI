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
    IHubContext<ExecutionHub> hubContext) : IAgentExecutionEngine
{
    private readonly List<IAgentStep> _orderedSteps = steps.OrderBy(s => s.Name switch
    {
        "PlanStep" => 1,
        "CodeGenerationStep" => 2,
        "ValidationStep" => 3,
        _ => 99
    }).ToList();

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
            var result = await step.ExecuteAsync(context, retryCount: 0, cancellationToken);
            results.Add(result);
            await logService.PersistStepResultAsync(task.Id, result, cancellationToken);

            await hubContext.Clients.Group(task.Id.ToString()).SendAsync("stepUpdated", result, cancellationToken);

            if (!result.Success)
            {
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
        var retryResult = await step.ExecuteAsync(context, retryCount, cancellationToken);
        await logService.PersistStepResultAsync(taskRunId, retryResult, cancellationToken);
        await hubContext.Clients.Group(taskRunId.ToString()).SendAsync("stepUpdated", retryResult, cancellationToken);

        return retryResult;
    }
}
