# Code explanation: `Program.cs` (Chapter 13)

This is a code-first walkthrough of the Graph of Thought implementation used in Example 13.

## Run

```bash
cd src/Chapter13
dotnet run
```

> The project uses DeepSeek V4 Flash via the OpenAI .NET SDK. Copy `appsettings.Secrets.example.json` to `appsettings.Secrets.json` and add your API key before running.

---

## 1) Core graph object: `ThoughtGraph`

`ThoughtGraph` is the central data structure.

### Stored state

- `_nodes: Dictionary<string, Node>`
- `_edges: List<(string From, string To)>`
- `_next` for sequential node ids (`n1`, `n2`, ...)

### Key methods

- `AddNode(type, content, meta?, parentIds...)`
- `Get(id)`
- `PrintGraph()`
- `ToGraphNodes()` / `ToGraphEdges()` for visualization

This is what makes the example truly graph-based instead of tree-based.

---

## 2) Shared JSON call utility: `PromptJsonAsync()`

`PromptJsonAsync(prompt)` is reused by every operation:

- keeps the system message in place,
- sets `ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()`,
- parses JSON safely with `JsonParser.Parse`.

All operation functions are then clean and focused on graph logic.

---

## 3) Phase functions (and the node type each creates)

### `branch` -> `hypothesis` nodes

- input: root behavior + hypothesis lenses
- output: one node per lens
- parent: always root

### `score` loop -> updates `score` on hypothesis nodes

- raw criterion scoring per hypothesis
- strict reranking pass (no ties) with calibrated spread
- writes score back into graph nodes

### `ContrastAsync(...)` -> `contrast` nodes

- input: two hypothesis nodes
- output: contradiction node
- parents: both compared nodes

### `RefineAsync(...)` -> `refined` nodes

- input: weak/medium node + strong node/context
- output: improved version of the argument
- parents: both source nodes

### `AggregateAsync(...)` -> `synthesis` nodes

- input: multiple source nodes
- output: integrated synthesis
- parents: all source nodes

### `ConcludeAsync(...)` -> `conclusion` node

- input: selected high-value strands
- output: final integrated analysis
- parents: multiple synthesis/contrast/refined nodes

---

## 4) Controller flow: `Main`

`Main` orchestrates everything:

1. Create `root`.
2. Branch into four hypotheses.
3. Score + rerank hypotheses.
4. Build contrast nodes.
5. Refine weak/medium nodes.
6. Create two synthesis nodes.
7. Conclude from multiple strands.
8. Print the graph + final narrative + generate visualization.

The ranking step affects which nodes are considered `strongA`, `strongB`, `medium`, `weak`, which then influences contrast/refine selection.

---

## 5) Why this is GoT in code (not just in concept)

Look at the `parentIds` arrays passed to `AddNode(...)`:

- `contrast`: two parents
- `refine`: two parents
- `aggregate`: many parents
- `conclusion`: many parents

Multiple-parent nodes are impossible in strict tree search; they are the concrete code signature of GoT.

---

## 6) Visualization integration

Visualization logic is intentionally extracted to the shared library:

- `VisualizationWriters.WriteGraphOfThoughtVisualization(...)`

So `Program.cs` stays focused on graph operations and orchestration.

---

## Suggested code-reading order

1. `ThoughtGraph` class
2. `PromptJsonAsync`
3. branch code in `Main`
4. score loop in `Main`
5. `ContrastAsync`
6. `RefineAsync`
7. `AggregateAsync`
8. `ConcludeAsync`
9. `Main`

This gives you the same order as runtime execution and the cleanest learning path.
