export interface AIServiceOptions {
  apiKey?: string;
  model?: string;
  baseUrl?: string;
}

export interface AIServiceResponse {
  success: boolean;
  text?: string;
  raw?: unknown;
  error?: string;
  status?: number;
}

export class AIService {
  private readonly apiKey: string;
  private readonly model: string;
  private readonly baseUrl: string;

  constructor(options: AIServiceOptions = {}) {
    const apiKey = options.apiKey ?? process.env.OPENAI_API_KEY;
    if (!apiKey) {
      throw new Error('OpenAI API key is required. Set apiKey or OPENAI_API_KEY.');
    }

    this.apiKey = apiKey;
    this.model = options.model ?? 'gpt-3.5-turbo';
    this.baseUrl = options.baseUrl ?? 'https://api.openai.com/v1';
  }

  public async sendPrompt(prompt: string): Promise<AIServiceResponse> {
    if (!prompt || !prompt.trim()) {
      return {
        success: false,
        error: 'Prompt cannot be empty.',
      };
    }

    try {
      const response = await fetch(`${this.baseUrl}/chat/completions`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${this.apiKey}`,
        },
        body: JSON.stringify({
          model: this.model,
          messages: [{ role: 'user', content: prompt }],
          temperature: 0.7,
          max_tokens: 1024,
        }),
      });

      const payload: any = await response.json();

      if (!response.ok) {
        const apiError = payload?.error?.message ?? response.statusText;
        return {
          success: false,
          error: `OpenAI API error (${response.status}): ${apiError}`,
          status: response.status,
          raw: payload,
        };
      }

      const text = Array.isArray(payload?.choices)
        ? payload.choices.map((choice: any) => choice?.message?.content ?? '').join('\n').trim()
        : '';

      return {
        success: true,
        text,
        raw: payload,
      };
    } catch (error) {
      return {
        success: false,
        error: error instanceof Error ? error.message : String(error),
      };
    }
  }
}
