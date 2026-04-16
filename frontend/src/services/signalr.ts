import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import type { StepResult } from '../types/step';

const HUB_URL = 'http://localhost:5000/hubs/execution';

export async function connectToTask(
  taskRunId: string,
  onStepUpdated: (step: StepResult) => void
): Promise<HubConnection> {
  const connection = new HubConnectionBuilder()
    .withUrl(HUB_URL)
    .configureLogging(LogLevel.Information)
    .withAutomaticReconnect()
    .build();

  connection.on('stepUpdated', (step: StepResult) => {
    onStepUpdated(step);
  });

  await connection.start();
  await connection.invoke('JoinTaskGroup', taskRunId);
  return connection;
}
