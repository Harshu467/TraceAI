import { AgentEngine } from './AgentEngine';
import { StepResult } from './StepResult';

export interface ExecuteRequest {
  prompt?: string;
}

export interface RetryRequest {
  stepName?: string;
}

export interface ExecuteResponse {
  success: boolean;
  stepResults: StepResult[];
  error?: string;
}

export class TraceController {
  constructor(private readonly agentEngine: AgentEngine) {}

  public async execute(request: ExecuteRequest): Promise<ExecuteResponse> {
    if (!request?.prompt || !request.prompt.trim()) {
      return {
        success: false,
        stepResults: [],
        error: 'Prompt is required.',
      };
    }

    try {
      const executionResult = await this.agentEngine.execute(request.prompt.trim());
      return {
        success: executionResult.success,
        stepResults: executionResult.stepResults,
      };
    } catch (error) {
      return {
        success: false,
        stepResults: [],
        error: error instanceof Error ? error.message : String(error),
      };
    }
  }

  public async retry(request: RetryRequest): Promise<ExecuteResponse> {
    if (!request?.stepName || !request.stepName.trim()) {
      return {
        success: false,
        stepResults: [],
        error: 'Step name is required for retry.',
      };
    }

    try {
      const executionResult = await this.agentEngine.retryStep(request.stepName.trim());
      return {
        success: executionResult.success,
        stepResults: executionResult.stepResults,
      };
    } catch (error) {
      return {
        success: false,
        stepResults: [],
        error: error instanceof Error ? error.message : String(error),
      };
    }
  }
}
