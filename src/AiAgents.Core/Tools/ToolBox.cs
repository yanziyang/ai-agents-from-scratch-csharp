using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

namespace AiAgents.Core.Tools;

public class ToolBox
{
    private readonly Dictionary<string, AgentTool> _tools = new(StringComparer.Ordinal);

    public void Add(AgentTool tool) => _tools[tool.Name] = tool;

    public bool HasTool(string name) => _tools.ContainsKey(name);

    public IEnumerable<AgentTool> Tools => _tools.Values;

    public ChatCompletionOptions CreateOptions()
    {
        var options = new ChatCompletionOptions
        {
            ToolChoice = ChatToolChoice.CreateAutoChoice()
        };

        foreach (var tool in _tools.Values)
        {
            var schemaData = BinaryData.FromObjectAsJson(tool.ParametersSchema);
            options.Tools.Add(ChatTool.CreateFunctionTool(tool.Name, tool.Description, schemaData));
        }

        return options;
    }

    public async Task<bool> HandleToolCallsAsync(ChatCompletion response, List<ChatMessage> messages)
    {
        if (response.ToolCalls.Count == 0)
            return false;

        // Add the assistant message that contains the tool calls to the conversation history.
        messages.Add(ChatMessage.CreateAssistantMessage(response));

        foreach (var call in response.ToolCalls)
        {
            if (!_tools.TryGetValue(call.FunctionName, out var tool))
                throw new InvalidOperationException($"Unknown tool: {call.FunctionName}");

            using var argsDocument = JsonDocument.Parse(call.FunctionArguments.ToString());
            var result = await tool.Handler(argsDocument.RootElement);

            messages.Add(ChatMessage.CreateToolMessage(call.Id, result?.ToString() ?? string.Empty));
        }

        return true;
    }
}
