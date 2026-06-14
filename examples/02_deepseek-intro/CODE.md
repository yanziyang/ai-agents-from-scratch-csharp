# Code Explanation: Chapter 02 — DeepSeek API Intro

This example shows how to call a hosted LLM (DeepSeek) from .NET 10 using the official `OpenAI` SDK. DeepSeek exposes an OpenAI-compatible endpoint, so the same code works for both providers by changing the base URL and model name.

> **Source code:** `src/Chapter02/Program.cs`
> **Run:** `dotnet run --project src/Chapter02`

## Requirements

- A DeepSeek account and API key from [https://platform.deepseek.com/api_keys](https://platform.deepseek.com/api_keys).
- Copy `appsettings.Secrets.example.json` to `appsettings.Secrets.json` and set your key.

## Setup

```csharp
var config = ConfigurationFactory.Create();
var chatClient = DeepSeekClientFactory.CreateChatClient(config);
```

- `ConfigurationFactory.Create()` loads `appsettings.json`, the git-ignored `appsettings.Secrets.json`, and environment variables.
- `DeepSeekClientFactory.CreateChatClient(...)` creates an `OpenAIClient` aimed at `https://api.deepseek.com` and selects the model configured in `DeepSeek:ChatModel` (`deepseek-v4-flash` by default).

## Example 1: Basic Chat Completion

```csharp
var messages = new List<ChatMessage>
{
    ChatMessage.CreateUserMessage("What is node-llama-cpp?")
};

var response = await client.CompleteChatAsync(messages);
Console.WriteLine("AI: " + response.Value.Content[0].Text);
```

- `CompleteChatAsync` sends the messages and waits for the full response.
- `response.Value` is a `ChatCompletion`; `Content[0].Text` is the generated answer.

## Example 2: System Prompts

```csharp
var messages = new List<ChatMessage>
{
    ChatMessage.CreateSystemMessage("You are a coding assistant that talks like a pirate."),
    ChatMessage.CreateUserMessage("Explain what async/await does in C#.")
};
```

- System messages set behavior and personality.
- They are placed at the start of the message list and influence every following turn.

## Example 3: Temperature Control

```csharp
var focused = await client.CompleteChatAsync(
    messages,
    new ChatCompletionOptions { Temperature = 0.2f });

var creative = await client.CompleteChatAsync(
    messages,
    new ChatCompletionOptions { Temperature = 1.5f });
```

- `Temperature` controls randomness (0.0 = deterministic, 2.0 = very creative).
- Lower values are good for facts and code; higher values for brainstorming.

## Example 4: Conversation Context

```csharp
messages.Add(ChatMessage.CreateAssistantMessage(response1.Value.Content[0].Text));
messages.Add(ChatMessage.CreateUserMessage("Can you show me a simple example?"));

var response2 = await client.CompleteChatAsync(messages);
```

- DeepSeek's API is **stateless**: it does not remember prior calls.
- You must append assistant responses and new user messages to the list and resend the full history.

## Example 5: Streaming Responses

```csharp
await foreach (var update in client.CompleteChatStreamingAsync(messages))
{
    foreach (var part in update.ContentUpdate)
    {
        Console.Write(part.Text);
    }
}
```

- `CompleteChatStreamingAsync` returns an `IAsyncEnumerable<StreamingChatCompletionUpdate>`.
- Each update carries a small text fragment, letting you display output as it arrives.

## Example 6: Token Usage

```csharp
var response = await client.CompleteChatAsync(messages, new ChatCompletionOptions
{
    MaxOutputTokenCount = 100
});

Console.WriteLine($"Prompt tokens: {response.Value.Usage.InputTokenCount}");
Console.WriteLine($"Completion tokens: {response.Value.Usage.OutputTokenCount}");
Console.WriteLine($"Total tokens: {response.Value.Usage.TotalTokenCount}");
```

- `MaxOutputTokenCount` caps the response length.
- `Usage` reports how many tokens were consumed.

## Example 7: Model Comparison

```csharp
var flashClient = client;
var proClient = DeepSeekClientFactory.CreateChatClient(config, "deepseek-v4-pro");

var flashResponse = await flashClient.CompleteChatAsync(messages);
var proResponse = await proClient.CompleteChatAsync(messages);
```

- You can create multiple `ChatClient` instances with different model names.
- This example compares `deepseek-v4-flash` (fast, cheap) with `deepseek-v4-pro` (more capable).

## Error Handling

```csharp
try
{
    await BasicCompletion(client);
}
catch (Exception ex)
{
    Console.Error.WriteLine("Error: " + ex.Message);
}
```

- Always wrap network calls in `try/catch`.
- Common issues: missing API key, rate limits (429), context length exceeded, and service errors.

## Key Takeaways

1. **Stateless API** — send full context each time.
2. **Message roles** — `system`, `user`, `assistant`.
3. **Temperature** — tune randomness for the task.
4. **Streaming** — better UX for interactive apps.
5. **Token management** — monitor cost and limits.
6. **Model selection** — match the model to the task.
