import { useRef, useState } from 'react';
import type { HubConnection } from '@microsoft/signalr';
import { TaskInput } from '../components/TaskInput';
import { StepViewer } from '../components/StepViewer';
import { executeTask, retryStep } from '../services/api';
import { connectToTask } from '../services/signalr';
import type { StepResult } from '../types/step';

export function App() {
  const [steps, setSteps] = useState<StepResult[]>([]);
  const [taskRunId, setTaskRunId] = useState<string | undefined>();
  const [isRunning, setIsRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const connectionRef = useRef<HubConnection | null>(null);

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

  return (
    <main className="layout">
      <h1>TraceAI - Debuggable AI Execution Engine</h1>
      {error && <p className="error">{error}</p>}
      <TaskInput onExecute={handleExecute} isRunning={isRunning} />
      <StepViewer steps={steps} taskRunId={taskRunId} onRetry={handleRetry} />
    </main>
  );
}
