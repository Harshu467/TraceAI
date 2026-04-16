import { useState } from 'react';

interface TaskInputProps {
  onExecute: (prompt: string, model: string) => Promise<void>;
  isRunning: boolean;
}

export function TaskInput({ onExecute, isRunning }: TaskInputProps) {
  const [prompt, setPrompt] = useState('Fix this C# function to avoid null reference exceptions.');
  const [model, setModel] = useState('gpt-4o-mini');

  return (
    <section className="card">
      <h2>TraceAI Task Input</h2>
      <textarea
        rows={6}
        value={prompt}
        onChange={(e) => setPrompt(e.target.value)}
        placeholder="Enter engineering task prompt..."
      />
      <div className="row">
        <label htmlFor="model">Model</label>
        <select id="model" value={model} onChange={(e) => setModel(e.target.value)}>
          <option value="gpt-4o-mini">gpt-4o-mini</option>
          <option value="gpt-4.1-mini">gpt-4.1-mini</option>
          <option value="gpt-4.1">gpt-4.1</option>
        </select>
      </div>
      <button disabled={isRunning} onClick={() => onExecute(prompt, model)}>
        {isRunning ? 'Executing...' : 'Execute Task'}
      </button>
    </section>
  );
}
