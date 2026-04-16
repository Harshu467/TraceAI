# TraceAI
TraceAI is a developer-first AI execution engine that breaks tasks into traceable steps, enabling debugging, retry, and full visibility into AI reasoning and outputs.

## Core implementation

- `src/AgentEngine.ts`: Engine orchestrates sequential steps and stops on failure.
- `src/DefaultSteps.ts`: Modular `Plan`, `CodeGeneration`, and `Validation` steps with a simple default pipeline.
- `src/index.ts`: Re-exports engine types and default step helpers.
