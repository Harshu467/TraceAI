# TraceAI MVP

TraceAI is a debuggable AI execution engine that breaks AI tasks into explicit steps, stores each step's traces, and supports step-level retries.

## Stack
- Backend: .NET 8 Web API
- Frontend: React + TypeScript (Vite)
- AI: OpenAI Chat Completions API
- Database: SQLite with EF Core
- Realtime: SignalR

## Architecture
Backend layers implemented:
1. Controller Layer (`Controllers/ExecutionController.cs`)
2. Agent Execution Engine (`Engine/AgentExecutionEngine.cs`)
3. Step System (`Steps/PlanStep.cs`, `CodeGenerationStep.cs`, `ValidationStep.cs`)
4. AI Service Layer (`Services/OpenAiService.cs`)
5. Logging & Debug Layer (`Services/StepLogService.cs`, `Models/StepResult.cs`)
6. Database Layer (`Data/TraceAiDbContext.cs`)

## Backend setup
```bash
cd backend
dotnet restore
dotnet run
```

API base: `https://localhost:5001` or `http://localhost:5000` (depending on local profile).

### Required configuration
Set OpenAI API key in `backend/appsettings.json` or environment variable mapping:
```json
"OpenAI": {
  "ApiKey": "YOUR_KEY"
}
```

## Frontend setup
```bash
cd frontend
npm install
cp .env.example .env # optional override
npm run dev
```

Frontend URL: `http://localhost:5173`

### Frontend environment variables
Create `frontend/.env` (or pass envs via your deploy platform):
```bash
VITE_API_BASE_URL=http://localhost:5000/api
VITE_HUB_URL=http://localhost:5000/hubs/execution
```

## Docker Compose (full stack)
From repository root:
```bash
cp .env.example .env
# edit .env and set OPENAI_API_KEY
docker compose up --build
```

Services:
- Frontend: `http://localhost:5173`
- Backend API: `http://localhost:5000`

## Production notes
- Backend CORS origins are configurable through `Cors:AllowedOrigins` (or `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ... environment variables).
- Prefer setting `OpenAI__ApiKey` via environment variables instead of committing secrets.

## API endpoints
### Execute task
`POST /api/execute`

Request:
```json
{
  "prompt": "Fix this C# function...",
  "model": "gpt-4o-mini"
}
```

Response:
```json
{
  "taskRunId": "GUID",
  "steps": [
    {
      "stepName": "PlanStep",
      "input": "...",
      "output": "...",
      "success": true,
      "error": "",
      "retryCount": 0,
      "promptUsed": "...",
      "rawAiResponse": "...",
      "timestampUtc": "2026-01-01T00:00:00Z"
    }
  ]
}
```

### Retry a failed step
`POST /api/tasks/{taskRunId}/steps/{stepName}/retry`

### SignalR hub
`/hubs/execution`
- client invokes: `JoinTaskGroup(taskRunId)`
- server event: `stepUpdated`

## Database schema
- `Tasks`
- `Steps`
- `StepLogs`

SQLite file is created automatically as `backend/traceai.db`.

## Sample cURL
```bash
curl -X POST http://localhost:5000/api/execute \
  -H "Content-Type: application/json" \
  -d '{"prompt":"Fix this C# function to avoid null reference exceptions.","model":"gpt-4o-mini"}'
```

## Notes
- Retry history is persisted through repeated `Steps` records with incremented `RetryCount`.
- Prompt transparency is included in each step (`PromptUsed`, `RawAiResponse`).
- Includes global error middleware (`Middleware/ErrorHandlingMiddleware.cs`).
