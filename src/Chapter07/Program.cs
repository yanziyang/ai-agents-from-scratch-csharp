using AiAgents.Core.Client;
using AiAgents.Core.Tools;
using OpenAI.Chat;

Console.WriteLine("=== Simple Agent: Function Calling ===\n");

var config = ConfigurationFactory.Create();
var chatClient = DeepSeekClientFactory.CreateChatClient(config);

const string systemPrompt = """
    You are a professional chronologist who standardizes time representations across different systems.

    Always convert times from 12-hour format (e.g., "1:46:36 PM") to 24-hour format (e.g., "13:46") without seconds
    before returning them.
    """;

var messages = new List<ChatMessage>
{
    ChatMessage.CreateSystemMessage(systemPrompt),
    ChatMessage.CreateUserMessage("What time is it right now?")
};

var toolbox = new ToolBox();
toolbox.Add(new AgentTool(
    name: "getCurrentTime",
    description: "Get the current time",
    parametersSchema: new { type = "object", properties = new { }, required = Array.Empty<string>() },
    handler: async _ => DateTime.Now.ToString("h:mm:ss tt")
));

var options = toolbox.CreateOptions();

var response = await chatClient.CompleteChatAsync(messages, options);
while (await toolbox.HandleToolCallsAsync(response.Value, messages))
{
    response = await chatClient.CompleteChatAsync(messages, options);
}

Console.WriteLine("AI: " + response.Value.Content[0].Text);

var debugger = new PromptDebugger(outputDir: "./logs", filename: "qwen_prompts");
debugger.Log(messages, toolbox.Tools);
