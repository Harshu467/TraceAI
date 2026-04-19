import { useState, useCallback } from 'react';
import { StepData } from '../components';

export interface UseExecutionManagerOptions {
  baseUrl?: string;
}

export interface UseExecutionManagerResult {
  steps: StepData[];
  success: boolean;
  prompt?: string;
  isLoading: boolean;
  error?: string;
  executePrompt: (prompt: string) => Promise<void>;
  retryStep: (stepName: string, currentSteps: StepData[]) => Promise<void>;
}

export const useExecutionManager = (options: UseExecutionManagerOptions = {}): UseExecutionManagerResult => {
  const [steps, setSteps] = useState<StepData[]>([]);
  const [success, setSuccess] = useState(false);
  const [prompt, setPrompt] = useState<string>();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string>();

  const baseUrl = options.baseUrl || '/api';

  const executePrompt = useCallback(
    async (inputPrompt: string) => {
      setIsLoading(true);
      setError(undefined);
      setSteps([]);
      setPrompt(inputPrompt);

      try {
        const response = await fetch(`${baseUrl}/execute`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({ prompt: inputPrompt }),
        });

        if (!response.ok) {
          const errorData: any = await response.json().catch(() => ({}));
          throw new Error(errorData.error || `Execution failed with status ${response.status}`);
        }

        const data: any = await response.json();
        setSteps(data.stepResults || []);
        setSuccess(data.success ?? false);
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Unknown error occurred';
        setError(errorMessage);
        setSuccess(false);
      } finally {
        setIsLoading(false);
      }
    },
    [baseUrl],
  );

  const retryStep = useCallback(
    async (stepName: string, currentSteps: StepData[]) => {
      setIsLoading(true);
      setError(undefined);

      try {
        const response = await fetch(`${baseUrl}/retry`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({ stepName }),
        });

        if (!response.ok) {
          const errorData: any = await response.json().catch(() => ({}));
          throw new Error(errorData.error || `Retry failed with status ${response.status}`);
        }

        const data: any = await response.json();
        const updatedSteps = data.stepResults || currentSteps;
        setSteps(updatedSteps);
        setSuccess(data.success ?? false);
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Unknown error occurred';
        setError(errorMessage);
      } finally {
        setIsLoading(false);
      }
    },
    [baseUrl],
  );

  return {
    steps,
    success,
    prompt,
    isLoading,
    error,
    executePrompt,
    retryStep,
  };
};
