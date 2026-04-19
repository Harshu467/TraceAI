import React, { useState } from 'react';
import { ExecutionResultsDisplay } from './components';
import { useExecutionManager } from './hooks';
import './App.css';

/**
 * ExecutionApp: Complete example showing how to:
 * - Execute a prompt
 * - Display results with retry functionality
 * - Handle retries and update UI in real-time
 */
export const ExecutionApp: React.FC = () => {
  const [promptInput, setPromptInput] = useState('');
  const [hasExecuted, setHasExecuted] = useState(false);

  const { steps, success, prompt, isLoading, error, executePrompt, retryStep } = useExecutionManager({
    baseUrl: process.env.REACT_APP_API_URL || 'http://localhost:4000',
  });

  const handleExecute = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!promptInput.trim()) {
      // @ts-ignore
      alert('Please enter a prompt');
      return;
    }

    await executePrompt(promptInput);
    setHasExecuted(true);
  };

  const handleRetry = async (stepName: string) => {
    await retryStep(stepName, steps);
  };

  return (
    <div className="execution-app">
      <header className="app-header">
        <h1>TraceAI</h1>
        <p>AI Execution Engine with Real-Time Step Tracking</p>
      </header>

      <main className="app-main">
        <div className="execution-form">
          <form onSubmit={handleExecute}>
            <div className="form-group">
              <label htmlFor="prompt">Enter Your Prompt</label>
              <textarea
                id="prompt"
                value={promptInput}
                onChange={(e) => setPromptInput((e.target as HTMLTextAreaElement).value)}
                placeholder="Example: Create a function that validates email addresses"
                rows={4}
                disabled={isLoading}
              />
            </div>

            <button type="submit" className="submit-button" disabled={isLoading}>
              {isLoading ? 'Executing...' : 'Execute'}
            </button>
          </form>

          {error && (
            <div className="error-alert">
              <span className="error-icon">✕</span>
              <p>{error}</p>
            </div>
          )}
        </div>

        {hasExecuted && (
          <ExecutionResultsDisplay
            steps={steps}
            success={success}
            prompt={prompt}
            onRetry={handleRetry}
            isLoading={isLoading}
          />
        )}
      </main>

      <footer className="app-footer">
        <p>© 2026 TraceAI - Developer-First AI Execution Engine</p>
      </footer>
    </div>
  );
};

export default ExecutionApp;
