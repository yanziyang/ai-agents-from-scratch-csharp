using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace AiAgents.Core.Client;

public static class DeepSeekClientFactory
{
    public static ChatClient CreateChatClient(IConfiguration configuration, string? model = null)
    {
        var options = configuration.GetSection("DeepSeek").Get<DeepSeekOptions>() ?? new DeepSeekOptions();

        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("DeepSeek API key is missing. Set it in appsettings.Secrets.json or the DEEPSEEK__APIKEY environment variable.");

        var credential = new ApiKeyCredential(options.ApiKey);
        var openAiClient = new OpenAIClient(credential, new OpenAIClientOptions
        {
            Endpoint = new Uri(options.BaseUrl)
        });

        return openAiClient.GetChatClient(model ?? options.ChatModel);
    }
}
