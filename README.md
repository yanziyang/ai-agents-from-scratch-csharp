# AI Agents From Scratch — .NET 10 / DeepSeek Edition

Learn to build AI agents from first principles in C# with .NET 10. The examples use **DeepSeek V4 Flash** via the OpenAI .NET SDK, so you can focus on agent patterns without managing a local chat model. Chapter 15 also shows how to add a **local embedding model** with LLamaSharp for tool routing.

![Agent architecture overview](diagrams/agent-architecture.png)

## Purpose

This repository teaches you to build AI agents from first principles. By working through these examples, you'll understand:

- How LLMs work at a fundamental level
- What agents really are (LLM + tools + patterns)
- How different agent architectures function
- Why frameworks make certain design choices

> A JavaScript/TypeScript version of the original tutorial is available here:
> https://github.com/pguso/agents-from-scratch

**Philosophy**: Learn by building. Understand deeply, then use frameworks wisely.

## Companion Website

This repository has a **matching companion website**:

**https://agentsfromscratch.com**

The website is **not a replacement for this repo**, but a **conceptual companion** that:

- Explains *why* each example exists
- Visualizes the learning path from raw LLM calls to full agents
- Separates **code**, **explanations**, and **core concepts**
- Helps you understand agent architectures before using frameworks

**Recommended workflow:**
- Use **GitHub** for running, modifying, and studying the code
- Use the **website** for mental models, explanations, and progression

> Think of the site as the *map* and this repo as the *terrain*.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A DeepSeek API key (sign up at https://platform.deepseek.com)
- At least 8 GB RAM (16 GB recommended if you run the local embedding model in Chapter 15)

## Setup

1. Clone the repository.
2. For every chapter project, copy the secrets example file:
   ```bash
   cp src/Chapter01/appsettings.Secrets.example.json src/Chapter01/appsrets.json
   ```
   Or run the helper once per project:
   ```bash
   Get-ChildItem src/Chapter* | ForEach-Object { Copy-Item "$($_.FullName)\appsettings.Secrets.example.json" "$($_.FullName)\appsettings.Secrets.json" }
   ```
3. Add your DeepSeek API key to each `appsettings.Secrets.json`.
4. (Optional, for Chapter 15) Download an embedding GGUF model and place it under `models/`. See [DOWNLOAD.md](DOWNLOAD.md).

## Run Examples

Each chapter is a standalone .NET console app under `src/`.

```bash
cd src/Chapter01
dotnet run
```

```bash
cd src/Chapter07
dotnet run
```

```bash
cd src/Chapter09
dotnet run
```

You can also build the whole solution:

```bash
dotnet build
```

---

## Learning Path

Follow these examples in order to build understanding progressively:

### 1. **Introduction** — Basic LLM Interaction
`src/Chapter01` | [Concepts](examples/01_intro/CONCEPT.md) | [Code Explanation](examples/01_intro/CODE.md)

**What you'll learn:**
- Calling a hosted LLM from C#
- Basic prompt/response cycle

**Key concepts**: Chat client, API key configuration, inference endpoint

---

### 2. **DeepSeek Intro** — Using the OpenAI .NET SDK with DeepSeek
`src/Chapter02` | [Concepts](examples/02_deepseek-intro/CONCEPT.md) | [Code Explanation](examples/02_deepseek-intro/CODE.md)

**What you'll learn:**
- Pointing the OpenAI .NET SDK at DeepSeek's base URL
- Temperature and token usage

**Key concepts**: OpenAI-compatible endpoints, base URL, model name

---

### 3. **Translation** — System Prompts & Specialization
`src/Chapter03` | [Concepts](examples/03_translation/CONCEPT.md) | [Code Explanation](examples/03_translation/CODE.md)

**What you'll learn:**
- Using system prompts to specialize agents
- Output format control
- Role-based behavior

**Key concepts**: System prompts, agent specialization, behavioral constraints, prompt engineering

---

### 4. **Think** — Reasoning & Problem Solving
`src/Chapter04` | [Concepts](examples/04_think/CONCEPT.md) | [Code Explanation](examples/04_think/CODE.md)

**What you'll learn:**
- Configuring LLMs for logical reasoning
- Complex quantitative problems
- Limitations of pure LLM reasoning
- When to use external tools

**Key concepts**: Reasoning agents, problem decomposition, cognitive tasks, reasoning limitations

---

### 5. **Batch** — Parallel Processing
`src/Chapter05` | [Concepts](examples/05_batch/CONCEPT.md) | [Code Explanation](examples/05_batch/CODE.md)

**What you'll learn:**
- Processing multiple requests concurrently
- Parallel async patterns in C#
- Throughput optimization

**Key concepts**: Parallel execution, async/await, batch size, throughput optimization

---

### 6. **Coding** — Streaming & Response Control
`src/Chapter06` | [Concepts](examples/06_coding/CONCEPT.md) | [Code Explanation](examples/06_coding/CODE.md)

**What you'll learn:**
- Real-time streaming responses
- Token limits and budget management
- Progressive output display
- User experience optimization

**Key concepts**: Streaming, token-by-token generation, response control, real-time feedback

---

### 7. **Simple Agent** — Function Calling (Tools)
`src/Chapter07` | [Concepts](examples/07_simple-agent/CONCEPT.md) | [Code Explanation](examples/07_simple-agent/CODE.md)

**What you'll learn:**
- Function calling / tool use fundamentals
- Defining tools the LLM can use
- JSON Schema for parameters
- How LLMs decide when to use tools

**Key concepts**: Function calling, tool definitions, agent decision making, action-taking

**This is where text generation becomes agency!**

---

### 8. **Simple Agent with Memory** — Persistent State
`src/Chapter08` | [Concepts](examples/08_simple-agent-with-memory/CONCEPT.md) | [Code Explanation](examples/08_simple-agent-with-memory/CODE.md)

**What you'll learn:**
- Persisting information across sessions
- Long-term memory management
- Facts and preferences storage
- Memory retrieval strategies

**Key concepts**: Persistent memory, state management, memory systems, context augmentation

---

### 9. **ReAct Agent** — Reasoning + Acting
`src/Chapter09` | [Concepts](examples/09_react-agent/CONCEPT.md) | [Code Explanation](examples/09_react-agent/CODE.md)

**What you'll learn:**
- ReAct pattern (Reason → Act → Observe)
- Iterative problem solving
- Step-by-step tool use
- Self-correction loops

**Key concepts**: ReAct pattern, iterative reasoning, observation-action cycles, multi-step agents

**This is the foundation of modern agent frameworks!**

---

### 10. **AoT Agent** — Atom of Thought Planning
`src/Chapter10` | [Concepts](examples/10_aot-agent/CONCEPT.md) | [Code Explanation](examples/10_aot-agent/CODE.md)

**What you'll learn:**
- Atom of Thought methodology
- Atomic planning for multi-step computations
- Dependency management between operations
- Structured JSON output for reasoning plans
- Deterministic execution of plans

**Key concepts**: AoT planning, atomic operations, dependency resolution, plan validation, structured reasoning

---

### 11. **Error Handling** — Resilience for LLM + Tools
`src/Chapter11` | [Concepts](examples/11_error-handling/CONCEPT.md) | [Code Explanation](examples/11_error-handling/CODE.md)

**What you'll learn:**
- Typed error taxonomy (validation, LLM, tools, workflow) with stable codes
- Timeouts, retries with backoff/jitter, and classifying transient failures
- Graceful degradation when the LLM path fails (deterministic tool fallback)
- Orchestration-level errors and correlation ids for support

**Key concepts**: Error taxonomy, retry policies, timeouts, fallbacks, degraded mode, observability, user-safe messaging

---

### 12. **Tree of Thought** — Search over reasoning branches
`src/Chapter12` | [Concepts](examples/12_tree-of-thought/CONCEPT.md) | [Code Explanation](examples/12_tree-of-thought/CODE.md)

**What you'll learn:**
- Generating multiple candidate hypotheses from the same behavior
- Ranking and pruning branches with a deterministic score in code
- Running a compact search loop with inspectable kept/pruned decisions

**Key concepts**: Tree of Thought, branch/score/prune, verifiable objectives, search controllers

---

### 13. **Graph of Thought** — DAG merge for multi-source outputs
`src/Chapter13` | [Concepts](examples/13_graph-of-thought/CONCEPT.md) | [Code Explanation](examples/13_graph-of-thought/CODE.md)

**What you'll learn:**
- Modeling reasoning as a graph: parallel hypotheses → contrast → refine → aggregate → conclude
- Keeping weaker branches alive and improving them
- Running independent nodes in parallel to reduce latency

**Key concepts**: Graph of Thought, DAG orchestration, multi-source fusion, merge-before-generate

**Decision guide**: use ToT when you need to search competing paths; use GoT when you need to combine multiple sources into one consistent view.

---

### 14. **Chain of Thought** — Auditable stepwise decisioning
`src/Chapter14` | [Concepts](examples/14_chain-of-thought/CONCEPT.md) | [Code Explanation](examples/14_chain-of-thought/CODE.md)

**What you'll learn:**
- Splitting a high-stakes decision into explicit reasoning phases
- Preventing early bias with a facts-only extraction step
- Balancing fraud signals with legitimacy evidence before policy application
- Producing an auditable final decision with customer-safe and internal outputs

**Key concepts**: Chain of Thought, structured reasoning traces, policy-constrained decisions, explainability

---

### 15. **Tool routing (embeddings)** — Narrow the tool catalog per request
`src/Chapter15` | [Concepts](examples/15_tool-routing-embeddings/CONCEPT.md) | [Code Explanation](examples/15_tool-routing-embeddings/CODE.md)

**What you'll learn:**
- Precomputing embeddings for short **exemplar** phrases per tool
- Scoring the user message against exemplars (cosine similarity) with a local embedding model
- Passing only **top-k** tools (plus optional **always-include** tools) into the chat model
- Observing **recall** failure when k is too small for multi-intent prompts

**Key concepts**: Tool routing, embedding similarity, exemplar design, context/token savings, pinned tools, retrieval-style agent design

---

## Documentation Structure

Each example folder contains:

- **`src/ChapterXX/Program.cs`** — The working C# code example
- **`src/ChapterXX/appsettings.json`** — Non-secret configuration (DeepSeek base URL, model name, optional embedding model path)
- **`src/ChapterXX/appsettings.Secrets.example.json`** — Example secrets file (copy to `appsettings.Secrets.json`)
- **`examples/XX_name/CODE.md`** — Step-by-step code explanation
- **`examples/XX_name/CONCEPT.md`** — High-level concepts, Mermaid diagrams, and real-world applications

## Core Concepts

### What is an AI Agent?

```text
AI Agent = LLM + System Prompt + Tools + Memory + Reasoning Pattern
           ─┬─   ──────┬──────   ──┬──   ──┬───   ────────┬────────
            │          │           │       │              │
         Brain      Identity    Hands   State         Strategy
```

### Evolution of Capabilities

```text
1. intro          → Basic LLM usage
2. deepseek-intro → Hosted LLM via OpenAI-compatible API
3. translation    → Specialized behavior (system prompts)
4. think          → Reasoning ability
5. batch          → Parallel processing
6. coding         → Streaming & control
7. simple-agent   → Tool use (function calling)
8. memory-agent   → Persistent state
9. react-agent    → Strategic reasoning + tool use
```

### Architecture Patterns

**Simple Agent (Chapters 1-5)**
```text
User → LLM → Response
```

**Tool-Using Agent (Chapter 7)**
```text
User → LLM ⟷ Tools → Response
```

**Memory Agent (Chapter 8)**
```text
User → LLM ⟷ Tools → Response
       ↕
     Memory
```

**ReAct Agent (Chapter 9)**
```text
User → LLM → Think → Act → Observe
       ↑      ↓      ↓      ↓
       └──────┴──────┴──────┘
           Iterate until solved
```

---

## Helper Utilities

### `AiAgents.Core`

A shared .NET class library under `src/AiAgents.Core` provides reusable helpers used across chapters:

- `DeepSeekClientFactory` — creates an OpenAI `ChatClient` configured for DeepSeek
- `ConfigurationFactory` — loads `appsettings.json` + `appsettings.Secrets.json`
- `ToolBox` — register .NET delegates as LLM-callable tools
- `MemoryManager` — simple JSON file-based memory
- `JsonParser` — robust JSON parsing with repair attempts
- `VisualizationWriters` — HTML visualization writers for ToT, GoT, CoT, and tool routing
- `RetryHelper` and typed exception taxonomy — resilience helpers

## Project Structure

```text
ai-agents-from-scratch-csharp/
├── README.md
├── DOWNLOAD.md                         ← Model download notes
├── AiAgentsFromScratch.slnx
├── src/
│   ├── AiAgents.Core/                  ← Shared library
│   │   ├── Client/
│   │   ├── Exceptions/
│   │   ├── Memory/
│   │   ├── Parsing/
│   │   ├── Tools/
│   │   └── Visualization/
│   ├── Chapter01/                      ← One console app per chapter
│   ├── Chapter02/
│   │   ...
│   └── Chapter15/
├── examples/
│   ├── 01_intro/
│   │   ├── CODE.md
│   │   └── CONCEPT.md
│   ├── 02_deepseek-intro/
│   │   ...
│   └── 15_tool-routing-embeddings/
│       ├── CODE.md
│       └── CONCEPT.md
├── models/                             ← Optional local GGUF models (Chapter 15)
└── diagrams/
```

## Additional Resources

- **OpenAI .NET SDK**: https://github.com/openai/openai-dotnet
- **DeepSeek API docs**: https://platform.deepseek.com/api-docs
- **LLamaSharp**: https://github.com/SciSharp/LLamaSharp
- **Hugging Face GGUF models**: https://huggingface.co/models?library=gguf

## Contributing

This is a learning resource. Feel free to:
- Suggest improvements to documentation
- Add more example patterns
- Fix bugs or unclear explanations
- Share what you built!

## License

Educational resource — use and modify as needed for learning.

---

**Built with care for people who want to truly understand AI agents**

Start with `src/Chapter01` and work your way through. Each example builds on the previous one. Read both `CODE.md` and `CONCEPT.md` for full understanding.

Happy learning!
