import { StepResult } from './StepResult';

export interface AgentExecutionContext {
  prompt: string;
  completedSteps: StepResult[];
  metadata?: Record<string, unknown>;
}

export interface IStep<Input = unknown, Output = unknown> {
  readonly Name: string;
  ExecuteAsync(input: Input, context: AgentExecutionContext): Promise<StepResult<Output>>;
}

export interface AgentStep<Input = unknown, Output = unknown> extends IStep<Input, Output> {
  readonly name: string;
  execute(input: Input, context: AgentExecutionContext): Promise<StepResult<Input, Output>>;
}

export interface AgentExecutionResult {
  prompt: string;
  success: boolean;
  stepResults: StepResult[];
}

export class AgentEngine {
  private readonly steps: AgentStep[];
  private lastExecution?: AgentExecutionResult;

  constructor(steps: AgentStep[]) {
    this.steps = [...steps];
  }

  public async execute(prompt: string): Promise<AgentExecutionResult> {
    const stepResults: StepResult[] = [];
    const context: AgentExecutionContext = { prompt, completedSteps: stepResults };
    let currentInput: unknown = prompt;

    for (const step of this.steps) {
      const result = await this.runStep(step, currentInput, context);
      stepResults.push(result);

      if (!result.Success) {
        break;
      }

      currentInput = result.Output;
    }

    const executionResult: AgentExecutionResult = {
      prompt,
      success: stepResults.every((stepResult) => stepResult.Success),
      stepResults,
    };

    this.lastExecution = executionResult;
    return executionResult;
  }

  public async retryStep(stepName: string): Promise<AgentExecutionResult> {
    if (!this.lastExecution) {
      throw new Error('No previous execution exists to retry. Run execute() first.');
    }

    const failedIndex = this.lastExecution.stepResults.findIndex((stepResult) => stepResult.StepName === stepName);
    if (failedIndex === -1) {
      throw new Error(`Step not found in the last execution: ${stepName}`);
    }

    const previousStepResult = this.lastExecution.stepResults[failedIndex];
    if (previousStepResult.Success) {
      throw new Error(`Step '${stepName}' was not a failed step and cannot be retried.`);
    }

    const previousSuccessfulResults = this.lastExecution.stepResults.slice(0, failedIndex);
    const input = failedIndex === 0 ? this.lastExecution.prompt : previousSuccessfulResults[failedIndex - 1]?.Output;
    const context: AgentExecutionContext = {
      prompt: this.lastExecution.prompt,
      completedSteps: [...previousSuccessfulResults],
    };

    const failedStep = this.steps[failedIndex];
    if (!failedStep) {
      throw new Error(`No configured step found at index ${failedIndex}.`);
    }

    const retryResult = await this.runStep(failedStep, input, context, previousStepResult);
    const updatedStepResults: StepResult[] = [...previousSuccessfulResults, retryResult];
    let currentInput: unknown = retryResult.Output;
    let executionSuccess = retryResult.Success;

    if (retryResult.Success) {
      for (let nextIndex = failedIndex + 1; nextIndex < this.steps.length; nextIndex += 1) {
        const nextStep = this.steps[nextIndex];
        const nextContext: AgentExecutionContext = {
          prompt: this.lastExecution.prompt,
          completedSteps: [...updatedStepResults],
        };

        const nextResult = await this.runStep(nextStep, currentInput, nextContext);
        updatedStepResults.push(nextResult);

        if (!nextResult.Success) {
          executionSuccess = false;
          break;
        }

        currentInput = nextResult.Output;
      }
    } else {
      executionSuccess = false;
    }

    const executionResult: AgentExecutionResult = {
      prompt: this.lastExecution.prompt,
      success: executionSuccess,
      stepResults: updatedStepResults,
    };

    this.lastExecution = executionResult;
    return executionResult;
  }

  private async runStep(
    step: AgentStep,
    input: unknown,
    context: AgentExecutionContext,
    previousAttempt?: StepResult,
  ): Promise<StepResult> {
    try {
      const result = await step.ExecuteAsync(input, context);
      if (previousAttempt) {
        return result.withRetryCount(previousAttempt.RetryCount + 1);
      }

      return result;
    } catch (error) {
      const retryCount = previousAttempt ? previousAttempt.RetryCount + 1 : 0;
      return StepResult.createFailure({
        StepName: step.Name,
        Input: input,
        ErrorMessage: error instanceof Error ? error.message : String(error),
        RetryCount: retryCount,
      });
    }
  }
}
