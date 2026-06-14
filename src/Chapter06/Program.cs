using AiAgents.Core.Client;
using OpenAI.Chat;

Console.WriteLine("=== Coding: Streaming & Response Control ===\n");

var config = ConfigurationFactory.Create();
var chatClient = DeepSeekClientFactory.CreateChatClient(config);

const string question = "What is the difference between async and await in C#? Explain with examples.";

Console.WriteLine("Question: " + question);
Console.Write("AI: ");

var messages = new List<ChatMessage>
{
    ChatMessage.CreateUserMessage(question)
};

var options = new ChatCompletionOptions
{
    MaxOutputTokenCount = 2000
};

var fullResponse = new System.Text.StringBuilder();

await foreach (var update in chatClient.CompleteChatStreamingAsync(messages, options))
{
    foreach (var part in update.ContentUpdate)
    {
        Console.Write(part.Text);
        fullResponse.Append(part.Text);
    }
}

Console.WriteLine("\n\nFinal answer:\n" + fullResponse);
