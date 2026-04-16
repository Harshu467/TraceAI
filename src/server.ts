import http from 'http';
import { AIService } from './AIService';
import { AgentEngine } from './AgentEngine';
import { createDefaultSteps } from './DefaultSteps';
import { TraceController } from './TraceController';

const PORT = process.env.PORT ? Number(process.env.PORT) : 4000;

const aiService = new AIService();
const agentEngine = new AgentEngine(createDefaultSteps(aiService));
const traceController = new TraceController(agentEngine);

const sendJson = (response: http.ServerResponse, status: number, payload: unknown) => {
  const body = JSON.stringify(payload);
  response.writeHead(status, {
    'Content-Type': 'application/json',
    'Content-Length': Buffer.byteLength(body, 'utf8'),
    'Access-Control-Allow-Origin': '*',
    'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type',
  });
  response.end(body);
};

const parseJsonBody = (request: http.IncomingMessage): Promise<any> =>
  new Promise((resolve, reject) => {
    const chunks: Uint8Array[] = [];
    request.on('data', (chunk) => chunks.push(chunk));
    request.on('end', () => {
      try {
        const raw = Buffer.concat(chunks).toString('utf8');
        resolve(raw ? JSON.parse(raw) : {});
      } catch (error) {
        reject(error);
      }
    });
    request.on('error', reject);
  });

const server = http.createServer(async (req, res) => {
  // Handle CORS preflight
  if (req.method === 'OPTIONS') {
    res.writeHead(200, {
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
      'Access-Control-Allow-Headers': 'Content-Type',
    });
    res.end();
    return;
  }

  // Handle POST /execute
  if (req.method === 'POST' && req.url === '/execute') {
    try {
      const body = await parseJsonBody(req);
      const result = await traceController.execute(body);
      return sendJson(res, result.success ? 200 : 400, result);
    } catch (error) {
      return sendJson(res, 500, {
        success: false,
        stepResults: [],
        error: error instanceof Error ? error.message : String(error),
      });
    }
  }

  // Handle POST /retry
  if (req.method === 'POST' && req.url === '/retry') {
    try {
      const body = await parseJsonBody(req);
      const result = await traceController.retry(body);
      return sendJson(res, result.success ? 200 : 400, result);
    } catch (error) {
      return sendJson(res, 500, {
        success: false,
        stepResults: [],
        error: error instanceof Error ? error.message : String(error),
      });
    }
  }

  // 404 for unrecognized routes
  return sendJson(res, 404, { success: false, error: 'Not found' });
});

server.listen(PORT, () => {
  // eslint-disable-next-line no-console
  console.log(`TraceAI server listening on http://localhost:${PORT}`);
  // eslint-disable-next-line no-console
  console.log(`Available endpoints: POST /execute, POST /retry`);
});
