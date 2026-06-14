# Code explanation: `Program.cs` (Chapter 14)

This walkthrough maps each Chain of Thought phase to the actual C# code.

## Run

```bash
cd src/Chapter14
dotnet run
```

> The project uses DeepSeek V4 Flash via the OpenAI .NET SDK. Copy `appsettings.Secrets.example.json` to `appsettings.Secrets.json` and add your API key before running.

---

## 1) Setup: model, input case, and JSON mode

At the top of `Program.cs`:

- `returnCase` defines the customer request.
- `policy` defines hard business constraints.
- `PromptJsonAsync(prompt)` is the shared utility that:
  - keeps the system message in place,
  - sets `ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()`,
  - parses and repairs JSON safely with `JsonParser.Parse`.

This gives each phase method a strict output shape.

```csharp
var returnCase = new
{
    request_id = "RET-2026-0414",
    claimed_reason = "Right ear cup has intermittent sound dropouts",
    claim_timing_days_after_delivery = 23,
    order_value_eur = 189.0,
    // ...
};

var policy = new
{
    return_window_days = 30,
    max_high_value_returns_12m_before_manual_review = 2,
    mandatory_manual_review_amount_eur = 250
};

async Task<JsonElement> PromptJsonAsync(string prompt)
{
    var requestMessages = new List<ChatMessage>(messages)
    {
        ChatMessage.CreateUserMessage(prompt)
    };
    var response = await chatClient.CompleteChatAsync(requestMessages, jsonOptions);
    return JsonParser.Parse(response.Value.Content[0].Text);
}
```

---

## 2) Phase 1 (Facts): `facts`

The prompt asks for:

- only explicit facts,
- no scoring,
- no judgment.

It returns:

- `extracted_facts`
- `missing_information`

This protects against early bias before risk reasoning starts.

---

## 3) Phase 2 (Red Flags): `redFlags`

The red-flag prompt performs explicit fraud screening with fixed checkpoints.

Output:

- `checkpoints[]` with `present/not_present/unclear`
- `fraud_score`
- `fraud_rationale`

The important part is checklist coverage, not just one final score.

---

## 4) Phase 3 (Legitimacy): `legitimacy`

The legitimacy prompt builds the customer-side argument:

- plausible defect indicators,
- fairness/context factors,
- supporting evidence quality.

Output:

- `customer_supporting_points[]`
- `legitimacy_score`
- `legitimacy_rationale`

Without this phase, risk logic tends to dominate every borderline case.

---

## 5) Phase 4 (Policy): `policyResult`

The policy prompt applies hard rules:

- return window,
- value thresholds,
- return-history triggers.

Output:

- per-rule statuses in `policy_checks[]`
- `policy_outcome` (`approve`, `reject`, `manual_review`)

This is the governance gate between analysis and action.

---

## 6) Phase 5 (Decision): `decision`

The decision prompt can decide only after all prior phases.

Output:

- `final_decision`
- `confidence`
- `decision_reasoning`
- `customer_message`
- `internal_note`

The prompt explicitly references conflict handling (for example fraud 6/10 vs legitimacy 7/10), so the result must explain how policy resolves the tension.

---

## 7) Orchestration flow: `Main`

The main controller executes phases in strict order:

1. facts
2. red flags
3. legitimacy
4. policy check
5. final decision

Then it prints a compact report and writes a browser visualization via:

- `VisualizationWriters.WriteChainOfThoughtVisualization(...)`

This keeps the core file focused on CoT logic.

---

## 8) Adapting the implementation per model class

The C# version uses DeepSeek V4 Flash, a capable reasoning-friendly model accessed through the OpenAI .NET SDK. The 5-phase scaffolding works well for reasoning and non-reasoning models, but tuning differs.

For the conceptual side of this distinction, see the "CoT with reasoning vs non-reasoning LLMs" section in [CONCEPT.md](CONCEPT.md).

### What the current code assumes

- Per-phase JSON responses via `PromptJsonAsync(...)` with `ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()`.
- Low `Temperature` (0.2) and a generous `MaxOutputTokenCount` budget per phase.
- A fresh user message per phase while keeping the system prompt constant.

### Tuning for non-reasoning models

If you swap in a base/chat model without internal reasoning (for example a small local instruct model):

```csharp
var jsonOptions = new ChatCompletionOptions
{
    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
    MaxOutputTokenCount = 1800,
    Temperature = 0.1f
};
```

- Lower `Temperature` further (0.05 - 0.15). Borderline cases regress badly with creative sampling.
- Increase `MaxOutputTokenCount` per phase. The model often needs room to "talk to itself" inside the JSON before it commits to scores.
- Keep schemas strict. Avoid wide free-form fields; replace them with enums, fixed-length arrays, or short bounded strings.
- Add explicit examples to phase prompts. Non-reasoning models latch on to format examples much faster than abstract specs.

### Tuning for reasoning models

If you swap in a reasoning-tuned model (DeepSeek-R1, o3, Claude Extended Thinking):

```csharp
var jsonOptions = new ChatCompletionOptions
{
    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
    MaxOutputTokenCount = 900,
    Temperature = 0.3f
};
```

- Shorten phase prompts. The model already reasons internally; verbose instructions add noise.
- Lower `MaxOutputTokenCount` for purely structural phases (Facts, Policy Check). They do not need long thinking budgets.
- Keep schemas as a **contract**, not as a reasoning crutch. Their main job here is downstream interoperability.
- Log any internal reasoning traces for debugging only — never as part of the audit trail.

### Per-phase callouts

- **Phase 1 (Facts)** - non-reasoning models often hallucinate fact entries that look plausible but were never in the input. Tighten the schema and instruct explicitly: "Do not infer."
- **Phase 2 (Red Flags)** - reasoning models tend to over-suspect when given a fraud framing. Anchor them with the fixed checkpoint list rather than open-ended red flag generation.
- **Phase 3 (Legitimacy)** - this phase exists exactly to counter Phase 2's bias. Do not collapse it into Phase 2 to save tokens, regardless of model class. It is a structural counterweight.
- **Phase 4 (Policy Check)** - both classes benefit from injecting the policy as inline JSON rather than describing it in prose. Reduces drift and silent rule invention.
- **Phase 5 (Decision)** - confidence calibration differs sharply between classes. A `confidence: 0.79` from a reasoning model is not directly comparable to `0.79` from a base model. Treat confidence as model-internal; route on `final_decision` and `policy_outcome` instead.

---

## Suggested code-reading order

1. `PromptJsonAsync`
2. Phase 1 facts prompt
3. Phase 2 red flags prompt
4. Phase 3 legitimacy prompt
5. Phase 4 policy prompt
6. Phase 5 decision prompt
7. `Main`

That sequence mirrors runtime and makes the example easy to reason about.
