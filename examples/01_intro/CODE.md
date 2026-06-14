# Code Explanation: Chapter 01 — Basic LLM Interaction

This example shows the simplest possible interaction with a Large Language Model using .NET 10, the official `OpenAI` SDK, and the DeepSeek API.

> **Source code:** `src/Chapter01/Program.cs`
> **Run:** `dotnet run --project src/Chapter01`

## Step-by-Step Code Breakdown

### 1. Import namespaces

```csharp
using AiAgents.Core.Client;
using OpenAI.Chat;
```

- `AiAgents.Core.Client` contains our shared helpers for configuration and creating the DeepSeek client.
- `OpenAI.Chat` provides the chat-completion types (`ChatMessage`, `ChatClient`, etc.).

### 2. Load configuration

```csharp
var config = ConfigurationFactory.Create();
var chatClient = DeepSeekClientFactory.CreateChatClient(config);
```

- `ConfigurationFactory.Create()` reads `appsettings.json`, the git-ignored `appsettings.Secrets.json`, and environment variables.
- `DeepSeekClientFactory.CreateChatClient(...)` builds an `OpenAIClient` pointing at `https://api.deepseek.com` and returns a `ChatClient` configured for `deepseek-v4-flash`.

### 3. Build the message list

```csharp
var messages = new List<ChatMessage>
{
    ChatMessage.CreateUserMessage("Do you know node-llama-cpp?")
};
```

- DeepSeek uses the same chat-message format as OpenAI.
- `ChatMessage.CreateUserMessage(...)` creates a message with role `user`.

### 4. Send the prompt and print the response

```csharp
var response = await chatClient.CompleteChatAsync(messages);
Console.WriteLine("AI: " + response.Value.Content[0].Text);
```

- `CompleteChatAsync` is asynchronous; it sends the messages to DeepSeek and waits for the full response.
- `response.Value` is a `ChatCompletion`. Its `Content` collection contains the generated text parts.

## Configuration

### `appsettings.json` (committed)

```json
{
  "DeepSeek": {
    "BaseUrl": "https://api.deepseek.com",
    "ChatModel": "deepseek-v4-flash"
  }
}
```

### `appsettings.Secrets.json` (NOT committed — add to `.gitignore`)

```json
{
  "DeepSeek": {
    "ApiKey": "your-deepseek-api-key-here"
  }
}
```

A template is provided as `appsettings.Secrets.example.json`. Copy it to `appsettings.Secrets.json` and insert your real key.

## Key Concepts Demonstrated

1. **Basic LLM API call**: Send a message list to a hosted model and receive a response.
2. **Configuration outside code**: The API key lives in a secret config file, never in source control.
3. **Async/await**: All network calls in .NET are asynchronous.

## Expected Output

```
=== Introduction: Basic LLM Interaction ===

AI: Yes, I'm familiar with node-llama-cpp. It is a Node.js binding for llama.cpp...
```

The exact wording depends on the model, temperature, and system prompt (none here).
