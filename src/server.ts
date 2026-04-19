import express from 'express';
import cors from 'cors';
import path from 'path';
import { AIService } from './AIService';
import { AgentEngine } from './AgentEngine';
import { createDefaultSteps } from './DefaultSteps';
import { TraceController } from './TraceController';

const PORT = process.env.PORT ? Number(process.env.PORT) : 4000;

const aiService = new AIService();
const agentEngine = new AgentEngine(createDefaultSteps(aiService));
const traceController = new TraceController(agentEngine);

const app = express();
app.use(cors());
app.use(express.json());

// Serve static files from the React app build directory
app.use(express.static(path.join(__dirname, '../public')));

// API routes
app.post('/api/execute', async (req, res) => {
  try {
    const result = await traceController.execute(req.body);
    res.status(result.success ? 200 : 400).json(result);
  } catch (error) {
    res.status(500).json({
      success: false,
      stepResults: [],
      error: error instanceof Error ? error.message : String(error),
    });
  }
});

app.post('/api/retry', async (req, res) => {
  try {
    const result = await traceController.retry(req.body);
    res.status(result.success ? 200 : 400).json(result);
  } catch (error) {
    res.status(500).json({
      success: false,
      stepResults: [],
      error: error instanceof Error ? error.message : String(error),
    });
  }
});

// Catch all handler: send back React's index.html file for client-side routing
app.get('*', (req, res) => {
  res.sendFile(path.join(__dirname, '../public/index.html'));
});

app.listen(PORT, () => {
  // eslint-disable-next-line no-console
  console.log(`TraceAI server listening on http://localhost:${PORT}`);
  // eslint-disable-next-line no-console
  console.log(`Available endpoints: POST /execute, POST /retry`);
});
