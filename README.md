# TraceAI
TraceAI is a developer-first AI execution engine that breaks tasks into traceable steps, enabling debugging, retry, and full visibility into AI reasoning and outputs.

## Core implementation

- `src/AgentEngine.ts`: Engine orchestrates sequential steps and stops on failure.
- `src/DefaultSteps.ts`: Modular `Plan`, `CodeGeneration`, and `Validation` steps with a simple default pipeline.
- `src/index.ts`: Re-exports engine types and default step helpers.

## Development

1. Install dependencies: `npm install`
2. Start development server: `npm run dev`

## Deployment

The project is configured for deployment using Docker or Vercel.

### Docker Deployment

Build and run with Docker:

```bash
docker build -t traceai .
docker run -p 4000:4000 traceai
```

Deploy to platforms like Railway, Render, or AWS using the Dockerfile.

### Vercel Deployment

The project includes Vercel configuration for serverless deployment:

- API routes in `api/` folder
- Frontend built with Webpack
- Static files served from `dist/public`

Push to GitHub and connect to Vercel for automatic deployment.
