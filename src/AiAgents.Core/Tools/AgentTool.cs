using System.Text.Json;

namespace AiAgents.Core.Tools;

public delegate Task<object> ToolHandlerAsync(JsonElement arguments);

public class AgentTool
{
    public string Name { get; }
    public string Description { get; }
    public object ParametersSchema { get; }
    public ToolHandlerAsync Handler { get; }

    public AgentTool(string name, string description, object parametersSchema, ToolHandlerAsync handler)
    {
        Name = name;
        Description = description;
        ParametersSchema = parametersSchema;
        Handler = handler;
    }
}
