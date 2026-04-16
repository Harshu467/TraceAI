using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace TraceAI.Data.Hubs
{
    public sealed class StepUpdateHub : Hub
    {
        public async Task NotifyStepStarted(int stepId, string stepName)
        {
            await Clients.All.SendAsync("StepStarted", stepId, stepName);
        }

        public async Task NotifyStepCompleted(int stepId, string stepName, bool success)
        {
            await Clients.All.SendAsync("StepCompleted", stepId, stepName, success);
        }

        public async Task NotifyStepFailed(int stepId, string stepName, string error)
        {
            await Clients.All.SendAsync("StepFailed", stepId, stepName, error);
        }

        public async Task NotifyExecutionStarted(string executionId, string prompt)
        {
            await Clients.All.SendAsync("ExecutionStarted", executionId, prompt);
        }

        public async Task NotifyExecutionCompleted(string executionId, bool success)
        {
            await Clients.All.SendAsync("ExecutionCompleted", executionId, success);
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
            await base.OnConnectedAsync();
        }
    }
}
