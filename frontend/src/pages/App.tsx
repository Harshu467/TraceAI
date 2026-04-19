import { useEffect, useRef, useState } from 'react';
import type { HubConnection } from '@microsoft/signalr';
import { TaskInput } from '../components/TaskInput';
import { StepViewer } from '../components/StepViewer';
import { executeTask, retryStep } from '../services/api';
import { connectToTask } from '../services/signalr';
import type { StepResult } from '../types/step';

type Page = 'workspace' | 'settings' | 'environment';
type ThemeMode = 'light' | 'dark' | 'system';

export function App() {
  const [steps, setSteps] = useState<StepResult[]>([]);
  const [taskRunId, setTaskRunId] = useState<string | undefined>();
  const [isRunning, setIsRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [activePage, setActivePage] = useState<Page>('workspace');
  const [mode, setMode] = useState<ThemeMode>('system');
  const [language, setLanguage] = useState('English');
  const connectionRef = useRef<HubConnection | null>(null);
  const envInfo = [
    { key: 'VITE_API_BASE_URL', value: import.meta.env.VITE_API_BASE_URL ?? '(default) http://localhost:5000/api' },
    { key: 'VITE_HUB_URL', value: import.meta.env.VITE_HUB_URL ?? '(default) http://localhost:5000/hubs/execution' },
    { key: 'MODE', value: import.meta.env.MODE }
  ];

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
  const lastStep = steps.at(-1);
  const runStatus = isRunning ? 'Running' : failedSteps > 0 ? 'Needs Attention' : steps.length ? 'Completed' : 'Idle';

  const renderContent = () => {
    if (activePage === 'workspace') {
      return (
        <section className="workspace-shell">
          <div className="workspace-grid">
            <div className="stack">
              <section className="card hero-card">
                <p className="chip">AI Task Orchestrator</p>
                <h2>Professional trace-first workflow</h2>
                <p>
                  Run engineering prompts with structured step visibility. Track execution quality in real time and retry
                  only the failed stages when needed.
                </p>
              </section>
              <TaskInput onExecute={handleExecute} isRunning={isRunning} />
            </div>

            <section className="card run-summary">
              <h3>Run Overview</h3>
              <p className="muted">Task Run ID: {taskRunId ?? 'Not started'}</p>

              <div className="stats-grid">
                <article>
                  <h4>Status</h4>
                  <p>{runStatus}</p>
                </article>
                <article>
                  <h4>Total Steps</h4>
                  <p>{steps.length}</p>
                </article>
                <article>
                  <h4>Successful</h4>
                  <p>{successfulSteps}</p>
                </article>
                <article>
                  <h4>Failed</h4>
                  <p>{failedSteps}</p>
                </article>
              </div>

              <div className="timeline-meta">
                <h4>Latest Activity</h4>
                {lastStep ? (
                  <p>
                    {lastStep.stepName} · {new Date(lastStep.timestampUtc).toLocaleString()}
                  </p>
                ) : (
                  <p className="muted">No step activity yet.</p>
                )}
              </div>
            </section>
          </div>

          {error && <p className="error">{error}</p>}
          <StepViewer steps={steps} taskRunId={taskRunId} onRetry={handleRetry} />
        </section>
      );
    }

    if (activePage === 'environment') {
      return (
        <section className="card content">
          <h2>Environment</h2>
          <p>These values help you verify frontend runtime connectivity settings.</p>
          <table className="env-table">
            <thead>
              <tr>
                <th>Variable</th>
                <th>Value</th>
              </tr>
            </thead>
            <tbody>
              {envInfo.map((item) => (
                <tr key={item.key}>
                  <td>{item.key}</td>
                  <td>{item.value}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <p className="muted">Tip: configure `frontend/.env` for local overrides.</p>
        </section>
      );
    }

    return (
      <section className="card content">
        <h2>Settings</h2>
        <label htmlFor="language">Language</label>
        <select id="language" value={language} onChange={(event) => setLanguage(event.target.value)}>
          <option>English</option>
          <option>Español</option>
          <option>Français</option>
          <option>Deutsch</option>
        </select>

        <label htmlFor="mode">Mode</label>
        <select id="mode" value={mode} onChange={(event) => setMode(event.target.value as ThemeMode)}>
          <option value="light">Light</option>
          <option value="dark">Dark</option>
          <option value="system">System</option>
        </select>

        <p className="muted">Current language: {language}. Theme mode: {mode}.</p>
      </section>
    );
  };

  return (
    <main className="layout">
      <header className="page-header">
        <h1>TraceAI Control Center</h1>
        <p>Production-grade AI execution workspace for planning, coding, validation, and retry control.</p>
      </header>

      <nav className="nav-grid" aria-label="Primary navigation">
        {[
          ['workspace', 'Workspace'],
          ['settings', 'Settings'],
          ['environment', 'Environment']
        ].map(([key, label]) => (
          <button
            key={key}
            type="button"
            className={activePage === key ? 'nav-button active' : 'nav-button'}
            onClick={() => setActivePage(key as Page)}
          >
            {label}
          </button>
        ))}
      </nav>

      {renderContent()}
    </main>
  );
}
