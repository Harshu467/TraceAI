import { useEffect, useRef, useState } from 'react';
import type { HubConnection } from '@microsoft/signalr';
import { TaskInput } from '../components/TaskInput';
import { StepViewer } from '../components/StepViewer';
import { executeTask, retryStep } from '../services/api';
import { connectToTask } from '../services/signalr';
import type { StepResult } from '../types/step';

type ThemeMode = 'light' | 'dark' | 'system';

export function WorkspacePage() {
  const [steps, setSteps] = useState<StepResult[]>([]);
  const [taskRunId, setTaskRunId] = useState<string | undefined>();
  const [isRunning, setIsRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [mode, setMode] = useState<ThemeMode>('system');
  const connectionRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', mode);
  }, [mode]);

  const handleExecute = async (prompt: string, model: string) => {
    setError(null);
    setIsRunning(true);
    setSteps([]);

    try {
      const response = await executeTask(prompt, model);
      setTaskRunId(response.taskRunId);
      setSteps(response.steps);

      connectionRef.current?.stop();
      connectionRef.current = await connectToTask(response.taskRunId, (step) => {
        setSteps((prev) => [...prev, step]);
      });
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setIsRunning(false);
    }
  };

  const handleRetry = async (stepName: string) => {
    if (!taskRunId) return;

    try {
      const result = await retryStep(taskRunId, stepName);
      setSteps((prev) => [...prev, result]);
    } catch (e) {
      setError((e as Error).message);
    }
  };

  const successfulSteps = steps.filter((step) => step.success).length;
  const failedSteps = steps.length - successfulSteps;
  const runStatus = isRunning ? 'Running' : failedSteps > 0 ? 'Needs Attention' : steps.length ? 'Completed' : 'Idle';

  return (
    <section className="workspace-shell">
      <div className="row compact-row">
        <h2>Workspace</h2>
        <select value={mode} onChange={(event) => setMode(event.target.value as ThemeMode)}>
          <option value="light">Light</option>
          <option value="dark">Dark</option>
          <option value="system">System</option>
        </select>
      </div>
      <div className="workspace-grid">
        <div className="stack">
          <section className="card hero-card">
            <p className="chip">AI Task Orchestrator</p>
            <h2>Professional trace-first workflow</h2>
            <p>Run engineering prompts with structured step visibility and retry only failed stages.</p>
          </section>
          <TaskInput onExecute={handleExecute} isRunning={isRunning} />
        </div>

        <section className="card run-summary">
          <h3>Run Overview</h3>
          <p className="muted">Task Run ID: {taskRunId ?? 'Not started'}</p>
          <p>Status: {runStatus}</p>
          <p>Total Steps: {steps.length}</p>
          <p>Successful: {successfulSteps}</p>
          <p>Failed: {failedSteps}</p>
        </section>
      </div>
      {error && <p className="error">{error}</p>}
      <StepViewer steps={steps} taskRunId={taskRunId} onRetry={handleRetry} />
    </section>
  );
}
