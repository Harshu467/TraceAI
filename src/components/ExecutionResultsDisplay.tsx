import React from 'react';
import { StepDisplay, StepData } from './StepDisplay';
import './ExecutionResultsDisplay.css';

export interface ExecutionResultsDisplayProps {
  steps: StepData[];
  success: boolean;
  prompt?: string;
  onRetry?: (stepName: string) => Promise<void>;
  isLoading?: boolean;
}

export const ExecutionResultsDisplay: React.FC<ExecutionResultsDisplayProps> = ({
  steps,
  success,
  prompt,
  onRetry,
  isLoading = false,
}) => {
  const completedCount = steps.filter((s) => s.Success).length;
  const failedCount = steps.filter((s) => !s.Success).length;

  return (
    <div className="execution-results-container">
      <div className="results-summary">
        <div className={`summary-status ${success ? 'summary-success' : 'summary-failure'}`}>
          {success ? '✓ Execution Completed' : '✗ Execution Failed'}
        </div>

        <div className="summary-stats">
          <div className="stat">
            <span className="stat-label">Total Steps:</span>
            <span className="stat-value">{steps.length}</span>
          </div>
          <div className="stat success-stat">
            <span className="stat-label">Successful:</span>
            <span className="stat-value">{completedCount}</span>
          </div>
          <div className="stat failure-stat">
            <span className="stat-label">Failed:</span>
            <span className="stat-value">{failedCount}</span>
          </div>
        </div>

        {prompt && (
          <div className="prompt-section">
            <h4>Prompt</h4>
            <p className="prompt-text">{prompt}</p>
          </div>
        )}
      </div>

      <div className="steps-list">
        <h3>Step Details</h3>
        {steps.length === 0 ? (
          <p className="no-steps">No steps executed yet.</p>
        ) : (
          steps.map((step, index) => (
            <StepDisplay
              key={`${step.StepName}-${index}`}
              step={step}
              onRetry={onRetry}
              isLoading={isLoading}
            />
          ))
        )}
      </div>
    </div>
  );
};
