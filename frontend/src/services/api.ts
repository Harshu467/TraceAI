import type { ExecuteResponse, StepResult } from '../types/step';

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000/api';

export async function executeTask(prompt: string, model: string): Promise<ExecuteResponse> {
  const response = await fetch(`${API_BASE}/execute`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ prompt, model })
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json();
}

export async function retryStep(taskRunId: string, stepName: string): Promise<StepResult> {
  const response = await fetch(`${API_BASE}/tasks/${taskRunId}/steps/${stepName}/retry`, {
    method: 'POST'
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json();
}
