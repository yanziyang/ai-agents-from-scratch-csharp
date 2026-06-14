# Concept: Persistent Memory & State Management

## Overview

Persistent memory lets agents remember information across sessions, turning stateless responders into personalized assistants.

## The Memory Problem

```mermaid
flowchart LR
    subgraph WithoutMemory
        A[Session 1: I'm Alex] --> B[Session 2: What's my name?]
        B --> C[I don't know]
    end
    subgraph WithMemory
        D[Session 1: I'm Alex] --> E[(Memory)]
        F[Session 2: What's my name?] --> E
        E --> G[Alex!]
    end
```

## Architecture

```mermaid
flowchart TB
    subgraph Agent
        S[System Prompt + Memory Summary]
        L[LLM]
        T[saveMemory Tool]
    end
    User --> L
    L --> T
    T --> MM[MemoryManager]
    MM -->|JSON| File[(agent-memory.json)]
    File --> MM
    MM --> S
```

## Memory Types

| Type | Example |
|------|---------|
| Fact | "user_name: Alex" |
| Preference | "favorite_food: pizza" |
| Event | "Last discussed: C# async" |

## Duplicate Prevention

```mermaid
flowchart TD
    New[New user statement]
    Compare{Compare to memory}
    New --> Compare
    Compare -->|Exact match| Ack[Acknowledge only]
    Compare -->|Updated value| Update[Update memory]
    Compare -->|New info| Save[Save memory]
```

## Real-World Applications

- Personal assistants.
- Customer service bots.
- Learning tutors.
- Healthcare trackers.

## Key Takeaways

1. Memory makes agents personalized.
2. Inject a memory summary into the system prompt.
3. Let the agent decide what to save via a tool.
4. Avoid duplicate saves with reasoning instructions.
5. Store memories in a structured, persistent format.
