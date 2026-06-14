using AiAgents.Core.Client;
using AiAgents.Core.Parsing;
using AiAgents.Core.Tools;
using OpenAI.Chat;
using System.Text.Json;

Console.WriteLine("=== Atom of Thought (AoT) Agent ===\n");

var config = ConfigurationFactory.Create();
var chatClient = DeepSeekClientFactory.CreateChatClient(config);

const string systemPrompt = """
    You are a mathematical planning assistant using Atom of Thought methodology.

    CRITICAL RULES:
    1. Extract every number from the user's question and put it in the "input" field.
    2. Each atom expresses EXACTLY ONE operation: add, subtract, multiply, divide.
    3. NEVER combine operations in one atom.
    4. The "final" atom reports only the result of the last computational atom; it must NOT have an "input" field.
    5. Use "<result_of_N>" to reference previous atom results.
    6. Output ONLY valid JSON matching the schema, with no explanation or extra text.

    CORRECT EXAMPLE for "What is (15 + 7) * 3 - 10?":
    {
      "atoms": [
        {"id": 1, "kind": "tool", "name": "add", "input": {"a": 15, "b": 7}, "dependsOn": []},
        {"id": 2, "kind": "tool", "name": "multiply", "input": {"a": "<result_of_1>", "b": 3}, "dependsOn": [1]},
        {"id": 3, "kind": "tool", "name": "subtract", "input": {"a": "<result_of_2>", "b": 10}, "dependsOn": [2]},
        {"id": 4, "kind": "final", "name": "report", "dependsOn": [3]}
      ]
    }

    Available tools: add, subtract, multiply, divide.
    Each tool requires: {"a": <number or reference>, "b": <number or reference>}.
    """;

var messages = new List<ChatMessage>
{
    ChatMessage.CreateSystemMessage(systemPrompt)
};

var tools = new Dictionary<string, Func<double, double, double>>(StringComparer.Ordinal)
{
    ["add"] = (a, b) => a + b,
    ["subtract"] = (a, b) => a - b,
    ["multiply"] = (a, b) => a * b,
    ["divide"] = (a, b) => a / b
};

var decisions = new Dictionary<string, Func<List<double>, double>>(StringComparer.Ordinal)
{
    ["average"] = values => values.Average(),
    ["chooseCheapest"] = values => values.Min()
};

async Task<JsonElement> GeneratePlanAsync(string userPrompt)
{
    Console.WriteLine(new string('=', 70));
    Console.WriteLine("PHASE 1: PLANNING");
    Console.WriteLine(new string('=', 70));
    Console.WriteLine("USER QUESTION: " + userPrompt + "\n");

    messages.Add(ChatMessage.CreateUserMessage(userPrompt + "\n\nRemember: Extract the actual numbers and put them in the input fields."));

    var options = new ChatCompletionOptions
    {
        ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
        MaxOutputTokenCount = 1000
    };

    var response = await chatClient.CompleteChatAsync(messages, options);
    var planText = response.Value.Content[0].Text;

    var plan = JsonParser.Parse(planText);
    JsonParser.ValidatePlan(plan, tools.Keys.ToArray(), decisions.Keys.ToArray());

    Console.WriteLine("GENERATED PLAN:");
    Console.WriteLine(JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine();

    return plan;
}

void ValidatePlan(JsonElement plan)
{
    Console.WriteLine(new string('=', 70));
    Console.WriteLine("PHASE 2: VALIDATION");
    Console.WriteLine(new string('=', 70) + "\n");

    JsonParser.ValidatePlan(plan, tools.Keys.ToArray(), decisions.Keys.ToArray());

    foreach (var atom in plan.GetProperty("atoms").EnumerateArray())
    {
        var id = atom.GetProperty("id").GetInt32();
        var kind = atom.GetProperty("kind").GetString();
        var name = atom.GetProperty("name").GetString();
        Console.WriteLine($"Atom {id} ({kind}:{name}) validated");
    }

    Console.WriteLine("\nPlan validation successful\n");
}

Dictionary<int, double> ExecutePlan(JsonElement plan)
{
    Console.WriteLine(new string('=', 70));
    Console.WriteLine("PHASE 3: EXECUTION");
    Console.WriteLine(new string('=', 70) + "\n");

    var state = new Dictionary<int, double>();
    var atoms = plan.GetProperty("atoms").EnumerateArray().OrderBy(a => a.GetProperty("id").GetInt32()).ToList();

    foreach (var atom in atoms)
    {
        var id = atom.GetProperty("id").GetInt32();
        var kind = atom.GetProperty("kind").GetString()!;
        var name = atom.GetProperty("name").GetString()!;
        var dependsOn = atom.TryGetProperty("dependsOn", out var deps)
            ? deps.EnumerateArray().Select(x => x.GetInt32()).ToList()
            : new List<int>();

        Console.WriteLine($"Executing atom {id} ({kind}:{name})");

        var resolved = new Dictionary<string, double>();
        if (atom.TryGetProperty("input", out var input))
        {
            foreach (var property in input.EnumerateObject())
            {
                var value = property.Value;
                if (value.ValueKind == JsonValueKind.Number)
                {
                    resolved[property.Name] = value.GetDouble();
                }
                else if (value.ValueKind == JsonValueKind.String)
                {
                    var refId = int.Parse(value.GetString()!.Replace("<result_of_", "").Replace(">", ""));
                    resolved[property.Name] = state[refId];
                    Console.WriteLine($"Resolved {property.Name}: {value.GetString()} -> {state[refId]}");
                }
            }
        }

        if (kind == "tool")
        {
            var a = resolved["a"];
            var b = resolved["b"];
            var result = tools[name](a, b);
            Console.WriteLine($"Input: a={a}, b={b}");
            Console.WriteLine($"EXECUTING: {name}({a}, {b}) = {result}\n");
            state[id] = result;
        }
        else if (kind == "decision")
        {
            var depResults = dependsOn.Select(d => state[d]).ToList();
            var result = decisions[name](depResults);
            Console.WriteLine($"DECISION: {name}([{string.Join(", ", depResults)}]) = {result}\n");
            state[id] = result;
        }
        else if (kind == "final")
        {
            var finalValue = state[dependsOn[0]];
            Console.WriteLine($"FINAL RESULT: {finalValue}\n");
            state[id] = finalValue;
        }
    }

    return state;
}

async Task AotAgentAsync(string userPrompt)
{
    try
    {
        var plan = await GeneratePlanAsync(userPrompt);
        ValidatePlan(plan);
        var state = ExecutePlan(plan);

        Console.WriteLine(new string('=', 70));
        Console.WriteLine("EXECUTION COMPLETE");
        Console.WriteLine(new string('=', 70));

        var finalAtom = plan.GetProperty("atoms").EnumerateArray().FirstOrDefault(a => a.GetProperty("kind").GetString() == "final");
        if (finalAtom.ValueKind != JsonValueKind.Undefined)
        {
            var finalId = finalAtom.GetProperty("id").GetInt32();
            Console.WriteLine($"\nANSWER: {state[finalId]}\n");
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("\nEXECUTION FAILED: " + ex.Message);
    }
}

await AotAgentAsync("Calculate: 100 divided by 5, then add 3, then multiply by 2");

var debugger = new PromptDebugger(outputDir: "./logs", filename: "aot_calculator");
debugger.Log(messages, Array.Empty<AgentTool>());
