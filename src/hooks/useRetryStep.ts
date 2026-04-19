import { useState, useCallback } from 'react';
import { StepData } from '../components';

export interface UseRetryStepOptions {
  baseUrl?: string;
}

export interface UseRetryStepResult {
  isRetrying: boolean;
  error?: string;
  retryStep: (stepName: string) => Promise<StepData | null>;
}

export const useRetryStep = (options: UseRetryStepOptions = {}): UseRetryStepResult => {
  const [isRetrying, setIsRetrying] = useState(false);
  const [error, setError] = useState<string>();

  const baseUrl = options.baseUrl || '/api';

  const retryStep = useCallback(
    async (stepName: string): Promise<StepData | null> => {
      setIsRetrying(true);
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
        return data as StepData;
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Unknown error occurred';
        setError(errorMessage);
        return null;
      } finally {
        setIsRetrying(false);
      }
    },
    [baseUrl],
  );

  return { isRetrying, error, retryStep };
};
