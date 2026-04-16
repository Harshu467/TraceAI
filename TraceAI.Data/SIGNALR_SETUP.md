# SignalR Real-Time Updates

This document explains how to use the SignalR hub for real-time step execution updates.

## Hub Endpoint

- **URL**: `/hubs/stepupdate`
- **Hub Class**: `StepUpdateHub`

## Setup in ASP.NET Program.cs

```csharp
using TraceAI.Data;
using TraceAI.Data.Configuration;
using TraceAI.Data.Hubs;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Add SignalR services
builder.Services.AddSignalRServices();
builder.Services.AddSignalR();

var app = builder.Build();

// Map the hub endpoint
app.MapHub<StepUpdateHub>("/hubs/stepupdate");

app.Run();
```

## Client Events

### StepStarted
Sent when a step begins execution.
```javascript
connection.on("StepStarted", (stepId, stepName) => {
  console.log(`Step ${stepName} started`);
});
```

### StepCompleted
Sent when a step completes successfully.
```javascript
connection.on("StepCompleted", (stepId, stepName, success) => {
  console.log(`Step ${stepName} completed: ${success}`);
});
```

### StepFailed
Sent when a step fails.
```javascript
connection.on("StepFailed", (stepId, stepName, error) => {
  console.error(`Step ${stepName} failed: ${error}`);
});
```

### ExecutionStarted
Sent when execution begins.
```javascript
connection.on("ExecutionStarted", (executionId, prompt) => {
  console.log(`Execution ${executionId} started`);
});
```

### ExecutionCompleted
Sent when execution finishes.
```javascript
connection.on("ExecutionCompleted", (executionId, success) => {
  console.log(`Execution ${executionId} completed: ${success}`);
});
```

## Service Usage

Inject `IStepUpdateService` to send updates:

```csharp
public class MyService
{
    private readonly IStepUpdateService _updateService;

    public MyService(IStepUpdateService updateService)
    {
        _updateService = updateService;
    }

    public async Task RunStepAsync()
    {
        await _updateService.NotifyStepStartedAsync(1, "CodeGeneration");
        // ... execute step ...
        await _updateService.NotifyStepCompletedAsync(1, "CodeGeneration", true);
    }
}
```
