import { useState } from 'react';

interface TaskInputProps {
  onExecute: (prompt: string, model: string) => Promise<void>;
  isRunning: boolean;
}

export function TaskInput({ onExecute, isRunning }: TaskInputProps) {
  const [prompt, setPrompt] = useState('Fix this C# function to avoid null reference exceptions.');
  const [model, setModel] = useState('gpt-4o-mini');

  return (
    <section className="card task-input">
      <div className="section-title">
        <h3>Task Brief</h3>
        <p className="muted">Describe a concrete engineering objective for the agent pipeline.</p>
      </div>
      <textarea
        rows={6}
        value={prompt}
        onChange={(e) => setPrompt(e.target.value)}
        placeholder="Example: Analyze this failing API endpoint, propose a fix, and include tests."
      />
      <div className="row">
        <div>
          <label htmlFor="model">Execution model</label>
          <select id="model" value={model} onChange={(e) => setModel(e.target.value)}>
            <option value="gpt-4o-mini">gpt-4o-mini</option>
            <option value="gpt-4.1-mini">gpt-4.1-mini</option>
            <option value="gpt-4.1">gpt-4.1</option>
          </select>
        </div>
        <div className="inline-kpi">
          <span>Mode</span>
          <strong>Step-traced</strong>
        </div>
      </div>
      <button disabled={isRunning} onClick={() => onExecute(prompt, model)}>
        {isRunning ? 'Running execution...' : 'Run Task Pipeline'}
      </button>
    </section>
  );
}
