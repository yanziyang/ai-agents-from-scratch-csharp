using AiAgents.Core.Client;
using AiAgents.Core.Parsing;
using AiAgents.Core.Visualization;
using OpenAI.Chat;
using System.Text.Json;

Console.WriteLine("=== Tree of Thought: Motivation Analysis ===\n");

var config = ConfigurationFactory.Create();
var chatClient = DeepSeekClientFactory.CreateChatClient(config);

var hypothesisTypes = new[]
{
    "Avoidance motivation (escaping something negative)",
    "Burnout and emotional exhaustion",
    "Growth motivation (moving toward something better)",
    "External social pressure (family, partner, society)"
};

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
    MaxOutputTokenCount = 1200,
    Temperature = 0.2f
};

async Task<JsonElement> PromptJsonAsync(string prompt)
{
    var requestMessages = new List<ChatMessage>(messages) { ChatMessage.CreateUserMessage(prompt) };
    var response = await chatClient.CompleteChatAsync(requestMessages, jsonOptions);
    return JsonParser.Parse(response.Value.Content[0].Text);
}

async Task<JsonElement> DevelopHypothesisAsync(string behavior, string hypothesisType)
{
    return await PromptJsonAsync($"""
        You are an experienced psychologist. Analyze this behavior strictly through one lens:
        "{hypothesisType}".

        Behavior:
        "{behavior}"

        Develop one coherent explanation.
        Think only in this direction and ignore other possible explanations.

        Return JSON with fields: name, argument, signals (array of strings), counter_evidence (array of strings).
        """);
}

async Task<ScoredHypothesis> ScoreHypothesisAsync(string behavior, JsonElement hypothesis)
{
    var scored = await PromptJsonAsync($"""
        You are a critical psychologist. Score this hypothesis.

        Behavior:
        "{behavior}"

        Hypothesis argument:
        "{hypothesis.GetProperty("argument").GetString()}"

        Supporting signals: {string.Join(", ", hypothesis.GetProperty("signals").EnumerateArray().Select(x => x.GetString()))}
        Counter evidence: {string.Join(", ", hypothesis.GetProperty("counter_evidence").EnumerateArray().Select(x => x.GetString()))}

        Scoring criteria:
        - explanatory_power (1-10): how much behavior this explains
        - plausibility (1-10): how psychologically grounded it is
        - falsifiability (1-10): how testable/challengeable it is
        Use decimal scores (one decimal place) for all criteria and total.
        Compute total with this formula:
        total = 0.45 * explanatory_power + 0.35 * plausibility + 0.20 * falsifiability

        Return JSON with fields: explanatory_power, plausibility, falsifiability, total, reasoning, blind_spot.
        """);

    return new ScoredHypothesis(
        new Hypothesis(
            hypothesis.GetProperty("name").GetString()!,
            hypothesis.GetProperty("argument").GetString()!,
            hypothesis.GetProperty("signals").EnumerateArray().Select(x => x.GetString()!).ToList(),
            hypothesis.GetProperty("counter_evidence").EnumerateArray().Select(x => x.GetString()!).ToList()),
        scored.GetProperty("total").GetDouble(),
        scored.GetProperty("reasoning").GetString()!,
        scored.GetProperty("blind_spot").GetString()!,
        new ScoreDetails(
            scored.GetProperty("explanatory_power").GetDouble(),
            scored.GetProperty("plausibility").GetDouble(),
            scored.GetProperty("falsifiability").GetDouble()));
}

async Task<List<ScoredHypothesis>> RerankHypothesesAsync(string behavior, List<ScoredHypothesis> scored)
{
    var payload = string.Join("\n\n", scored.Select((s, i) =>
        $"#{i + 1}: {s.Hypothesis.Name}\nscore={s.Score}\nargument={s.Hypothesis.Argument}"));

    var ranked = await PromptJsonAsync($"""
        Rank these hypotheses from strongest to weakest.
        You must return a strict ranking with no ties.

        Behavior:
        "{behavior}"

        Hypotheses:
        {payload}

        Return JSON with fields: ranking (array of integers), rationale (string).
        """);

    var order = ranked.GetProperty("ranking").EnumerateArray().Select(x => x.GetInt32()).ToList();
    if (order.Count != scored.Count || order.ToHashSet().Count != scored.Count)
        return scored;

    const double baseScore = 8.8;
    const double step = 0.7;
    var adjusted = new List<ScoredHypothesis>(scored);
    for (int pos = 0; pos < order.Count; pos++)
    {
        var idx = order[pos] - 1;
        if (idx >= 0 && idx < adjusted.Count)
        {
            var original = adjusted[idx];
            adjusted[idx] = original with { Score = Math.Round(baseScore - pos * step, 1) };
        }
    }

    return adjusted;
}

async Task<JsonElement> CreateConclusionAsync(string behavior, ScoredHypothesis winner)
{
    return await PromptJsonAsync($"""
        You are an experienced psychologist writing a case analysis.
        Base this analysis only on the strongest hypothesis.

        Behavior:
        "{behavior}"

        Leading hypothesis:
        "{winner.Hypothesis.Name}"

        Core argument:
        "{winner.Hypothesis.Argument}"

        Strengths:
        {string.Join(", ", winner.Hypothesis.Signals)}

        Return JSON with fields: summary, psychological_background, recommendation, open_questions (array of strings).
        """);
}

Console.WriteLine($"Behavior: \"{behaviorInput}\"\n");

Console.WriteLine("Phase 1: Branch - develop 4 competing hypotheses");
var hypotheses = new List<JsonElement>();
foreach (var type in hypothesisTypes)
{
    Console.Write($"  Developing \"{type.Split('(')[0].Trim()}\" ... ");
    var h = await DevelopHypothesisAsync(behaviorInput, type);
    hypotheses.Add(h);
    Console.WriteLine("done");
}

Console.WriteLine("\nPhase 2: Score - evaluate each hypothesis independently");
var scored = new List<ScoredHypothesis>();
foreach (var h in hypotheses)
{
    Console.Write($"  Scoring \"{h.GetProperty("name").GetString()}\" ... ");
    var s = await ScoreHypothesisAsync(behaviorInput, h);
    scored.Add(s);
    Console.WriteLine("captured");
}

scored = await RerankHypothesesAsync(behaviorInput, scored);

Console.WriteLine("  Calibrated scores (used for pruning):");
foreach (var s in scored.OrderByDescending(x => x.Score))
{
    Console.WriteLine($"    - {s.Hypothesis.Name.Split('(')[0].Trim()}: {s.Score}/10");
    Console.WriteLine($"      criteria: explanatory_power={s.Details.ExplanatoryPower:F1} | plausibility={s.Details.Plausibility:F1} | falsifiability={s.Details.Falsifiability:F1}");
    Console.WriteLine($"      blind spot: \"{s.BlindSpot[..Math.Min(s.BlindSpot.Length, 95)]}...\"");
}

Console.WriteLine("\nPhase 3: Prune - keep winner, drop the rest");
var winner = scored.OrderByDescending(x => x.Score).First();
var discarded = scored.Where(x => x != winner).ToList();
Console.WriteLine($"  Winner: \"{winner.Hypothesis.Name}\" (score: {winner.Score})");
foreach (var d in discarded)
{
    Console.WriteLine($"  Discarded: \"{d.Hypothesis.Name}\" (score: {d.Score})");
}

Console.WriteLine("\nPhase 4: Conclusion - analyze from winner only");
var analysis = await CreateConclusionAsync(behaviorInput, winner);

Console.WriteLine("\n" + new string('=', 64));
Console.WriteLine("MOTIVATION ANALYSIS (Tree of Thought)");
Console.WriteLine(new string('=', 64));
Console.WriteLine($"\nLeading hypothesis: \"{winner.Hypothesis.Name}\"");
Console.WriteLine($"\nSummary:\n{analysis.GetProperty("summary").GetString()}");
Console.WriteLine($"\nPsychological background:\n{analysis.GetProperty("psychological_background").GetString()}");
Console.WriteLine($"\nRecommendation:\n{analysis.GetProperty("recommendation").GetString()}");

var coreScored = scored.Select(s => new VisualizationWriters.ScoredHypothesis(
    new VisualizationWriters.Hypothesis(s.Hypothesis.Name, s.Hypothesis.Argument, s.Hypothesis.Signals, s.Hypothesis.CounterEvidence),
    s.Score,
    s.Reasoning,
    s.BlindSpot,
    new VisualizationWriters.ScoreDetails(s.Details.ExplanatoryPower, s.Details.Plausibility, s.Details.Falsifiability))).ToList();

VisualizationWriters.WriteTreeOfThoughtVisualization("./", coreScored, winner.Hypothesis.Name, analysis);

record Hypothesis(string Name, string Argument, List<string> Signals, List<string> CounterEvidence);
record ScoreDetails(double ExplanatoryPower, double Plausibility, double Falsifiability);
record ScoredHypothesis(Hypothesis Hypothesis, double Score, string Reasoning, string BlindSpot, ScoreDetails Details);
