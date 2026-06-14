
using AiAgents.Core.Client;
using AiAgents.Core.Parsing;
using AiAgents.Core.Visualization;
using OpenAI.Chat;
using System.Text.Json;

Console.WriteLine("=== Graph of Thought: Motivation Analysis ===\n");

var config = ConfigurationFactory.Create();
var chatClient = DeepSeekClientFactory.CreateChatClient(config);

const string behaviorInput = "A 34-year-old woman resigns from a secure office job, withdraws from friends, and starts taking long solo walks daily without explaining why.";

var messages = new List<ChatMessage>
{
    ChatMessage.CreateSystemMessage("""
        You are a careful psychology analysis assistant.
        You always return valid JSON only, matching the provided schema.
        No markdown, no code fences, no text outside JSON.
        """)
};

var jsonOptions = new ChatCompletionOptions
{
    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
    MaxOutputTokenCount = 1400,
    Temperature = 0.2f
};

async Task<JsonElement> PromptJsonAsync(string prompt)
{
    for (var attempt = 1; ; attempt++)
    {
        var requestMessages = new List<ChatMessage>(messages) { ChatMessage.CreateUserMessage(prompt) };
        var response = await chatClient.CompleteChatAsync(requestMessages, jsonOptions);
        var text = response.Value.Content[0].Text;
        try
        {
            return JsonParser.Parse(text, debug: true);
        }
        catch (JsonException ex) when (attempt < 3)
        {
            var delay = 300 * attempt + Random.Shared.Next(0, 200);
            Console.WriteLine($"[retry] JSON parse failed (attempt {attempt}/3): {ex.Message}. Retrying in {delay}ms.");
            await Task.Delay(delay);
        }
    }
}

var graph = new ThoughtGraph();
var rootId = graph.AddNode("root", behaviorInput, null);

var hypothesisTypes = new[]
{
    "Avoidance motivation (escaping something negative)",
    "Burnout and emotional exhaustion",
    "Growth motivation (moving toward something better)",
    "External social pressure (family, partner, society)"
};

Console.WriteLine("Phase 1: Branch into competing hypotheses");
var hypothesisIds = new List<string>();
foreach (var type in hypothesisTypes)
{
    var h = await PromptJsonAsync($"""
        You are an experienced psychologist. Analyze this behavior strictly through this lens:
        "{type}".

        Behavior:
        "{behaviorInput}"

        Return JSON with fields: name (string), argument (string), signals (array of strings), counter_evidence (array of strings).
        """);

    var id = graph.AddNode("hypothesis", h.GetProperty("argument").GetString()!, new Dictionary<string, object?>
    {
        ["name"] = h.GetProperty("name").GetString(),
        ["signals"] = h.GetProperty("signals").EnumerateArray().Select(x => x.GetString()!).ToList(),
        ["counter_evidence"] = h.GetProperty("counter_evidence").EnumerateArray().Select(x => x.GetString()!).ToList()
    }, rootId);
    hypothesisIds.Add(id);
}

Console.WriteLine("\nPhase 2: Score and rerank hypotheses");
var scoredList = new List<ScoredNode>();
foreach (var id in hypothesisIds)
{
    var node = graph.Get(id)!;
    var scored = await PromptJsonAsync($"""
        Score this hypothesis.

        Behavior:
        "{behaviorInput}"

        Hypothesis name: {node.Meta!["name"]}
        Argument: {node.Content}
        Signals: {string.Join(", ", (List<string>)node.Meta!["signals"]!)}
        Counter evidence: {string.Join(", ", (List<string>)node.Meta!["counter_evidence"]!)}

        Use decimal scores.
        Return JSON with fields: explanatory_power, plausibility, falsifiability, total, reasoning.
        """);

    double total = scored.GetProperty("total").GetDouble();
    node.Meta!["score"] = total;
    node.Meta!["reasoning"] = scored.GetProperty("reasoning").GetString();
    scoredList.Add(new ScoredNode(id, total));
}

var ordered = Rerank(scoredList);
foreach (var s in ordered)
{
    graph.Get(s.Id)!.Meta!["score"] = s.Score;
}

var strongA = ordered[0].Id;
var strongB = ordered[1].Id;
var medium = ordered[2].Id;
var weak = ordered[3].Id;

Console.WriteLine($"  Strong A: {graph.Get(strongA)!.Meta!["name"]} ({ordered[0].Score})");
Console.WriteLine($"  Strong B: {graph.Get(strongB)!.Meta!["name"]} ({ordered[1].Score})");
Console.WriteLine($"  Medium:   {graph.Get(medium)!.Meta!["name"]} ({ordered[2].Score})");
Console.WriteLine($"  Weak:     {graph.Get(weak)!.Meta!["name"]} ({ordered[3].Score})");

Console.WriteLine("\nPhase 3: Contrast strong hypotheses against others");
var contrastAId = await ContrastAsync(strongA, strongB, graph);
var contrastBId = await ContrastAsync(strongA, weak, graph);

Console.WriteLine("\nPhase 4: Refine weak/medium hypotheses using other strands");
var refinedWeakId = await RefineAsync(weak, strongA, graph);
var refinedMediumId = await RefineAsync(medium, contrastAId, graph);

Console.WriteLine("\nPhase 5: Aggregate partial syntheses");
var synthesisAId = await AggregateAsync("Synthesis from top hypotheses and their contrast", new[] { strongA, strongB, contrastAId }, graph);
var synthesisBId = await AggregateAsync("Synthesis from refined strands and remaining contrast", new[] { refinedWeakId, refinedMediumId, contrastBId }, graph);

Console.WriteLine("\nPhase 6: Conclude from multiple graph strands");
var conclusionId = await ConcludeAsync(new[] { synthesisAId, synthesisBId, contrastAId, contrastBId, refinedWeakId }, graph);

Console.WriteLine("\n" + new string('=', 64));
Console.WriteLine("FINAL GRAPH SUMMARY");
Console.WriteLine(new string('=', 64));
graph.PrintGraph();

var conclusionNode = graph.Get(conclusionId)!;
Console.WriteLine("\n--- Integrated conclusion ---");
Console.WriteLine(conclusionNode.Content);

var analysisObj = new
{
    summary = conclusionNode.Content,
    winner = graph.Get(strongA)!.Meta!["name"]
};

VisualizationWriters.WriteGraphOfThoughtVisualization("./", graph.ToGraphNodes(), graph.ToGraphEdges(), analysisObj);

async Task<string> ContrastAsync(string aId, string bId, ThoughtGraph g)
{
    var a = g.Get(aId)!;
    var b = g.Get(bId)!;

    var result = await PromptJsonAsync($"""
        Compare these two psychological hypotheses and find the most productive contradiction or tension between them.

        Hypothesis A ({a.Meta!["name"]}):
        {a.Content}

        Hypothesis B ({b.Meta!["name"]}):
        {b.Content}

        Return JSON with fields: label (short name), contradiction (string), insight (string).
        """);

    var label = result.GetProperty("label").GetString()!;
    var content = $"{label}: {result.GetProperty("contradiction").GetString()}\n\nInsight: {result.GetProperty("insight").GetString()}";
    var id = g.AddNode("contrast", content, new Dictionary<string, object?> { ["name"] = label }, aId, bId);
    Console.WriteLine($"  Contrast created: {label} ({a.Meta!["name"]} vs {b.Meta!["name"]})");
    return id;
}

async Task<string> RefineAsync(string weakId, string sourceId, ThoughtGraph g)
{
    var weakNode = g.Get(weakId)!;
    var sourceNode = g.Get(sourceId)!;

    var result = await PromptJsonAsync($"""
        Improve the following weak or medium hypothesis by integrating useful signals from another graph strand.
        Do not change the core lens of the weak hypothesis; strengthen it.

        Weak hypothesis ({weakNode.Meta!["name"]}):
        {weakNode.Content}

        Source strand ({sourceNode.Meta!["name"] ?? "contrast"}):
        {sourceNode.Content}

        Return JSON with fields: refined_argument (string), improvement_note (string).
        """);

    var content = $"{weakNode.Meta!["name"]} (refined): {result.GetProperty("refined_argument").GetString()}";
    var id = g.AddNode("refined", content, new Dictionary<string, object?> { ["name"] = weakNode.Meta!["name"], ["note"] = result.GetProperty("improvement_note").GetString() }, weakId, sourceId);
    Console.WriteLine($"  Refined: {weakNode.Meta!["name"]} using {sourceNode.Meta!["name"] ?? "contrast"}");
    return id;
}

async Task<string> AggregateAsync(string label, string[] sourceIds, ThoughtGraph g)
{
    var sources = sourceIds.Select(id => g.Get(id)!).ToList();
    var payload = string.Join("\n\n", sources.Select(s => $"- {s.Meta!["name"] ?? label}: {s.Content}"));

    var result = await PromptJsonAsync($"""
        Synthesize the following strands into one coherent intermediate view. Preserve tensions and contradictions.

        {payload}

        Return JSON with fields: synthesis (string).
        """);

    var content = result.GetProperty("synthesis").GetString()!;
    var id = g.AddNode("synthesis", content, new Dictionary<string, object?> { ["name"] = label }, sourceIds);
    Console.WriteLine($"  Synthesis created from {sourceIds.Length} strands");
    return id;
}

async Task<string> ConcludeAsync(string[] sourceIds, ThoughtGraph g)
{
    var sources = sourceIds.Select(id => g.Get(id)!).ToList();
    var payload = string.Join("\n\n", sources.Select((s, i) => $"Strand {i + 1} ({s.Meta!["name"] ?? s.Type}): {s.Content}"));

    var result = await PromptJsonAsync($"""
        You are an experienced psychologist writing an integrated case analysis.
        Combine all the following reasoning strands into one final conclusion.
        Preserve contradictions and note open questions.

        Behavior:
        "{behaviorInput}"

        {payload}

        Return JSON with fields: conclusion (string), open_questions (array of strings).
        """);

    var content = result.GetProperty("conclusion").GetString()!;
    var id = g.AddNode("conclusion", content, new Dictionary<string, object?> { ["open_questions"] = result.GetProperty("open_questions").EnumerateArray().Select(x => x.GetString()!).ToList() }, sourceIds);
    return id;
}

List<ScoredNode> Rerank(List<ScoredNode> scored)
{
    const double baseScore = 8.8;
    const double step = 0.7;
    var ordered = scored.OrderByDescending(s => s.Score).ToList();
    for (int i = 0; i < ordered.Count; i++)
    {
        ordered[i] = ordered[i] with { Score = Math.Round(baseScore - i * step, 1) };
    }
    return ordered;
}

class ThoughtGraph
{
    private int _next = 1;
    private readonly Dictionary<string, Node> _nodes = new();
    private readonly List<(string From, string To)> _edges = new();

    public string AddNode(string type, string content, Dictionary<string, object?>? meta = null, params string[] parentIds)
    {
        var id = $"n{_next++}";
        _nodes[id] = new Node(id, type, content, meta ?? new Dictionary<string, object?>());
        foreach (var p in parentIds)
        {
            _edges.Add((p, id));
        }
        return id;
    }

    public Node? Get(string id) => _nodes.TryGetValue(id, out var n) ? n : null;

    public void PrintGraph()
    {
        foreach (var node in _nodes.Values.OrderBy(n => n.Id))
        {
            var score = node.Meta.TryGetValue("score", out var s) ? $" [score {s}]" : "";
            Console.WriteLine($"{node.Id} [{node.Type}]{score}: {node.Content[..Math.Min(node.Content.Length, 70)]}...");
        }
        Console.WriteLine("\nEdges:");
        foreach (var (from, to) in _edges)
        {
            Console.WriteLine($"  {from} -> {to}");
        }
    }

    public List<VisualizationWriters.GraphNode> ToGraphNodes()
    {
        return _nodes.Values.Select(n =>
        {
            var label = n.Meta.TryGetValue("name", out var name) && name != null ? name.ToString()! : n.Type;
            double? score = n.Meta.TryGetValue("score", out var s) && s is double d ? d : null;
            return new VisualizationWriters.GraphNode(n.Id, n.Type, label, score);
        }).ToList();
    }

    public List<VisualizationWriters.GraphEdge> ToGraphEdges()
    {
        return _edges.Select(e => new VisualizationWriters.GraphEdge(e.From, e.To)).ToList();
    }
}

record Node(string Id, string Type, string Content, Dictionary<string, object?> Meta);
record ScoredNode(string Id, double Score);
