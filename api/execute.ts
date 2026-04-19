import { AIService } from '../src/AIService';
import { AgentEngine } from '../src/AgentEngine';
import { createDefaultSteps } from '../src/DefaultSteps';
import { TraceController } from '../src/TraceController';

const aiService = new AIService();
const agentEngine = new AgentEngine(createDefaultSteps(aiService));
const traceController = new TraceController(agentEngine);

export default async function handler(req, res) {
  if (req.method !== 'POST') {
    res.status(405).json({ error: 'Method not allowed' });
    return;
  }

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
}