export interface StepResultParams<TInput = unknown, TOutput = unknown> {
  StepName: string;
  Input?: TInput;
  Output?: TOutput;
  Success: boolean;
  ErrorMessage?: string;
  RetryCount?: number;
  Timestamp?: string;
}

export class StepResult<TInput = unknown, TOutput = unknown> {
  public StepName: string;
  public Input?: TInput;
  public Output?: TOutput;
  public Success: boolean;
  public ErrorMessage?: string;
  public RetryCount: number;
  public Timestamp: string;

  constructor(params: StepResultParams<TInput, TOutput>) {
    this.StepName = params.StepName;
    this.Input = params.Input;
    this.Output = params.Output;
    this.Success = params.Success;
    this.ErrorMessage = params.ErrorMessage;
    this.RetryCount = params.RetryCount ?? 0;
    this.Timestamp = params.Timestamp ?? new Date().toISOString();
  }

  public static createSuccess<TInput = unknown, TOutput = unknown>(params: {
    StepName: string;
    Input?: TInput;
    Output?: TOutput;
    RetryCount?: number;
    Timestamp?: string;
  }): StepResult<TInput, TOutput> {
    return new StepResult<TInput, TOutput>({
      StepName: params.StepName,
      Input: params.Input,
      Output: params.Output,
      Success: true,
      RetryCount: params.RetryCount,
      Timestamp: params.Timestamp,
    });
  }

  public static createFailure<TInput = unknown, TOutput = unknown>(params: {
    StepName: string;
    Input?: TInput;
    ErrorMessage: string;
    RetryCount?: number;
    Timestamp?: string;
  }): StepResult<TInput, TOutput> {
    return new StepResult<TInput, TOutput>({
      StepName: params.StepName,
      Input: params.Input,
      Output: undefined,
      Success: false,
      ErrorMessage: params.ErrorMessage,
      RetryCount: params.RetryCount,
      Timestamp: params.Timestamp,
    });
  }

  public withRetryCount(retryCount: number): StepResult<TInput, TOutput> {
    return new StepResult<TInput, TOutput>({
      StepName: this.StepName,
      Input: this.Input,
      Output: this.Output,
      Success: this.Success,
      ErrorMessage: this.ErrorMessage,
      RetryCount: retryCount,
      Timestamp: this.Timestamp,
    });
  }

  public toJSON(): StepResultParams<TInput, TOutput> {
    return {
      StepName: this.StepName,
      Input: this.Input,
      Output: this.Output,
      Success: this.Success,
      ErrorMessage: this.ErrorMessage,
      RetryCount: this.RetryCount,
      Timestamp: this.Timestamp,
    };
  }
}
