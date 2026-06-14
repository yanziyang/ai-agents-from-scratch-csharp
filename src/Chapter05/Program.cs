using AiAgents.Core.Client;
using OpenAI.Chat;

Console.WriteLine("=== Batch: Parallel Processing ===\n");

var config = ConfigurationFactory.Create();
var chatClient = DeepSeekClientFactory.CreateChatClient(config);

const string q1 = "Hi there, how are you?";
const string q2 = "How much is 6 + 6?";

Console.WriteLine("Batching started...\n");

var task1 = chatClient.CompleteChatAsync(new List<ChatMessage> { ChatMessage.CreateUserMessage(q1) });
var task2 = chatClient.CompleteChatAsync(new List<ChatMessage> { ChatMessage.CreateUserMessage(q2) });

var results = await Task.WhenAll(task1, task2);

Console.WriteLine("User: " + q1);
Console.WriteLine("AI: " + results[0].Value.Content[0].Text);

Console.WriteLine("\nUser: " + q2);
Console.WriteLine("AI: " + results[1].Value.Content[0].Text);
