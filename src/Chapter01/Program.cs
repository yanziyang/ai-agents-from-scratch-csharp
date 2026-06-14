using AiAgents.Core.Client;
using OpenAI.Chat;

Console.WriteLine("=== Introduction: Basic LLM Interaction ===\n");

var config = ConfigurationFactory.Create();
var chatClient = DeepSeekClientFactory.CreateChatClient(config);

var messages = new List<ChatMessage>
{
    ChatMessage.CreateUserMessage("Do you know node-llama-cpp?")
};

var response = await chatClient.CompleteChatAsync(messages);

Console.WriteLine("AI: " + response.Value.Content[0].Text);
