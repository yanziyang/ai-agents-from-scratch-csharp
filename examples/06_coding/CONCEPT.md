# Concept: Streaming & Response Control

## Overview

This example covers **streaming responses** and **token limits**, two techniques essential for responsive and cost-controlled agent interfaces.

## The Streaming Problem

### Non-Streaming

```mermaid
flowchart LR
    U[User prompt] --> W[Wait 10s] --> F[Full response]
```

### Streaming

```mermaid
flowchart LR
    U[User prompt] --> T1["Token 1 (0.1s)"] --> T2["Token 2 (0.2s)"] --> T3["..."] --> F[Complete]
```

Streaming gives immediate feedback and improves perceived performance.

## How Streaming Works

```mermaid
flowchart TB
    G[Model generates token] --> S[Server sends chunk]
    S --> C[Client receives chunk]
    C --> D[Display / log / process]
```

The model still generates one token at a time; streaming exposes each chunk as it is produced.

## Token Limits

```mermaid
flowchart TB
    subgraph Context
        Sys[System 200]
        User[User 100]
        Res[Response max 2000]
        Hist[Remaining history]
    end
```

Limiting output:

- Prevents overly long responses.
- Controls cost.
- Keeps latency predictable.

## .NET Streaming Pattern

```csharp
await foreach (var update in chatClient.CompleteChatStreamingAsync(messages, options))
{
    foreach (var part in update.ContentUpdate)
    {
        Console.Write(part.Text);
    }
}
```

## Buffering Strategies

| Strategy | Behavior | Use Case |
|----------|----------|----------|
| Immediate | Every token → display | Smoothest UX |
| Line | Buffer until newline | Paragraph output |
| Time | Buffer 50ms then flush | Reduce callback frequency |

## Best Practices

1. Set `MaxOutputTokenCount` for predictable responses.
2. Use `StringBuilder` to accumulate while streaming.
3. Handle completion/cancellation gracefully.
4. Monitor time-to-first-token and throughput.

## Key Takeaways

1. Streaming improves UX in user-facing applications.
2. `IAsyncEnumerable<T>` is the idiomatic .NET streaming API.
3. Token limits control cost and response length.
4. Buffering strategies balance smoothness and overhead.
5. Streaming is essential for production chat interfaces.
