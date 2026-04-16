# Retry Functionality Implementation

## Overview

The TraceAI retry functionality allows users to re-run a failed step without re-running the entire execution pipeline. This is useful when a step fails temporarily or when debugging issues.

## Backend Implementation

### TraceController.retry()

```typescript
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
```

### API Endpoint

**POST /retry**

Request body:
```json
{
  "stepName": "Plan"
}
```

Response:
```json
{
  "success": true,
  "stepResults": [
    {
      "StepName": "Plan",
      "Success": true,
      "Input": {...},
      "Output": {...},
      "RetryCount": 1,
      "Timestamp": "2026-04-16T12:00:00.000Z"
    }
  ]
}
```

## Frontend Implementation

### useExecutionManager Hook

This hook manages the complete execution lifecycle including retries:

```typescript
const { steps, success, prompt, isLoading, error, executePrompt, retryStep } = useExecutionManager({
  baseUrl: 'http://localhost:4000',
});

// Execute initial prompt
await executePrompt('Create a function that validates emails');

// Retry a failed step
await retryStep('CodeGeneration', currentSteps);
```

### useRetryStep Hook

Lower-level hook for retrying a single step:

```typescript
const { isRetrying, error, retryStep } = useRetryStep({
  baseUrl: 'http://localhost:4000',
});

const updatedStep = await retryStep('Plan');
```

## React Component Integration

### StepDisplay Component

The `StepDisplay` component includes built-in retry functionality:

```typescript
<StepDisplay
  step={step}
  onRetry={async (stepName) => {
    await retryStep(stepName, steps);
  }}
  isLoading={isLoading}
/>
```

### ExecutionResultsDisplay Component

Container component that renders all steps and handles retries:

```typescript
<ExecutionResultsDisplay
  steps={steps}
  success={success}
  prompt={prompt}
  onRetry={handleRetry}
  isLoading={isLoading}
/>
```

## Complete Usage Example

```typescript
import React from 'react';
import { ExecutionApp } from './App';

function main() {
  return <ExecutionApp />;
}

export default main;
```

The `ExecutionApp` component provides:

1. **Prompt Input Form**: Users enter their prompt
2. **Execute Button**: Triggers execution via `POST /execute`
3. **Results Display**: Shows all step results
4. **Retry Functionality**: Failed steps can be retried individually
5. **Error Handling**: API errors are caught and displayed

## Flow Diagram

```
User enters prompt
    ↓
Click "Execute"
    ↓
POST /execute → Backend processes prompt
    ↓
Display results with step details
    ↓
[If step fails]
Click "Retry" on failed step
    ↓
POST /retry → Backend retries that step only
    ↓
Update UI with new step result + retry count incremented
```

## Retry Count Tracking

- Each `StepResult` includes a `RetryCount` field
- Starting retry count: 0
- Increments by 1 on each retry
- Displayed in UI: "Retry #1", "Retry #2", etc.

## Error Handling

### Frontend
- Network errors are caught and displayed in error alert
- Retry state prevents duplicate requests
- Loading state is managed during retries

### Backend
- Invalid step names return error
- Engine retry failures are caught and returned as error response
- Step count validation ensures steps exist

## Testing the Retry Flow

1. Start the server:
   ```bash
   npm run dev
   ```

2. Enter a prompt that will cause a step to fail (optional)

3. Click "Retry" on any failed step

4. Observe:
   - Loading state during retry
   - Retry count increments
   - Results update with new data
   - Error messages display if retry fails

## Performance Considerations

- Retries only re-run the failed step, not previous steps
- Previous successful results are reused
- Only downstream steps after a successful retry are re-executed
- No redundant API calls due to loading state management
