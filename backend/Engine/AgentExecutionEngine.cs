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
    private const int MaxAutoRetriesPerStep = 1;
    private const int MaxExecutionSteps = 12;
    private readonly Dictionary<string, IAgentStep> _stepsByName = steps.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

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

        var context = new TraceAI.Api.Models.ExecutionContext
        {
            TaskRunId = task.Id,
            UserPrompt = request.Prompt,
            Model = request.Model
        };

        var results = new List<StepResult>();
        var retryTracker = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var nextStepName = "PlanStep";

        while (!string.IsNullOrWhiteSpace(nextStepName) && results.Count < MaxExecutionSteps)
        {
            if (!_stepsByName.TryGetValue(nextStepName, out var step))
            {
                logger.LogWarning("Requested next step {StepName} does not exist.", nextStepName);
                break;
            }

            retryTracker.TryGetValue(step.Name, out var retryCount);
            logger.LogInformation("Executing step {StepName} for task {TaskRunId}. Retry count: {RetryCount}", step.Name, task.Id, retryCount);

            var result = await ExecuteStepSafelyAsync(step, context, retryCount, cancellationToken);
            results.Add(result);
            context.History.Add(result);

            await logService.PersistStepResultAsync(task.Id, result, cancellationToken);
            await hubContext.Clients.Group(task.Id.ToString()).SendAsync("stepUpdated", result, cancellationToken);

            if (!result.Success)
            {
                var failureHandling = HandleFailureDecision(step.Name, retryCount);
                result.Decision = failureHandling.Decision;

                if (failureHandling.ShouldRetry)
                {
                    retryTracker[step.Name] = retryCount + 1;
                    context.SharedContext["retryHint"] = failureHandling.RetryHint;
                    nextStepName = step.Name;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(failureHandling.FallbackStep))
                {
                    nextStepName = failureHandling.FallbackStep;
                    continue;
                }

                task.Status = "Failed";
                await dbContext.SaveChangesAsync(cancellationToken);
                return new ExecuteTaskResponse { TaskRunId = task.Id, Steps = results };
            }

            context.SharedContext.Remove("retryHint");
            retryTracker[step.Name] = 0;
            nextStepName = DecideNextStep(step.Name, result, context);
        }

        task.Status = results.Any(r => !r.Success) ? "Failed" : "Completed";
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

        var existingSteps = await dbContext.Steps
            .Where(s => s.TaskRunId == taskRunId)
            .OrderBy(s => s.TimestampUtc)
            .ToListAsync(cancellationToken);

        if (!_stepsByName.TryGetValue(stepName, out var step))
        {
            throw new InvalidOperationException($"Step '{stepName}' not found.");
        }

        var context = new TraceAI.Api.Models.ExecutionContext
        {
            TaskRunId = taskRunId,
            UserPrompt = task.Prompt
        };

        foreach (var existing in existingSteps.Where(s => s.Success))
        {
            context.StepOutputs[existing.StepName] = existing.Output;
            context.History.Add(new StepResult
            {
                StepName = existing.StepName,
                Output = existing.Output,
                Success = existing.Success,
                Error = existing.Error,
                TimestampUtc = existing.TimestampUtc
            });
        }

        var retryCount = existingSteps.Count(s => s.StepName == step.Name);
        context.SharedContext["retryHint"] = "Manual retry requested by user. Focus on fixing previous error.";

        logger.LogInformation("Retrying step {StepName} for task {TaskRunId}. Retry count: {RetryCount}.", step.Name, taskRunId, retryCount);

        var retryResult = await ExecuteStepSafelyAsync(step, context, retryCount, cancellationToken);
        context.History.Add(retryResult);
        await logService.PersistStepResultAsync(taskRunId, retryResult, cancellationToken);
        await hubContext.Clients.Group(taskRunId.ToString()).SendAsync("stepUpdated", retryResult, cancellationToken);

        return retryResult;
    }

    private (bool ShouldRetry, string RetryHint, string? FallbackStep, string Decision) HandleFailureDecision(
        string stepName,
        int retryCount)
    {
        if (retryCount < MaxAutoRetriesPerStep)
        {
            return (
                ShouldRetry: true,
                RetryHint: $"Step {stepName} failed previously. Use a simpler and more explicit response format.",
                FallbackStep: null,
                Decision: $"Retrying {stepName} with modified prompt.");
        }

        return stepName switch
        {
            "PlanStep" => (
                ShouldRetry: false,
                RetryHint: string.Empty,
                FallbackStep: "CodeGenerationStep",
                Decision: "Planning still failed. Fallback: continue directly to code generation."),
            "ExplainCodeStep" => (
                ShouldRetry: false,
                RetryHint: string.Empty,
                FallbackStep: "ValidationStep",
                Decision: "Explanation failed. Fallback: validate current implementation."),
            "ImproveStructureStep" => (
                ShouldRetry: false,
                RetryHint: string.Empty,
                FallbackStep: null,
                Decision: "Improvement failed. Fallback: finish with current validated implementation."),
            _ => (
                ShouldRetry: false,
                RetryHint: string.Empty,
                FallbackStep: null,
                Decision: $"{stepName} failed after retry. Ending execution.")
        };
    }

    private static string? DecideNextStep(string stepName, StepResult result, TraceAI.Api.Models.ExecutionContext context)
    {
        if (!result.Success)
        {
            return null;
        }

        return stepName switch
        {
            "PlanStep" => "CodeGenerationStep",
            "CodeGenerationStep" => "ExplainCodeStep",
            "ExplainCodeStep" => "ValidationStep",
            "FixIssuesStep" => "ValidationStep",
            "ImproveStructureStep" => "ValidationStep",
            "ValidationStep" when result.Output.Contains("FAIL", StringComparison.OrdinalIgnoreCase) => "FixIssuesStep",
            "ValidationStep" when !context.SharedContext.ContainsKey("structureImproved") => "ImproveStructureStep",
            "ValidationStep" => null,
            _ => null
        };
    }

    private async Task<StepResult> ExecuteStepSafelyAsync(
        IAgentStep step,
        TraceAI.Api.Models.ExecutionContext context,
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
                Status = "Failed",
                Input = context.UserPrompt,
                Output = string.Empty,
                Reasoning = $"{step.Name} threw an unhandled exception.",
                Decision = "Engine will apply retry/fallback policy.",
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
        result.Status = string.IsNullOrWhiteSpace(result.Status)
            ? (result.Success ? "Completed" : "Failed")
            : result.Status;
        result.Input ??= string.Empty;
        result.Output ??= string.Empty;
        result.Reasoning ??= string.Empty;
        result.Decision ??= string.Empty;
        result.Error ??= string.Empty;
        result.RetryCount = retryCount;
        result.TimestampUtc = result.TimestampUtc == default ? DateTime.UtcNow : result.TimestampUtc;
        return result;
    }
}
