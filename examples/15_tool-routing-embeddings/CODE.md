# Code explanation: `Program.cs` (Chapter 15)

This example uses **two models**: DeepSeek V4 Flash for chat/tool-calling and a **local GGUF embedding model** (LLamaSharp) for routing. The embedding model scores how close the user message is to short exemplar phrases per tool.

## Run

```bash
cd src/Chapter15
dotnet run
```

Requires a local embedding GGUF under `models/` (configured in `appsettings.json` as `Embeddings:ModelPath`).
The example defaults to `models/bge-small-en-v1.5-q8_0.gguf`.

> Copy `appsettings.Secrets.example.json` to `appsettings.Secrets.json` and add your DeepSeek API key before running.

---

## 1) Two routing functions: `ScoreTools` and `SelectToolKeys`

The routing logic is deliberately split into two small, inspectable methods. You can unit-test them independently and swap either one without touching the other.

**`ScoreTools`** - query vs. all exemplars, return a score per tool:

```csharp
static Dictionary<string, double> ScoreTools(float[] query, List<ExemplarRow> rows)
{
    var maxByTool = new Dictionary<string, double>();
    foreach (var row in rows)
    {
        var sim = CosineSimilarity(query, row.Embedding);
        if (!maxByTool.TryGetValue(row.ToolKey, out var current) || sim > current)
        {
            maxByTool[row.ToolKey] = sim;
        }
    }
    return maxByTool;
}
```

Each tool can have several exemplar strings. The score used is the **maximum** across all of them, not the mean. A single strong exemplar match keeps a tool competitive even if the other phrasings miss.

**`SelectToolKeys`** - pick the top-k tools plus any pinned tools:

```csharp
static HashSet<string> SelectToolKeys(Dictionary<string, double> scores, int k, List<string> alwaysInclude)
{
    var ranked = scores.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToList();
    var selected = new HashSet<string>(alwaysInclude);
    foreach (var key in ranked)
    {
        if (selected.Count >= alwaysInclude.Count + k) break;
        if (selected.Contains(key)) continue;
        selected.Add(key);
    }
    return selected;
}
```

Pinned tools are added first and do **not** consume a retrieval slot. Requesting `k=1` with one pinned tool yields two tools total, not one.

---

## 2) Tool catalog (12 IT helpdesk tools)

Every tool is declared with `ChatTool.CreateFunctionTool` plus a handler. The handlers return canned JSON so the lesson stays focused on routing, not business logic.

```csharp
Make("checkVPNStatus", "Check VPN client status, the active profile, and tunnel health for a user.",
    new { username = (string?)null },
    args => JsonSerializer.Serialize(new
    {
        username = args.username ?? "current_user",
        vpn_client = "Cisco AnyConnect 4.10",
        connected = false,
        // ...
    }))
```

The twelve tools cover five distinct semantic clusters. Distinct clusters matter: routing works because embedding similarity separates them.

| Cluster | Tools |
|---|---|
| Connectivity | `checkNetworkConnectivity`, `checkVPNStatus` |
| System health | `getSystemLogs`, `runDiagnostic`, `getHardwareInfo` |
| Storage / software | `checkDiskSpace`, `getInstalledSoftware` |
| Account / access | `getUserAccount`, `resetPassword` |
| Operations | `restartService`, `createSupportTicket`, `escalateToSpecialist` |

---

## 3) `Exemplars` and cold-start embedding

`Exemplars.Load()` returns a flat list of `(ToolKey, Text)` pairs — three phrasings per tool (36 total). The strings are **not** copied from the demo prompts; they are paraphrases so the example tests semantic generalization, not verbatim recall.

At startup, every exemplar is embedded once and cached:

```csharp
var exemplarRows = new List<ExemplarRow>();
foreach (var e in exemplars)
{
    var embedding = await GetEmbeddingAsync(e.Text);
    exemplarRows.Add(new ExemplarRow(e.ToolKey, e.Text, embedding));
}
```

In a production service you would persist these vectors and rebuild them only when the tool catalog changes.

---

## 4) Model setup: chat + local embedding model

The chat model uses the OpenAI .NET SDK pointed at DeepSeek:

```csharp
var chatClient = DeepSeekClientFactory.CreateChatClient(config);
```

The embedding model uses LLamaSharp:

```csharp
var parameters = new ModelParams(modelPath) { Embeddings = true, ContextSize = 512 };
using var weights = LLamaWeights.LoadFromFile(parameters);
using var embedder = new LLamaEmbedder(weights, parameters);
```

---

## 5) `RunWithRoutingAsync` - routing + prompt + logging

Each demo case calls this method. It runs the full pipeline and appends a record to `routingLog` for the visualization:

```csharp
async Task RunWithRoutingAsync(string userPrompt, RoutingOptions options)
{
    var queryEmbedding = await GetEmbeddingAsync(userPrompt);
    var scores = ScoreTools(queryEmbedding, exemplarRows);
    var selectedKeys = SelectToolKeys(scores, options.RetrievalK, options.AlwaysInclude);

    // print ranked table...

    var selectedTools = tools.Where(t => selectedKeys.Contains(t.Key)).ToList();
    var answer = await RunAgentLoopAsync(userPrompt, selectedTools);

    routingLog.Add(new VisualizationWriters.ToolRoutingLogEntry(...));
}
```

The similarity table is the key teaching output. The `x` mark shows exactly which tools the LLM can see. Every other tool is invisible to it for that turn.

---

## 6) Demo cases (read the console)

Cases are called in order. The first four establish baselines; cases 5 and 6 are the **same prompt** with different routing parameters to demonstrate recall failure and how pinning fixes it:

| # | Intent | k | Pinned | Expected tools exposed |
|---|---|---|---|---|
| 1 | VPN failure | 1 | - | `checkVPNStatus` |
| 2 | Locked account | 1 | - | `getUserAccount` or `resetPassword` |
| 3 | Crashing PC | 2 | - | `getSystemLogs` + `runDiagnostic` |
| 4 | Disk full + software | 2 | - | `checkDiskSpace` + `getInstalledSoftware` |
| 5 | VPN + locked account | 1 | - | VPN only - **account tool missing** |
| 6 | VPN + locked account | 1 | `getUserAccount` | Both covered |

---

## 7) Disposal

`weights` and `embedder` are created inside `using` statements so native resources are released when `Main` exits.
