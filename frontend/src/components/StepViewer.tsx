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
    return (
      <section className="card empty-state">
        <h3>Execution timeline</h3>
        <p className="muted">No steps yet. Run a task pipeline to populate detailed trace results.</p>
      </section>
    );
  }

  return (
    <section className="card">
      <div className="section-title">
        <h3>Execution Timeline {taskRunId ? `(${taskRunId})` : ''}</h3>
        <p className="muted">Inspect each stage, logs, and retry controls.</p>
      </div>
      <div className="stack">
        {steps.map((step, idx) => {
          const key = `${step.stepName}-${idx}-${step.retryCount}`;
          const isExpanded = expanded[key] ?? false;
          return (
            <article key={key} className="step">
              <div className="step-header">
                <div>
                  <strong>{step.stepName}</strong>
                  <p className="step-time">{new Date(step.timestampUtc).toLocaleString()}</p>
                </div>
                <span className={step.success ? 'ok pill' : 'fail pill'}>{step.success ? 'Success' : 'Failed'}</span>
              </div>
              <div className="row">
                <small>Retry: {step.retryCount}</small>
                <small>{step.error ? 'Requires review' : 'Validated'}</small>
              </div>
              <button onClick={() => setExpanded((prev) => ({ ...prev, [key]: !isExpanded }))}>
                {isExpanded ? 'Hide Details' : 'View Details'}
              </button>
              {!step.success && taskRunId && (
                <button className="retry" onClick={() => onRetry(step.stepName)}>
                  Retry This Step
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
