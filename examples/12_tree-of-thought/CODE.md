# Code explanation: `Program.cs` (Chapter 12)

This walkthrough follows the actual C# code structure so you can map each Tree of Thought concept to concrete methods.

## Run

```bash
cd src/Chapter12
dotnet run
```

> The project uses DeepSeek V4 Flash via the OpenAI .NET SDK. Copy `appsettings.Secrets.example.json` to `appsettings.Secrets.json` and add your API key before running.

---

## 1) Setup: model, schemas, constants

At the top of `Program.cs`:

- `hypothesisTypes` defines the four competing branches.
- `behaviorInput` is the case description.
- `PromptJsonAsync()` is the shared helper that:
  - keeps the system message in place,
  - sets `ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()`,
  - parses the model output with `JsonParser.Parse`.

This keeps each phase focused on logic, not JSON boilerplate.

---

## 2) Phase 1 (Branch): `DevelopHypothesisAsync()`

`DevelopHypothesisAsync(behavior, hypothesisType)` does one thing:

- prompts the model to reason through exactly one lens,
- returns a `JsonElement` with fields:
  - `name`
  - `argument`
  - `signals`
  - `counter_evidence`

In `Main`, this runs in a loop over `hypothesisTypes`, creating four competing branches.

---

## 3) Phase 2 (Score): `ScoreHypothesisAsync()` + `RerankHypothesesAsync()`

### Raw per-branch scoring

`ScoreHypothesisAsync(behavior, hypothesis)` returns a `ScoredHypothesis` containing:

- `Score` (raw numeric score from the formula),
- `Details` (`explanatory_power`, `plausibility`, `falsifiability`),
- `BlindSpot`,
- `Reasoning`.

### Anti-tie calibration pass

`RerankHypothesesAsync(behavior, scored)` forces a strict ranking with no ties and then maps ranks to calibrated scores:

- rank 1 -> `8.8`
- rank 2 -> `8.1`
- rank 3 -> `7.4`
- rank 4 -> `6.7`

This is why the console shows:

- captured raw evaluations,
- then calibrated scores used for pruning.

So learners see what the system *actually* uses for branch selection.

---

## 4) Phase 3 (Prune): inline pruning

After ranking, the code keeps only the highest-scoring branch:

```csharp
var winner = scored.OrderByDescending(x => x.Score).First();
var discarded = scored.Where(x => x != winner).ToList();
```

This is the structural heart of ToT: one winner continues, alternatives are dropped.

---

## 5) Phase 4 (Conclusion): `CreateConclusionAsync()`

`CreateConclusionAsync(behavior, winner)` builds the final analysis using only:

- winner name,
- winner argument,
- winner signals.

Discarded branches do not feed into the final answer.

---

## 6) Orchestration flow: `Main`

`Main` is the end-to-end controller:

1. Branch (collect hypotheses).
2. Score (raw + calibrated).
3. Prune (winner + discarded).
4. Conclude (winner only).
5. Print output and call `VisualizationWriters.WriteTreeOfThoughtVisualization(...)`.

Visualization is intentionally delegated to the shared `AiAgents.Core` library so `Program.cs` stays focused on ToT control flow.

---

## Suggested code-reading order

Read the methods in this sequence:

1. `PromptJsonAsync`
2. `DevelopHypothesisAsync`
3. `ScoreHypothesisAsync`
4. `RerankHypothesesAsync`
5. inline pruning code in `Main`
6. `CreateConclusionAsync`
7. `Main`

That order mirrors the runtime flow and makes the file much easier to understand.
