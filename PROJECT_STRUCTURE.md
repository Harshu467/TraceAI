# Generated Project Structure & Flow

## 1) Request flow (Controller → Execution Engine)
1. Frontend sends `POST /api/execute` with `prompt` and `model`.
2. `ExecutionController.Execute` validates the prompt and calls `IAgentExecutionEngine.ExecuteTaskAsync`.
3. `AgentExecutionEngine` creates a `TaskRun` row with status `Running`.
4. The engine builds an `ExecutionContext` and executes registered steps in order:
   - `PlanStep`
   - `CodeGenerationStep`
   - `ValidationStep`
5. After each step:
   - result is stored in DB (`Steps` table),
   - step update is sent through SignalR (`stepUpdated`).
6. If a step fails, task status becomes `Failed` and execution stops.
7. If all pass, task status becomes `Completed`.

Retry flow:
1. Frontend sends `POST /api/tasks/{taskRunId}/steps/{stepName}/retry`.
2. Engine reloads previous successful outputs into context.
3. Only the requested step runs again with incremented `retryCount`.
4. Result is persisted and pushed via SignalR.

## 2) Folder and file responsibilities

### Root
- `README.md`: setup, architecture, endpoints, and quick usage.

### `backend/`
- `Program.cs`: service registration, middleware, CORS, SignalR hub mapping, DB creation.
- `Controllers/ExecutionController.cs`: HTTP entrypoints for execute/retry/subscribe.
- `Engine/`
  - `IAgentExecutionEngine.cs`: engine contract.
  - `AgentExecutionEngine.cs`: orchestrates step execution order, persistence, and hub events.
- `Steps/`
  - `IAgentStep.cs`: step contract.
  - `PlanStep.cs`: generates a short plan from user prompt.
  - `CodeGenerationStep.cs`: generates improved code using plan output.
  - `ValidationStep.cs`: validates generated output and marks PASS/FAIL.
- `Services/`
  - `IAiService.cs`: AI abstraction.
  - `OpenAiService.cs`: actual OpenAI HTTP call implementation.
  - `IStepLogService.cs` / `StepLogService.cs`: persists step outputs and logs.
- `Models/`
  - Request/response DTOs (`ExecuteTaskRequest`, `ExecuteTaskResponse`).
  - Runtime context/result (`ExecutionContext`, `StepResult`).
  - DB entities (`TaskRun`, `TaskStep`, `StepLog`).
- `Data/TraceAiDbContext.cs`: EF Core schema and table relationships.
- `Hubs/ExecutionHub.cs`: SignalR group join/leave for per-task live updates.
- `Middleware/ErrorHandlingMiddleware.cs`: catches unhandled exceptions and returns JSON 500.
- `appsettings.json`: SQLite connection string and OpenAI config.

### `frontend/`
- `src/pages/App.tsx`: top-level state and orchestration (execute/retry + SignalR subscription).
- `src/components/TaskInput.tsx`: prompt/model form and execute button.
- `src/components/StepViewer.tsx`: renders trace cards and retry button for failed steps.
- `src/services/api.ts`: REST calls to backend execute/retry endpoints.
- `src/services/signalr.ts`: SignalR connection + group subscription.
- `src/types/step.ts`: shared response types used in UI.
- `src/main.tsx`, `src/styles.css`: app bootstrap and styling.

## 3) How steps execute internally
- Steps implement `IAgentStep.ExecuteAsync(context, retryCount, ct)`.
- Each step builds a prompt and calls `IAiService.CompleteAsync`.
- On success:
  - step output is saved into `ExecutionContext.StepOutputs` for downstream steps,
  - `StepResult.Success = true` (except validation, which checks output for `FAIL`).
- On exception:
  - step returns a failed `StepResult` with `Error` filled.
- Engine persists every `StepResult` to DB and streams it to clients.

## 4) Where AI API is called
- AI is called in `OpenAiService.CompleteAsync`.
- Endpoint used: `POST {BaseUrl}/chat/completions` (default `https://api.openai.com/v1/chat/completions`).
- This service is used by all steps through `IAiService`.

## 5) Potential issues / missing parts
1. **Frontend gets duplicate step entries**: execute API already returns all steps, then SignalR may push the same steps again.
2. **Retry does not update task status**: retry result is saved, but `TaskRun.Status` is not recalculated to `Completed`/`Failed`.
3. **No request model validation attributes** (`[Required]`, min length, etc.) beyond manual prompt empty-check.
4. **No auth/rate limiting** on execute/retry endpoints.
5. **No DB migrations workflow**: uses `EnsureCreated()` only, which is limited for schema evolution.
6. **Validation heuristic is brittle**: success is based on text containing `FAIL`.
7. **OpenAI response parsing assumes fixed JSON shape** and may throw if API format changes.
8. **Hard-coded frontend API URLs** (`http://localhost:5000`) rather than environment-based config.
9. **Unused log method**: `StepLogService.LogAsync` exists but is not used by engine/steps.
10. **No cancellation/timeout policy for AI calls** besides request token.
