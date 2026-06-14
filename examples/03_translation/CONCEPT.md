# Concept: System Prompts & Agent Specialization

## Overview

This example shows how to turn a general-purpose LLM into a **specialized agent** using a **system prompt**. The key insight: you often do not need a different model — you need different instructions.

## What is a System Prompt?

A system prompt is a persistent instruction that shapes the model's behavior for the entire conversation.

```mermaid
flowchart TB
    subgraph Context
        S[System Prompt:<br/>"You are a professional translator..."]
        U[User Message]
        A[Assistant Response]
    end
    S --> U --> A
```

The system prompt is always processed first and influences every response.

## Agent Specialization Pattern

```mermaid
flowchart LR
    M[General LLM] -->|+ system prompt| A[Specialized Agent]
    A -->|+ tools| B[Full Agent]
```

## Anatomy of an Effective System Prompt

```mermaid
flowchart TB
    R[1. Role: "You are a ..."]
    T[2. Task: "Your goal is to ..."]
    B[3. Rules: "Always X, never Y"]
    F[4. Format: "Respond as ..."]
    C[5. Constraints: "Do NOT ..."]
    R --> T --> B --> F --> C
```

This example's structure:

| Component | Content |
|-----------|---------|
| Role | Professional scientific translator |
| Task | Translate English → German precisely |
| Rules | 8 detailed translation guidelines |
| Format | Idiomatic German scientific style |
| Constraints | Only translated text, no commentary |

## C# Example

```csharp
var messages = new List<ChatMessage>
{
    ChatMessage.CreateSystemMessage("You are a coding assistant. Provide concise C# examples."),
    ChatMessage.CreateUserMessage("How do I read a file?")
};
```

## Why Detailed System Prompts Matter

**Minimal prompt** → unpredictable output, extra explanations, inconsistent terminology.

**Detailed prompt** → consistent quality, correct format, only the requested output.

## Design Patterns

| Pattern | Example |
|---------|---------|
| Role-playing | `"You are a senior database administrator..."` |
| Rule-based | `"Follow these rules: 1... 2... 3..."` |
| Output formatting | `"Respond in valid JSON."` |
| Contextual awareness | `"You know that... Current situation: ..."` |

## Key Takeaways

1. System prompts fundamentally change model behavior.
2. Detailed, structured instructions produce consistent results.
3. The same model can become many different agents.
4. Output constraints prevent unwanted commentary.
5. System prompts are the foundation for tool-using agents and reasoning patterns.
