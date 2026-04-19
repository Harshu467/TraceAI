import React, { useState } from 'react';
import { ExecutionResultsDisplay } from './ExecutionResultsDisplay';

/**
 * Example: ExecutionViewer Component
 * Shows how to use ExecutionResultsDisplay with real data
 */
export const ExecutionViewer: React.FC = () => {
  const [executionData, setExecutionData] = useState({
    success: false,
    prompt: 'Create a function that validates email addresses',
    stepResults: [
      {
        StepName: 'Plan',
        Success: true,
        Input: { userPrompt: 'Create a function that validates email addresses', aiPrompt: 'Analyze...' },
        Output: { plan: 'Step 1: Parse requirements\nStep 2: Generate validation logic\nStep 3: Test thoroughly' },
        ErrorMessage: undefined,
        RetryCount: 0,
        Timestamp: new Date(Date.now() - 10000).toISOString(),
      },
      {
        StepName: 'CodeGeneration',
        Success: true,
        Input: { plan: 'Step 1: Parse requirements...' },
        Output: `function validateEmail(email: string): boolean {\n  const regex = /^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$/;\n  return regex.test(email);\n}`,
        ErrorMessage: undefined,
        RetryCount: 0,
        Timestamp: new Date(Date.now() - 5000).toISOString(),
      },
      {
        StepName: 'Validation',
        Success: false,
        Input: `function validateEmail(email: string): boolean {...}`,
        Output: undefined,
        ErrorMessage: 'Regex pattern could be improved. Consider adding more comprehensive email validation.',
        RetryCount: 0,
        Timestamp: new Date().toISOString(),
      },
    ],
  });

  const [isRetrying, setIsRetrying] = useState(false);

  const handleRetry = async (stepName: string) => {
    setIsRetrying(true);
    // Simulate API call to retry the step
    await new Promise((resolve) => setTimeout(resolve, 1500));
    setIsRetrying(false);
    // Update with new results
    // @ts-ignore
    alert(`Retried step: ${stepName}`);
  };

  return (
    <div>
      <ExecutionResultsDisplay
        steps={executionData.stepResults}
        success={executionData.success}
        prompt={executionData.prompt}
        onRetry={handleRetry}
        isLoading={isRetrying}
      />
    </div>
  );
};

export default ExecutionViewer;
