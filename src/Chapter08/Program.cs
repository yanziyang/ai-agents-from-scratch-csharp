using AiAgents.Core.Client;
using AiAgents.Core.Memory;
using AiAgents.Core.Tools;
using OpenAI.Chat;

Console.WriteLine("=== Simple Agent with Memory ===\n");

var config = ConfigurationFactory.Create();
var chatClient = DeepSeekClientFactory.CreateChatClient(config);

var memoryManager = new MemoryManager("./agent-memory.json");
var memorySummary = memoryManager.GetMemorySummary();

var systemPrompt = """
    You are a helpful assistant with long-term memory.

    Before calling any function, always follow this reasoning process:

    1. Compare new user statements against existing memories below.
    2. If the same key and value already exist, do NOT call saveMemory again.
       Instead, simply acknowledge the known information.
    3. If the user provides an updated value, call saveMemory once to update the value.
    4. Only call saveMemory for genuinely new information.

    When saving new data, call saveMemory with structured fields:
    - type: "fact" or "preference"
    - key: short descriptive identifier (e.g., "user_name", "favorite_food")
    - value: the specific information

    Examples:
    saveMemory({ type: "fact", key: "user_name", value: "Alex" })
    saveMemory({ type: "preference", key: "favorite_food", value: "pizza" })

    """ + memorySummary;

var messages = new List<ChatMessage>
{
    ChatMessage.CreateSystemMessage(systemPrompt)
};

var toolbox = new ToolBox();
toolbox.Add(new AgentTool(
    name: "saveMemory",
    description: "Save important information to long-term memory (user preferences, facts, personal details)",
    parametersSchema: new
    {
        type = "object",
        properties = new
        {
            type = new { type = "string", @enum = new[] { "fact", "preference" } },
            key = new { type = "string" },
            value = new { type = "string" }
        },
        required = new[] { "type", "key", "value" }
    },
    handler: async args =>
    {
        memoryManager.AddMemory(
            args.GetProperty("type").GetString()!,
            args.GetProperty("key").GetString()!,
            args.GetProperty("value").GetString()!);
        return "Memory saved.";
    }
));

var options = toolbox.CreateOptions();

async Task<string> AskAsync(string prompt)
{
    messages.Add(ChatMessage.CreateUserMessage(prompt));

    var response = await chatClient.CompleteChatAsync(messages, options);
    while (await toolbox.HandleToolCallsAsync(response.Value, messages))
    {
        response = await chatClient.CompleteChatAsync(messages, options);
    }

    var text = response.Value.Content[0].Text;
    messages.Add(ChatMessage.CreateAssistantMessage(text));
    return text;
}

Console.WriteLine("User: Hi! My name is Alex and I love pizza.");
Console.WriteLine("AI: " + await AskAsync("Hi! My name is Alex and I love pizza."));

Console.WriteLine("\nUser: What's my favorite food?");
Console.WriteLine("AI: " + await AskAsync("What's my favorite food?"));
