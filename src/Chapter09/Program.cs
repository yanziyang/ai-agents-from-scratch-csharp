using AiAgents.Core.Client;
using AiAgents.Core.Tools;
using OpenAI.Chat;

Console.WriteLine("=== ReAct Agent ===\n");

var config = ConfigurationFactory.Create();
var chatClient = DeepSeekClientFactory.CreateChatClient(config);

const string systemPrompt = """
    You are a mathematical assistant that uses the ReAct (Reasoning + Acting) approach.

    CRITICAL: Follow this EXACT pattern:

    Thought: [Explain what calculation you need to do next and why]
    Action: [Call ONE tool with specific numbers]
    Observation: [Wait for the tool result]
    ... (repeat as many times as needed)
    Thought: [Once you have ALL the information needed]
    Answer: [Give the final answer and STOP]

    RULES:
    1. Only write "Answer:" when you have the complete final answer.
    2. After writing "Answer:", do not continue calculating or thinking.
    3. Break complex problems into the smallest possible steps.
    4. Use tools for ALL calculations - never calculate in your head.
    5. Each Action should call exactly ONE tool.
    """;

var messages = new List<ChatMessage>
{
    ChatMessage.CreateSystemMessage(systemPrompt)
};

var toolbox = new ToolBox();
toolbox.Add(new AgentTool("add", "Add two numbers together",
    new { type = "object", properties = new { a = new { type = "number" }, b = new { type = "number" } }, required = new[] { "a", "b" } },
    async args =>
    {
        var a = args.GetProperty("a").GetDouble();
        var b = args.GetProperty("b").GetDouble();
        var result = a + b;
        Console.WriteLine($"\n   TOOL CALLED: add({a}, {b})");
        Console.WriteLine($"   RESULT: {result}\n");
        return result.ToString();
    }));

toolbox.Add(new AgentTool("multiply", "Multiply two numbers together",
    new { type = "object", properties = new { a = new { type = "number" }, b = new { type = "number" } }, required = new[] { "a", "b" } },
    async args =>
    {
        var a = args.GetProperty("a").GetDouble();
        var b = args.GetProperty("b").GetDouble();
        var result = a * b;
        Console.WriteLine($"\n   TOOL CALLED: multiply({a}, {b})");
        Console.WriteLine($"   RESULT: {result}\n");
        return result.ToString();
    }));

toolbox.Add(new AgentTool("subtract", "Subtract second number from first number",
    new { type = "object", properties = new { a = new { type = "number" }, b = new { type = "number" } }, required = new[] { "a", "b" } },
    async args =>
    {
        var a = args.GetProperty("a").GetDouble();
        var b = args.GetProperty("b").GetDouble();
        var result = a - b;
        Console.WriteLine($"\n   TOOL CALLED: subtract({a}, {b})");
        Console.WriteLine($"   RESULT: {result}\n");
        return result.ToString();
    }));

toolbox.Add(new AgentTool("divide", "Divide first number by second number",
    new { type = "object", properties = new { a = new { type = "number" }, b = new { type = "number" } }, required = new[] { "a", "b" } },
    async args =>
    {
        var a = args.GetProperty("a").GetDouble();
        var b = args.GetProperty("b").GetDouble();
        if (b == 0)
        {
            Console.WriteLine($"\n   TOOL CALLED: divide({a}, {b})");
            Console.WriteLine("   ERROR: Cannot divide by zero\n");
            return "Error: Cannot divide by zero";
        }
        var result = a / b;
        Console.WriteLine($"\n   TOOL CALLED: divide({a}, {b})");
        Console.WriteLine($"   RESULT: {result}\n");
        return result.ToString();
    }));

var options = toolbox.CreateOptions();

async Task ReactAsync(string userPrompt, int maxIterations = 5)
{
    Console.WriteLine(new string('=', 70));
    Console.WriteLine("USER QUESTION: " + userPrompt);
    Console.WriteLine(new string('=', 70) + "\n");

    messages.Add(ChatMessage.CreateUserMessage(userPrompt));

    for (var iteration = 1; iteration <= maxIterations; iteration++)
    {
        Console.WriteLine($"--- Iteration {iteration} ---");

        var response = await chatClient.CompleteChatAsync(messages, options);
        var text = response.Value.Content[0].Text;
        Console.WriteLine(text);
        messages.Add(ChatMessage.CreateAssistantMessage(text));

        if (text.Contains("Answer:", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("\n" + new string('=', 70));
            Console.WriteLine("FINAL ANSWER REACHED");
            Console.WriteLine(new string('=', 70));
            return;
        }

        var hadToolCalls = await toolbox.HandleToolCallsAsync(response.Value, messages);
        if (!hadToolCalls)
        {
            Console.WriteLine("\nNo tool calls were made. Stopping.");
            return;
        }
    }

    Console.WriteLine("\nMax iterations reached without final answer.");
}

await ReactAsync("A store sells 15 items on Monday at $8 each, 20 items on Tuesday at $8 each, and 10 items on Wednesday at $8 each. What's the average number of items sold per day, and what's the total revenue?");

var debugger = new PromptDebugger(outputDir: "./logs", filename: "react_calculator");
debugger.Log(messages, toolbox.Tools);
