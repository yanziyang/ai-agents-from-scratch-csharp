using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace AiAgents.Core.Client;

public static class DeepSeekClientFactory
{
    public static ChatClient CreateChatClient(IConfiguration configuration, string? model = null)
    {
        var options = configuration.GetSection("DeepSeek").Get<DeepSeekOptions>() ?? new DeepSeekOptions();

        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("DeepSeek API key is missing. Set it in appsettings.Secrets.json or the DEEPSEEK__APIKEY environment variable.");

        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(options.BaseUrl)
        };

        var loggingOptions = configuration.GetSection("Logging").Get<LoggingOptions>();
        if (loggingOptions?.Enabled == true)
        {
            clientOptions.AddPolicy(new DeepSeekLoggingPolicy(loggingOptions), PipelinePosition.PerCall);
        }

        var credential = new ApiKeyCredential(options.ApiKey);
        var openAiClient = new OpenAIClient(credential, clientOptions);

        return openAiClient.GetChatClient(model ?? options.ChatModel);
    }
}
