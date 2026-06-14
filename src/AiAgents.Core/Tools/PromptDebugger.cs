using System.Text;
using System.Text.Json;

namespace AiAgents.Core.Tools;

public class PromptDebugger
{
    private readonly string _outputDir;
    private readonly string _filename;

    public PromptDebugger(string outputDir = "./logs", string filename = "prompt_debug")
    {
        _outputDir = outputDir;
        _filename = filename;
    }

    public void Log(IEnumerable<OpenAI.Chat.ChatMessage> messages, IEnumerable<AgentTool>? tools = null)
    {
        Directory.CreateDirectory(_outputDir);

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss");
        var path = System.IO.Path.Combine(_outputDir, $"{_filename}_{timestamp}.txt");

        var sb = new StringBuilder();
        sb.AppendLine("========== PROMPT DEBUG OUTPUT ==========");
        sb.AppendLine($"Timestamp: {DateTime.UtcNow:O}");
        sb.AppendLine();

        if (tools != null)
        {
            sb.AppendLine("Available tools:");
            foreach (var tool in tools)
            {
                sb.AppendLine($"  - {tool.Name}: {tool.Description}");
                sb.AppendLine($"    Schema: {JsonSerializer.Serialize(tool.ParametersSchema)}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("Messages:");
        foreach (var message in messages)
        {
            var role = message switch
            {
                OpenAI.Chat.SystemChatMessage => "system",
                OpenAI.Chat.UserChatMessage => "user",
                OpenAI.Chat.AssistantChatMessage => "assistant",
                OpenAI.Chat.ToolChatMessage => "tool",
                _ => message.GetType().Name
            };

            sb.AppendLine($"  Role: {role}");
            sb.AppendLine($"  Content: {message.Content}");
            sb.AppendLine();
        }

        sb.AppendLine("=========================================");

        File.WriteAllText(path, sb.ToString());
        Console.WriteLine($"Prompt debug output written to {path}");
    }
}
