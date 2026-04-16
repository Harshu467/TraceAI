using Microsoft.AspNetCore.SignalR;

namespace TraceAI.Api.Hubs;

public class ExecutionHub : Hub
{
    public Task JoinTaskGroup(string taskRunId) => Groups.AddToGroupAsync(Context.ConnectionId, taskRunId);
    public Task LeaveTaskGroup(string taskRunId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, taskRunId);
}
