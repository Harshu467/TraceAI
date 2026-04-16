using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TraceAI.Api.Engine;
using TraceAI.Api.Hubs;
using TraceAI.Api.Models;

namespace TraceAI.Api.Controllers;

[ApiController]
[Route("api")]
public class ExecutionController(IAgentExecutionEngine executionEngine, IHubContext<ExecutionHub> hubContext) : ControllerBase
{
    [HttpPost("execute")]
    public async Task<ActionResult<ExecuteTaskResponse>> Execute([FromBody] ExecuteTaskRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest("Prompt is required.");
        }

        var result = await executionEngine.ExecuteTaskAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("tasks/{taskRunId:guid}/steps/{stepName}/retry")]
    public async Task<ActionResult<StepResult>> RetryStep(Guid taskRunId, string stepName, CancellationToken cancellationToken)
    {
        var result = await executionEngine.RetryStepAsync(taskRunId, stepName, cancellationToken);
        return Ok(result);
    }

    [HttpPost("tasks/{taskRunId:guid}/subscribe")]
    public async Task<IActionResult> Subscribe(Guid taskRunId)
    {
        // This endpoint exists for a simple frontend handshake; actual SignalR group join happens over hub invocation.
        await hubContext.Clients.Group(taskRunId.ToString()).SendAsync("subscribed", taskRunId);
        return Ok();
    }
}
