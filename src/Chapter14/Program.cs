using AiAgents.Core.Client;
using AiAgents.Core.Parsing;
using AiAgents.Core.Visualization;
using OpenAI.Chat;
using System.Text.Json;

Console.WriteLine("=== Chain of Thought: Return Decision ===\n");

var config = ConfigurationFactory.Create();
var chatClient = DeepSeekClientFactory.CreateChatClient(config);

var returnCase = new
{
    request_id = "RET-2026-0414",
    claimed_reason = "Right ear cup has intermittent sound dropouts",
    claim_timing_days_after_delivery = 23,
    order_value_eur = 189.0,
    account_age_months = 14,
    returns_last_12m = 1,
    evidence_provided = "text_description",
    payment_method_matched_shipping = true
};

var policy = new
{
    return_window_days = 30,
    max_high_value_returns_12m_before_manual_review = 2,
    mandatory_manual_review_amount_eur = 250
};

var messages = new List<ChatMessage>
{
    ChatMessage.CreateSystemMessage("""
        You are a careful return-decision assistant.
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
    var requestMessages = new List<ChatMessage>(messages) { ChatMessage.CreateUserMessage(prompt) };
    var response = await chatClient.CompleteChatAsync(requestMessages, jsonOptions);
    return JsonParser.Parse(response.Value.Content[0].Text);
}

Console.WriteLine("Return case:");
Console.WriteLine(JsonSerializer.Serialize(returnCase, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine();

Console.WriteLine("Phase 1: Extract facts only");
var facts = await PromptJsonAsync($"""
    Phase 1 of 5: FACTS ONLY.
    Extract facts from the return request without evaluation, suspicion, or judgment.
    Do not infer intent. Do not score. Just capture what is explicitly known.

    Return request JSON:
    {JsonSerializer.Serialize(returnCase)}

    Return JSON with fields: extracted_facts (array of strings), missing_information (array of strings).
    """);

Console.WriteLine("Phase 2: Screen red flags");
var redFlags = await PromptJsonAsync($"""
    Phase 2 of 5: RED FLAG SCREENING.
    Evaluate potential fraud indicators one by one.

    Return request JSON:
    {JsonSerializer.Serialize(returnCase)}

    Known facts:
    {string.Join("\n", facts.GetProperty("extracted_facts").EnumerateArray().Select(x => "- " + x.GetString()))}

    Use these checkpoints:
    1) Frequent recent return behavior
    2) High-value return pattern
    3) Inconsistent payment/shipping identity
    4) Weak or missing defect evidence
    5) Timing pattern that looks strategic
    6) Account behavior anomaly

    Return JSON with fields: checkpoints (array of objects with name, status, evidence), fraud_score (0-10), fraud_rationale (string).
    """);

Console.WriteLine("Phase 3: Assess legitimacy");
var legitimacy = await PromptJsonAsync($"""
    Phase 3 of 5: LEGITIMACY VIEW.
    Now build the customer-side case.
    List reasons why this may be a legitimate return.
    Do not reference fraud score. Focus on fairness and plausible product failure.

    Return request JSON:
    {JsonSerializer.Serialize(returnCase)}

    Known facts:
    {string.Join("\n", facts.GetProperty("extracted_facts").EnumerateArray().Select(x => "- " + x.GetString()))}

    Return JSON with fields: customer_supporting_points (array of strings), legitimacy_score (0-10), legitimacy_rationale (string).
    """);

Console.WriteLine("Phase 4: Check policy");
var policyResult = await PromptJsonAsync($"""
    Phase 4 of 5: POLICY CHECK.
    Apply policy strictly. Do not invent rules.

    Policy JSON:
    {JsonSerializer.Serialize(policy)}

    Return request JSON:
    {JsonSerializer.Serialize(returnCase)}

    Fraud score: {redFlags.GetProperty("fraud_score").GetDouble()}
    Legitimacy score: {legitimacy.GetProperty("legitimacy_score").GetDouble()}

    Return JSON with fields: policy_checks (array of objects with rule, status), policy_outcome (approve|reject|manual_review), rationale (string).
    """);

Console.WriteLine("Phase 5: Make final decision");
var decision = await PromptJsonAsync($"""
    Phase 5 of 5: FINAL DECISION.
    You can decide only now. Use all prior phases.
    Explain trade-offs clearly. If conflict exists (e.g., fraud 6/10 vs legitimacy 7/10), show how policy resolves it.

    Return request JSON:
    {JsonSerializer.Serialize(returnCase)}

    Policy outcome: {policyResult.GetProperty("policy_outcome").GetString()}
    Fraud score: {redFlags.GetProperty("fraud_score").GetDouble()}
    Legitimacy score: {legitimacy.GetProperty("legitimacy_score").GetDouble()}

    Return JSON with fields: final_decision (approve|reject|manual_review), confidence (0-1), decision_reasoning (string), customer_message (string), internal_note (string).
    """);

Console.WriteLine("\n" + new string('=', 64));
Console.WriteLine("DECISION REPORT");
Console.WriteLine(new string('=', 64));
Console.WriteLine($"Final decision: {decision.GetProperty("final_decision").GetString()}");
Console.WriteLine($"Confidence: {decision.GetProperty("confidence").GetDouble():F2}");
Console.WriteLine($"Reasoning: {decision.GetProperty("decision_reasoning").GetString()}");
Console.WriteLine($"\nCustomer message:\n{decision.GetProperty("customer_message").GetString()}");
Console.WriteLine($"\nInternal note:\n{decision.GetProperty("internal_note").GetString()}");

var phases = new List<VisualizationWriters.CoTPhase>
{
    new("Facts", "complete", string.Join("; ", facts.GetProperty("extracted_facts").EnumerateArray().Select(x => x.GetString()!))),
    new("Red Flags", "complete", redFlags.GetProperty("fraud_rationale").GetString()!, redFlags.GetProperty("fraud_score").GetDouble()),
    new("Legitimacy", "complete", legitimacy.GetProperty("legitimacy_rationale").GetString()!, legitimacy.GetProperty("legitimacy_score").GetDouble()),
    new("Policy Check", policyResult.GetProperty("policy_outcome").GetString()!, $"Outcome: {policyResult.GetProperty("policy_outcome").GetString()} — {policyResult.GetProperty("rationale").GetString()}"),
    new("Decision", decision.GetProperty("final_decision").GetString()!, decision.GetProperty("decision_reasoning").GetString()!)
};

VisualizationWriters.WriteChainOfThoughtVisualization("./", phases, decision);
