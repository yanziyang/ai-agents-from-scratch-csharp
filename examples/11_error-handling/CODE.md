# Code Explanation: Chapter 11 — Error Handling

This example shows how to make an agent resilient: typed errors, timeouts, retries with jitter, fallback tools, degraded mode, and correlation IDs.

> **Source code:** `src/Chapter11/Program.cs`
> **Run:** `dotnet run --project src/Chapter11`

## Error Taxonomy

Shared error classes in `AiAgents.Core.Exceptions`:

```csharp
public class AppError : Exception
{
    public string Code { get; }
    public string UserMessage { get; }
    public bool Retryable { get; }
    public object? Details { get; }
}

public class ValidationError : AppError { }
public class LLMCallError : AppError { }
public class ToolExecutionError : AppError { }
public class AgentWorkflowError : AppError { }
```

## Retry and Timeout Helpers

```csharp
await RetryHelper.WithTimeout(async ct => { ... }, TimeSpan.FromSeconds(15), "LLM prompt");
await RetryHelper.WithRetries(async () => { ... }, retries: 2, label: "LLM prompt");
```

- `WithTimeout` throws an `AppError` with code `TIMEOUT`.
- `WithRetries` adds exponential backoff plus random jitter, retrying only when `Retryable` is true.

## Simulated Tools

Two profile-fetch tools demonstrate failure modes:

- `u_999` → non-retryable "not found".
- `u_777` → primary retryable failure, then fallback failure.
- ~20% random transient primary failures for other IDs.

## Degraded Mode

If the LLM call fails, the agent drops to a deterministic path:

1. Extract `u_<digits>` from the input.
2. Retry the primary tool.
3. Fall back to the backup tool.
4. Format a short answer without the LLM.

## Workflow Errors

When orchestration cannot recover (policy guard, both tools fail), the agent throws `AgentWorkflowError` with:

- `Step` — where it failed.
- `Code` — stable machine-readable code.
- `UserMessage` — safe text for users.
- `InnerException` — root cause for operators.

## Key Takeaways

1. Normalize all errors to `AppError`.
2. Classify retryability before retrying.
3. Bound every call with a timeout.
4. Add jitter to backoff delays.
5. Provide degraded paths when the LLM is unavailable.
6. Always include a correlation ID.
