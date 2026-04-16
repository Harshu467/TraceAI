using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using TraceAI.Data.Hubs;

namespace TraceAI.Data.Services
{
    public interface IStepUpdateService
    {
        Task NotifyStepStartedAsync(int stepId, string stepName);
        Task NotifyStepCompletedAsync(int stepId, string stepName, bool success);
        Task NotifyStepFailedAsync(int stepId, string stepName, string error);
        Task NotifyExecutionStartedAsync(string executionId, string prompt);
        Task NotifyExecutionCompletedAsync(string executionId, bool success);
    }

    public sealed class StepUpdateService : IStepUpdateService
    {
        private readonly IHubContext<StepUpdateHub> _hubContext;

        public StepUpdateService(IHubContext<StepUpdateHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyStepStartedAsync(int stepId, string stepName)
        {
            await _hubContext.Clients.All.SendAsync("StepStarted", stepId, stepName);
        }

        public async Task NotifyStepCompletedAsync(int stepId, string stepName, bool success)
        {
            await _hubContext.Clients.All.SendAsync("StepCompleted", stepId, stepName, success);
        }

        public async Task NotifyStepFailedAsync(int stepId, string stepName, string error)
        {
            await _hubContext.Clients.All.SendAsync("StepFailed", stepId, stepName, error);
        }

        public async Task NotifyExecutionStartedAsync(string executionId, string prompt)
        {
            await _hubContext.Clients.All.SendAsync("ExecutionStarted", executionId, prompt);
        }

        public async Task NotifyExecutionCompletedAsync(string executionId, bool success)
        {
            await _hubContext.Clients.All.SendAsync("ExecutionCompleted", executionId, success);
        }
    }
}
