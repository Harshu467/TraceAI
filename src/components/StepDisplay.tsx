import React, { useState } from 'react';
import './StepDisplay.css';

export interface StepData {
  StepName: string;
  Success: boolean;
  Input?: unknown;
  Output?: unknown;
  ErrorMessage?: string;
  RetryCount: number;
  Timestamp: string;
}

export interface StepDisplayProps {
  step: StepData;
  onRetry?: (stepName: string) => Promise<void>;
  isLoading?: boolean;
}

export const StepDisplay: React.FC<StepDisplayProps> = ({ step, onRetry, isLoading = false }) => {
  const [isExpanded, setIsExpanded] = useState(false);
  const [isRetrying, setIsRetrying] = useState(false);
  const [retryError, setRetryError] = useState<string>();

  const statusClass = step.Success ? 'step-success' : 'step-failure';
  const statusLabel = step.Success ? 'Success' : 'Failed';
  const timestamp = new Date(step.Timestamp).toLocaleTimeString();

  const handleToggleExpand = () => {
    setIsExpanded(!isExpanded);
  };

  const handleRetry = async () => {
    if (onRetry && !step.Success && !isRetrying) {
      setIsRetrying(true);
      setRetryError(undefined);

      try {
        await onRetry(step.StepName);
      } catch (err) {
        setRetryError(err instanceof Error ? err.message : 'Retry failed');
      } finally {
        setIsRetrying(false);
      }
    }
  };

  return (
    <div className={`step-container ${statusClass}`}>
      <div className="step-header">
        <div className="step-info">
          <span className={`step-status-badge ${statusClass}`}>{statusLabel}</span>
          <h3 className="step-name">{step.StepName}</h3>
          <span className="step-timestamp">{timestamp}</span>
          {step.RetryCount > 0 && <span className="retry-count">Retry #{step.RetryCount}</span>}
        </div>

        <div className="step-actions">
          <button className="expand-button" onClick={handleToggleExpand} title={isExpanded ? 'Collapse' : 'Expand'}>
            {isExpanded ? '▼' : '▶'}
          </button>
          {!step.Success && onRetry && (
            <button className="retry-button" onClick={handleRetry} disabled={isLoading || isRetrying} title="Retry this step">
              {isRetrying ? 'Retrying...' : 'Retry'}
            </button>
          )}
        </div>
      </div>

      {isExpanded && (
        <div className="step-details">
          {retryError && (
            <div className="detail-section error-section">
              <h4>Retry Error</h4>
              <p className="error-message">{retryError}</p>
            </div>
          )}

          {step.Input && (
            <div className="detail-section">
              <h4>Input</h4>
              <pre className="detail-content">{JSON.stringify(step.Input, null, 2)}</pre>
            </div>
          )}

          {step.Output && (
            <div className="detail-section">
              <h4>Output</h4>
              <pre className="detail-content">{JSON.stringify(step.Output, null, 2)}</pre>
            </div>
          )}

          {step.ErrorMessage && (
            <div className="detail-section error-section">
              <h4>Error</h4>
              <p className="error-message">{step.ErrorMessage}</p>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
