using Microsoft.Extensions.DependencyInjection;
using TraceAI.Data.Services;

namespace TraceAI.Data.Configuration
{
    public static class SignalRConfiguration
    {
        public static void AddSignalRServices(this IServiceCollection services)
        {
            services.AddSignalR();
            services.AddScoped<IStepUpdateService, StepUpdateService>();
        }
    }
}
