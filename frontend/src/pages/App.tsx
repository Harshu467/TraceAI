import { useEffect, useRef, useState } from 'react';
import type { HubConnection } from '@microsoft/signalr';
import { TaskInput } from '../components/TaskInput';
import { StepViewer } from '../components/StepViewer';
import { executeTask, retryStep } from '../services/api';
import { connectToTask } from '../services/signalr';
import type { StepResult } from '../types/step';

type Page = 'home' | 'about' | 'contact' | 'faq' | 'auth' | 'profile' | 'settings' | 'environment';
type ThemeMode = 'light' | 'dark' | 'system';

const FAQ_ITEMS = [
  {
    question: 'What is TraceAI?',
    answer:
      'TraceAI is a debuggable AI execution engine that breaks complex requests into transparent steps so users can review every phase of the workflow.'
  },
  {
    question: 'How do retries work?',
    answer:
      'If a step fails, you can retry that specific step from the Home page without re-running the full execution pipeline.'
  },
  {
    question: 'Can I change language and theme?',
    answer: 'Yes. Visit Settings to choose your preferred language and switch between Light, Dark, or System mode.'
  }
];

export function App() {
  const [steps, setSteps] = useState<StepResult[]>([]);
  const [taskRunId, setTaskRunId] = useState<string | undefined>();
  const [isRunning, setIsRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [activePage, setActivePage] = useState<Page>('home');
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

  const renderContent = () => {
    if (activePage === 'home') {
      return (
        <section className="card">
          <h2>Agent Workspace</h2>
          <p>Use this page to run and inspect each execution step in real time.</p>
          {error && <p className="error">{error}</p>}
          <TaskInput onExecute={handleExecute} isRunning={isRunning} />
          <StepViewer steps={steps} taskRunId={taskRunId} onRetry={handleRetry} />
        </section>
      );
    }

    if (activePage === 'about') {
      return (
        <section className="card content">
          <h2>About TraceAI</h2>
          <p>
            TraceAI helps teams execute AI-assisted development tasks with complete visibility. Each task is split
            into explainable phases such as planning, validation, implementation, and fixes.
          </p>
          <ul>
            <li>Transparent multi-step execution pipeline</li>
            <li>Structured logs and retry support</li>
            <li>Real-time step updates via SignalR</li>
            <li>Configuration options for user preferences</li>
          </ul>
        </section>
      );
    }

    if (activePage === 'contact') {
      return (
        <section className="card content">
          <h2>Contact & Queries</h2>
          <p>Need help, have feedback, or found an issue? Reach us through the channels below:</p>
          <ul>
            <li>Email: support@traceai.app</li>
            <li>Product Feedback: feedback@traceai.app</li>
            <li>Partnerships: partnerships@traceai.app</li>
          </ul>
          <p>Response time: within 24-48 business hours.</p>
        </section>
      );
    }

    if (activePage === 'faq') {
      return (
        <section className="card content">
          <h2>FAQ</h2>
          {FAQ_ITEMS.map((item) => (
            <article key={item.question} className="faq-item">
              <h3>{item.question}</h3>
              <p>{item.answer}</p>
            </article>
          ))}
        </section>
      );
    }

    if (activePage === 'auth') {
      return (
        <section className="card auth-grid">
          <div>
            <h2>Login</h2>
            <label htmlFor="login-email">Email</label>
            <input id="login-email" type="email" placeholder="you@example.com" />
            <label htmlFor="login-password">Password</label>
            <input id="login-password" type="password" placeholder="Enter password" />
            <button type="button">Login</button>
          </div>
          <div>
            <h2>Sign Up</h2>
            <label htmlFor="signup-name">Full Name</label>
            <input id="signup-name" type="text" placeholder="Your full name" />
            <label htmlFor="signup-email">Email</label>
            <input id="signup-email" type="email" placeholder="you@example.com" />
            <label htmlFor="signup-password">Password</label>
            <input id="signup-password" type="password" placeholder="Create password" />
            <button type="button">Create account</button>
          </div>
        </section>
      );
    }

    if (activePage === 'profile') {
      return (
        <section className="card content">
          <h2>Profile Details</h2>
          <div className="profile-grid">
            <p>
              <strong>Name:</strong> Demo User
            </p>
            <p>
              <strong>Email:</strong> demo.user@traceai.app
            </p>
            <p>
              <strong>Role:</strong> AI Workflow Engineer
            </p>
            <p>
              <strong>Region:</strong> United States
            </p>
          </div>
          <button type="button">Edit profile</button>
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
        <h1>TraceAI</h1>
        <p>Debuggable AI execution platform for planning, coding, and review.</p>
      </header>

      <nav className="nav-grid" aria-label="Primary navigation">
        {[
          ['home', 'Home'],
          ['about', 'About'],
          ['contact', 'Contact'],
          ['faq', 'FAQ'],
          ['auth', 'Login / Sign Up'],
          ['profile', 'Profile'],
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
