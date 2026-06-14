using AiAgents.Core.Client;
using OpenAI.Chat;

Console.WriteLine("=== DeepSeek API Intro ===\n");

var config = ConfigurationFactory.Create();
var chatClient = DeepSeekClientFactory.CreateChatClient(config);

await BasicCompletion(chatClient);
await SystemPromptExample(chatClient);
await TemperatureExample(chatClient);
await ConversationContext(chatClient);
await StreamingExample(chatClient);
await TokenUsageExample(chatClient);
await ModelComparison(chatClient);

Console.WriteLine("\n=== All examples completed! ===");

static async Task BasicCompletion(ChatClient client)
{
    Console.WriteLine("--- Example 1: Basic Chat Completion ---");

    var messages = new List<ChatMessage>
    {
        ChatMessage.CreateUserMessage("What is node-llama-cpp?")
    };

    var response = await client.CompleteChatAsync(messages);
    Console.WriteLine("AI: " + response.Value.Content[0].Text);
    Console.WriteLine();
}

static async Task SystemPromptExample(ChatClient client)
{
    Console.WriteLine("--- Example 2: System Prompts ---");

    var messages = new List<ChatMessage>
    {
        ChatMessage.CreateSystemMessage("You are a coding assistant that talks like a pirate."),
        ChatMessage.CreateUserMessage("Explain what async/await does in C#.")
    };

    var response = await client.CompleteChatAsync(messages);
    Console.WriteLine("AI: " + response.Value.Content[0].Text);
    Console.WriteLine();
}

static async Task TemperatureExample(ChatClient client)
{
    Console.WriteLine("--- Example 3: Temperature Control ---");

    const string prompt = "Write a one-sentence tagline for a coffee shop.";

    var focused = await client.CompleteChatAsync(
        new List<ChatMessage> { ChatMessage.CreateUserMessage(prompt) },
        new ChatCompletionOptions { Temperature = 0.2f });

    var creative = await client.CompleteChatAsync(
        new List<ChatMessage> { ChatMessage.CreateUserMessage(prompt) },
        new ChatCompletionOptions { Temperature = 1.5f });

    Console.WriteLine("Low temp (0.2): " + focused.Value.Content[0].Text);
    Console.WriteLine("High temp (1.5): " + creative.Value.Content[0].Text);
    Console.WriteLine();
}

static async Task ConversationContext(ChatClient client)
{
    Console.WriteLine("--- Example 4: Multi-turn Conversation ---");

    var messages = new List<ChatMessage>
    {
        ChatMessage.CreateSystemMessage("You are a helpful coding tutor."),
        ChatMessage.CreateUserMessage("What is a Task in C#?")
    };

    var response1 = await client.CompleteChatAsync(messages, new ChatCompletionOptions { MaxOutputTokenCount = 150 });
    Console.WriteLine("User: What is a Task in C#?");
    Console.WriteLine("AI: " + response1.Value.Content[0].Text);

    messages.Add(ChatMessage.CreateAssistantMessage(response1.Value.Content[0].Text));
    messages.Add(ChatMessage.CreateUserMessage("Can you show me a simple example?"));

    var response2 = await client.CompleteChatAsync(messages);
    Console.WriteLine("\nUser: Can you show me a simple example?");
    Console.WriteLine("AI: " + response2.Value.Content[0].Text);
    Console.WriteLine();
}

static async Task StreamingExample(ChatClient client)
{
    Console.WriteLine("--- Example 5: Streaming Response ---");
    Console.Write("AI: ");

    var messages = new List<ChatMessage>
    {
        ChatMessage.CreateUserMessage("Write a haiku about programming.")
    };

    await foreach (var update in client.CompleteChatStreamingAsync(messages))
    {
        foreach (var part in update.ContentUpdate)
        {
            Console.Write(part.Text);
        }
    }

    Console.WriteLine("\n");
}

static async Task TokenUsageExample(ChatClient client)
{
    Console.WriteLine("--- Example 6: Token Usage ---");

    var messages = new List<ChatMessage>
    {
        ChatMessage.CreateUserMessage("Explain recursion in 3 sentences.")
    };

    var response = await client.CompleteChatAsync(messages, new ChatCompletionOptions { MaxOutputTokenCount = 100 });

    Console.WriteLine("AI: " + response.Value.Content[0].Text);
    Console.WriteLine("\nToken usage:");
    Console.WriteLine("- Prompt tokens: " + response.Value.Usage.InputTokenCount);
    Console.WriteLine("- Completion tokens: " + response.Value.Usage.OutputTokenCount);
    Console.WriteLine("- Total tokens: " + response.Value.Usage.TotalTokenCount);
    Console.WriteLine();
}

static async Task ModelComparison(ChatClient client)
{
    Console.WriteLine("--- Example 7: Model Comparison ---");

    const string prompt = "What's 25 * 47?";

    var flashClient = client;
    var proClient = DeepSeekClientFactory.CreateChatClient(ConfigurationFactory.Create(), "deepseek-v4-pro");

    var flashResponse = await flashClient.CompleteChatAsync(new List<ChatMessage> { ChatMessage.CreateUserMessage(prompt) });
    var proResponse = await proClient.CompleteChatAsync(new List<ChatMessage> { ChatMessage.CreateUserMessage(prompt) });

    Console.WriteLine("deepseek-v4-flash: " + flashResponse.Value.Content[0].Text);
    Console.WriteLine("deepseek-v4-pro:   " + proResponse.Value.Content[0].Text);
    Console.WriteLine();
}
