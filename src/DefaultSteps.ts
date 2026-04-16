import { AgentExecutionContext, AgentStep } from './AgentEngine';
import { AIService } from './AIService';
import { StepResult } from './StepResult';

export abstract class BaseAgentStep<Input = unknown, Output = unknown> implements AgentStep<Input, Output> {
  public abstract readonly name: string;

  public get Name(): string {
    return this.name;
  }

  public async ExecuteAsync(input: Input, context: AgentExecutionContext): Promise<StepResult<Output>> {
    return this.execute(input, context);
  }

  public abstract execute(input: Input, context: AgentExecutionContext): Promise<StepResult<Output>>;

  protected success(input: Input, output: Output): StepResult<Input, Output> {
    return StepResult.createSuccess({
      StepName: this.name,
      Input: input,
      Output: output,
    });
  }

  protected failure(input: Input, error: string): StepResult<Input> {
    return StepResult.createFailure({
      StepName: this.name,
      Input: input,
      ErrorMessage: error,
    });
  }
}

export class PlanStep extends BaseAgentStep<string, { plan: string; userPrompt: string; aiPrompt: string }> {
  public readonly name = 'Plan';
  private readonly aiService: AIService;

  public constructor(aiService: AIService) {
    super();
    this.aiService = aiService;
  }

  public async execute(input: string): Promise<StepResult<string, { plan: string; userPrompt: string; aiPrompt: string }>> {
    const trimmedPrompt = input?.trim();
    if (!trimmedPrompt) {
      return this.failure(input, 'Prompt is empty.');
    }

    const aiPrompt = `Analyze the following user prompt and generate a structured execution plan. Return the plan as a concise set of steps for implementing the request.\n\nUser prompt:\n${trimmedPrompt}`;
    const aiResponse = await this.aiService.sendPrompt(aiPrompt);

    if (!aiResponse.success) {
      return this.failure(input, aiResponse.error ?? 'AI service failed to generate a plan.');
    }

    const plan = aiResponse.text?.trim();
    if (!plan) {
      return this.failure(input, 'AI returned an empty plan.');
    }

    return this.success(input, { plan, userPrompt: input, aiPrompt });
  }
}

export class CodeGenerationStep extends BaseAgentStep<{ plan: string; userPrompt: string; aiPrompt: string }, string> {
  public readonly name = 'CodeGeneration';
  private readonly aiService: AIService;

  public constructor(aiService: AIService) {
    super();
    this.aiService = aiService;
  }

  public async execute(input: { plan: string; userPrompt: string; aiPrompt: string }): Promise<StepResult<{ plan: string; userPrompt: string; aiPrompt: string }, string>> {
    const plan = input?.plan?.trim();
    const userPrompt = input?.userPrompt?.trim();

    if (!plan) {
      return this.failure(input, 'Plan is missing or empty.');
    }

    const generationPrompt = `Generate implementation code based on the following user request and execution plan. Return only the final code with no explanation.\n\nUser prompt:\n${userPrompt}\n\nExecution plan:\n${plan}`;
    const aiResponse = await this.aiService.sendPrompt(generationPrompt);

    if (!aiResponse.success) {
      return this.failure(input, aiResponse.error ?? 'AI service failed to generate code.');
    }

    const code = aiResponse.text?.trim();
    if (!code) {
      return this.failure(input, 'AI returned empty code output.');
    }

    return this.success(input, code);
  }
}

export class ValidationStep extends BaseAgentStep<string, string> {
  public readonly name = 'Validation';
  private readonly aiService: AIService;

  public constructor(aiService: AIService) {
    super();
    this.aiService = aiService;
  }

  public async execute(input: string): Promise<StepResult<string>> {
    const code = input?.trim();
    if (!code) {
      return this.failure(input, 'No generated code provided for validation.');
    }

    const validationPrompt = `Review the following generated code and identify issues or improvements. If the code is clean and correct, answer with a concise confirmation message.\n\nGenerated code:\n${code}`;
    const aiResponse = await this.aiService.sendPrompt(validationPrompt);

    if (!aiResponse.success) {
      return this.failure(input, aiResponse.error ?? 'AI service failed during code validation.');
    }

    const feedback = aiResponse.text?.trim();
    if (!feedback) {
      return this.failure(input, 'AI returned no validation feedback.');
    }

    const lowerFeedback = feedback.toLowerCase();
    const indicatesFailure = ['error', 'bug', 'issue', 'problem', 'invalid', 'fix', 'incorrect', 'fault'].some((keyword) => lowerFeedback.includes(keyword));

    if (indicatesFailure) {
      return this.failure(input, feedback);
    }

    return this.success(input, 'Validation passed. No major issues detected.');
  }
}

export function createDefaultSteps(aiService: AIService): AgentStep[] {
  return [new PlanStep(aiService), new CodeGenerationStep(aiService), new ValidationStep(aiService)];
}
