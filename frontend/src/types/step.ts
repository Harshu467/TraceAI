export interface StepResult {
  stepName: string;
  input: string;
  output: string;
  success: boolean;
  error: string;
  retryCount: number;
  promptUsed: string;
  rawAiResponse: string;
  timestampUtc: string;
}

export interface ExecuteResponse {
  taskRunId: string;
  steps: StepResult[];
}
