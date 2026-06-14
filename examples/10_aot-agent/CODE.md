# Code Explanation: Chapter 10 — Atom of Thought (AoT) Agent

This example demonstrates the **Atom of Thought** pattern: the LLM generates an explicit JSON plan, the system validates it, and then the system executes it deterministically.

> **Source code:** `src/Chapter10/Program.cs`
> **Run:** `dotnet run --project src/Chapter10`

## Three-Phase Architecture

### Phase 1: Planning (LLM)

```csharp
var options = new ChatCompletionOptions
{
    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
    MaxOutputTokenCount = 1000
};

var response = await chatClient.CompleteChatAsync(messages, options);
var planText = response.Value.Content[0].Text;
var plan = JsonParser.Parse(planText);
```

- `ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()` tells DeepSeek to return valid JSON.
- The system prompt describes the exact atom schema.
- `JsonParser.Parse` cleans and parses the output.

### Phase 2: Validation

```csharp
JsonParser.ValidatePlan(plan, tools.Keys.ToArray(), decisions.Keys.ToArray());
```

Checks:

- Atoms array exists.
- IDs are unique.
- Tool and decision names are known.
- Tool atoms have an input object.

### Phase 3: Execution

```csharp
var state = new Dictionary<int, double>();
foreach (var atom in sortedAtoms)
{
    // resolve <result_of_N> references
    // run tool / decision / final
    state[id] = result;
}
```

- Atoms run in ID order.
- References like `"<result_of_1>"` are replaced with values from `state`.
- Execution is deterministic and auditable.

## Example Plan

```json
{
  "atoms": [
    {"id": 1, "kind": "tool", "name": "divide", "input": {"a": 100, "b": 5}, "dependsOn": []},
    {"id": 2, "kind": "tool", "name": "add", "input": {"a": "<result_of_1>", "b": 3}, "dependsOn": [1]},
    {"id": 3, "kind": "tool", "name": "multiply", "input": {"a": "<result_of_2>", "b": 2}, "dependsOn": [2]},
    {"id": 4, "kind": "final", "name": "report", "dependsOn": [3]}
  ]
}
```

## AoT vs ReAct

| Aspect | ReAct | AoT |
|--------|-------|-----|
| Planning | Implicit in reasoning | Explicit JSON plan |
| Execution | LLM-driven | System-driven |
| Validation | Limited | Before execution |
| Debugging | Read text trace | Inspect atom N |
| Testing | Hard | Test executor independently |

## Key Takeaways

1. AoT separates planning from execution.
2. JSON mode enforces structured output.
3. Validation catches errors before running tools.
4. Execution is deterministic and easy to debug.
5. Best for multi-step, auditable workflows.
