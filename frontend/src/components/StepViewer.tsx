import { useState } from 'react';
import type { StepResult } from '../types/step';

interface StepViewerProps {
  steps: StepResult[];
  taskRunId?: string;
  onRetry: (stepName: string) => Promise<void>;
}

export function StepViewer({ steps, taskRunId, onRetry }: StepViewerProps) {
  const [expanded, setExpanded] = useState<Record<string, boolean>>({});

  if (!steps.length) {
    return <section className="card">No steps yet. Execute a task to see tracing.</section>;
  }

  return (
    <section className="card">
      <h2>Step Trace {taskRunId ? `(${taskRunId})` : ''}</h2>
      <div className="stack">
        {steps.map((step, idx) => {
          const key = `${step.stepName}-${idx}-${step.retryCount}`;
          const isExpanded = expanded[key] ?? false;
          return (
            <article key={key} className="step">
              <div className="step-header">
                <strong>{step.stepName}</strong>
                <span className={step.success ? 'ok' : 'fail'}>{step.success ? 'Success' : 'Failed'}</span>
              </div>
              <div className="row">
                <small>Retry: {step.retryCount}</small>
                <small>{new Date(step.timestampUtc).toLocaleString()}</small>
              </div>
              <button onClick={() => setExpanded((prev) => ({ ...prev, [key]: !isExpanded }))}>
                {isExpanded ? 'Hide Logs' : 'Show Logs'}
              </button>
              {!step.success && taskRunId && (
                <button className="retry" onClick={() => onRetry(step.stepName)}>
                  Retry Step
                </button>
              )}

              {isExpanded && (
                <div className="log-box">
                  <p><strong>Input:</strong> {step.input}</p>
                  <p><strong>Prompt Used:</strong> {step.promptUsed}</p>
                  <p><strong>Output:</strong> {step.output}</p>
                  <p><strong>Raw AI Response:</strong> {step.rawAiResponse}</p>
                  {!!step.error && <p><strong>Error:</strong> {step.error}</p>}
                </div>
              )}
            </article>
          );
        })}
      </div>
    </section>
  );
}
