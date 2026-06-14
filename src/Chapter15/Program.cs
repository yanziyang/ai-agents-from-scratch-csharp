using AiAgents.Core.Client;
using AiAgents.Core.Visualization;
using LLama;
using LLama.Common;
using OpenAI.Chat;
using System.Text.Json;

Console.WriteLine("=== Tool Routing with Embeddings ===\n");

var config = ConfigurationFactory.Create();
var chatClient = DeepSeekClientFactory.CreateChatClient(config);
var modelPath = config.GetSection("Embeddings:ModelPath").Value!;

if (!File.Exists(modelPath))
{
    Console.WriteLine($"Embedding model not found: {Path.GetFullPath(modelPath)}");
    Console.WriteLine("Please download a GGUF embedding model (e.g., bge-small-en-v1.5-q8_0.gguf) and place it in the models/ folder.");
    return;
}

Console.WriteLine($"Loading embedding model: {modelPath}");
var parameters = new ModelParams(modelPath)
{
    Embeddings = true,
    ContextSize = 512
};
using var weights = LLamaWeights.LoadFromFile(parameters);
using var embedder = new LLamaEmbedder(weights, parameters);

Console.WriteLine("Embedding model loaded.\n");

var tools = ToolCatalog.Create();
var exemplars = Exemplars.Load();

Console.WriteLine("Precomputing exemplar embeddings...");
var exemplarRows = new List<ExemplarRow>();
foreach (var e in exemplars)
{
    var embedding = await GetEmbeddingAsync(e.Text);
    exemplarRows.Add(new ExemplarRow(e.ToolKey, e.Text, embedding));
}
Console.WriteLine($"Cached {exemplarRows.Count} exemplar embeddings.\n");

var routingLog = new List<VisualizationWriters.ToolRoutingLogEntry>();

await RunWithRoutingAsync(
    "I cannot connect to the corporate VPN from home; AnyConnect keeps timing out.",
    new RoutingOptions(1, [], "Single intent - VPN failure, k=1"));

await RunWithRoutingAsync(
    "My account is locked after too many failed sign-in attempts.",
    new RoutingOptions(1, [], "Single intent - locked account, k=1"));

await RunWithRoutingAsync(
    "My PC keeps crashing with blue screens and the fans are running loud.",
    new RoutingOptions(2, [], "Dual intent - crashing PC, k=2"));

await RunWithRoutingAsync(
    "I am running out of disk space and I cannot find the software inventory for this machine.",
    new RoutingOptions(2, [], "Dual intent - disk full + software, k=2"));

await RunWithRoutingAsync(
    "I cannot connect to the VPN and my account is locked after failed logins.",
    new RoutingOptions(1, [], "Dual intent - VPN + locked account, k=1: recall failure"));

await RunWithRoutingAsync(
    "I cannot connect to the VPN and my account is locked after failed logins.",
    new RoutingOptions(1, ["getUserAccount"], "Dual intent - same query, k=1 + pinned getUserAccount"));

VisualizationWriters.WriteToolRoutingVisualization("./", routingLog);

async Task RunWithRoutingAsync(string userPrompt, RoutingOptions options)
{
    Console.WriteLine($"Case: {options.Label}");
    Console.WriteLine($"Prompt: \"{userPrompt}\"");

    var queryEmbedding = await GetEmbeddingAsync(userPrompt);
    var scores = ScoreTools(queryEmbedding, exemplarRows);
    var selectedKeys = SelectToolKeys(scores, options.RetrievalK, options.AlwaysInclude);

    Console.WriteLine("  Tool scores (top 6):");
    foreach (var (key, sim) in scores.OrderByDescending(kv => kv.Value).Take(6))
    {
        var tick = selectedKeys.Contains(key) ? "x" : " ";
        var bar = new string('#', (int)Math.Round(sim * 24));
        Console.WriteLine($"  [{tick}] {key,-28} {sim:F4}  {bar}");
    }

    var selectedTools = tools.Where(t => selectedKeys.Contains(t.Key)).ToList();
    var answer = await RunAgentLoopAsync(userPrompt, selectedTools);

    routingLog.Add(new VisualizationWriters.ToolRoutingLogEntry(
        options.Label,
        userPrompt,
        scores.ToDictionary(kv => kv.Key, kv => kv.Value),
        selectedKeys.ToList(),
        answer));

    Console.WriteLine($"  Answer: {answer}\n");
}

async Task<float[]> GetEmbeddingAsync(string text)
{
    var results = await embedder.GetEmbeddings(text);
    return results[0];
}

static Dictionary<string, double> ScoreTools(float[] query, List<ExemplarRow> rows)
{
    var maxByTool = new Dictionary<string, double>();
    foreach (var row in rows)
    {
        var sim = CosineSimilarity(query, row.Embedding);
        if (!maxByTool.TryGetValue(row.ToolKey, out var current) || sim > current)
        {
            maxByTool[row.ToolKey] = sim;
        }
    }
    return maxByTool;
}

static HashSet<string> SelectToolKeys(Dictionary<string, double> scores, int k, List<string> alwaysInclude)
{
    var ranked = scores.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToList();
    var selected = new HashSet<string>(alwaysInclude);
    foreach (var key in ranked)
    {
        if (selected.Count >= alwaysInclude.Count + k) break;
        if (selected.Contains(key)) continue;
        selected.Add(key);
    }
    return selected;
}

static double CosineSimilarity(float[] a, float[] b)
{
    double dot = 0, normA = 0, normB = 0;
    for (int i = 0; i < a.Length; i++)
    {
        dot += a[i] * b[i];
        normA += a[i] * a[i];
        normB += b[i] * b[i];
    }
    return dot / (Math.Sqrt(normA) * Math.Sqrt(normB) + 1e-10);
}

async Task<string> RunAgentLoopAsync(string userPrompt, List<ToolEntry> selectedTools)
{
    var messages = new List<ChatMessage>
    {
        ChatMessage.CreateSystemMessage("""
            You are a concise IT helpdesk assistant.
            Use only the provided tools. Call one tool at a time when needed.
            Reply with a short, helpful sentence after using a tool.
            """)
    };
    messages.Add(ChatMessage.CreateUserMessage(userPrompt));

    var options = new ChatCompletionOptions
    {
        MaxOutputTokenCount = 800,
        Temperature = 0.2f
    };
    foreach (var tool in selectedTools)
    {
        options.Tools.Add(tool.Definition);
    }

    for (int turn = 0; turn < 5; turn++)
    {
        var response = await chatClient.CompleteChatAsync(messages, options);
        var msg = response.Value;

        if (msg.ToolCalls.Count == 0)
        {
            return msg.Content[0].Text;
        }

        messages.Add(ChatMessage.CreateAssistantMessage(msg));

        foreach (var call in msg.ToolCalls)
        {
            var tool = selectedTools.FirstOrDefault(t => t.Key == call.FunctionName);
            var result = tool?.Handler(call.FunctionArguments) ?? $"Unknown tool: {call.FunctionName}";
            messages.Add(ChatMessage.CreateToolMessage(call.Id, result));
        }
    }

    return "The agent reached the maximum number of tool turns.";
}

record RoutingOptions(int RetrievalK, List<string> AlwaysInclude, string Label);
record ExemplarRow(string ToolKey, string Text, float[] Embedding);

class ToolEntry
{
    public required string Key { get; init; }
    public required ChatTool Definition { get; init; }
    public required Func<BinaryData, string> Handler { get; init; }
}

static class ToolCatalog
{
    public static List<ToolEntry> Create() => new()
    {
        Make("checkNetworkConnectivity", "Check general network connectivity, ping gateways, and DNS resolution.",
            new { host = (string?)null },
            args => JsonSerializer.Serialize(new { host = args.host ?? "corporate-gateway", reachable = true, latency_ms = 12 })),

        Make("checkVPNStatus", "Check VPN client status, active profile, and tunnel health for a user.",
            new { username = (string?)null },
            args => JsonSerializer.Serialize(new
            {
                username = args.username ?? "current_user",
                vpn_client = "Cisco AnyConnect 4.10",
                connected = false,
                last_connected = "2026-05-14T09:31:00Z",
                error = "Authentication timeout - check MFA token or try reconnecting",
                profile = "Corp-HQ"
            })),

        Make("getSystemLogs", "Retrieve recent system log entries for diagnostics.",
            new { minutes = (int?)null },
            args => JsonSerializer.Serialize(new { source = "System", entries = new[] { "Event 1001: unexpected reboot", "Event 7026: service start failure" }, minutes = args.minutes ?? 60 })),

        Make("runDiagnostic", "Run a hardware and software diagnostic suite.",
            new { quick = (bool?)null },
            args => JsonSerializer.Serialize(new { quick = args.quick ?? true, cpu_ok = true, memory_ok = false, disk_ok = true, thermal_warning = true })),

        Make("getHardwareInfo", "Get hardware summary for the device.",
            new { },
            args => JsonSerializer.Serialize(new { cpu = "Intel i7-1165G7", ram_gb = 16, disk_gb = 512, fan_status = "high RPM" })),

        Make("checkDiskSpace", "Check free and used disk space on the machine.",
            new { drive = (string?)null },
            args => JsonSerializer.Serialize(new { drive = args.drive ?? "C:", free_gb = 3.2, total_gb = 512, status = "critically low" })),

        Make("getInstalledSoftware", "List installed software and versions.",
            new { },
            args => JsonSerializer.Serialize(new { installed = new[] { new { name = "Acrobat Reader", version = "2023.1" }, new { name = "VPN Client", version = "4.10" } } })),

        Make("getUserAccount", "Look up a user account status, groups, and lock state.",
            new { username = (string?)null },
            args => JsonSerializer.Serialize(new { username = args.username ?? "current_user", locked = true, lock_reason = "too_many_failed_logins", groups = new[] { "Engineering", "VPN-Users" } })),

        Make("resetPassword", "Initiate a password reset for a user.",
            new { username = (string?)null },
            args => JsonSerializer.Serialize(new { username = args.username ?? "current_user", status = "reset_link_sent", expiry_minutes = 30 })),

        Make("restartService", "Restart a named Windows or Linux service.",
            new { service_name = (string?)null },
            args => JsonSerializer.Serialize(new { service_name = args.service_name ?? "unknown", status = "restarted", pid = 12345 })),

        Make("createSupportTicket", "Create a helpdesk support ticket.",
            new { summary = (string?)null, priority = (string?)null },
            args => JsonSerializer.Serialize(new { ticket_id = "TKT-" + Random.Shared.Next(100000, 999999), summary = args.summary ?? "No summary", priority = args.priority ?? "medium" })),

        Make("escalateToSpecialist", "Escalate the issue to a specialist team.",
            new { reason = (string?)null },
            args => JsonSerializer.Serialize(new { escalation_id = "ESC-" + Random.Shared.Next(1000, 9999), team = "L2-Support", reason = args.reason ?? "complex issue" }))
    };

    private static ToolEntry Make<T>(string key, string description, T schema, Func<T, string> handler) where T : notnull
    {
        var jsonSchema = JsonSchemaFor(schema);
        var tool = ChatTool.CreateFunctionTool(key, description, BinaryData.FromString(jsonSchema));

        return new ToolEntry
        {
            Key = key,
            Definition = tool,
            Handler = data =>
            {
                var parsed = JsonSerializer.Deserialize<T>(data.ToString())!;
                return handler(parsed);
            }
        };
    }

    private static string JsonSchemaFor<T>(T schema)
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();
        foreach (var prop in typeof(T).GetProperties())
        {
            var typeName = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            var jsonType = typeName switch
            {
                _ when typeName == typeof(string) => "string",
                _ when typeName == typeof(int) => "integer",
                _ when typeName == typeof(bool) => "boolean",
                _ => "string"
            };
            properties[prop.Name] = new { type = jsonType, description = $"{prop.Name} parameter" };
            if (Nullable.GetUnderlyingType(prop.PropertyType) is null && typeName.IsValueType)
            {
                required.Add(prop.Name);
            }
        }
        var schemaObj = new
        {
            type = "object",
            properties,
            required = required.ToArray()
        };
        return JsonSerializer.Serialize(schemaObj);
    }
}

static class Exemplars
{
    public record Entry(string ToolKey, string Text);

    public static List<Entry> Load() => new()
    {
        new("checkVPNStatus", "remote tunnel to corporate network not establishing from outside office"),
        new("checkVPNStatus", "VPN client shows disconnected even after entering valid credentials"),
        new("checkVPNStatus", "AnyConnect auth keeps timing out when working remotely"),

        new("getUserAccount", "account suspended or locked in Active Directory after failed logins"),
        new("getUserAccount", "verify user permissions and group membership in the directory"),
        new("getUserAccount", "sign-in blocked, need to confirm account standing"),

        new("checkNetworkConnectivity", "no internet access on wired connection in the office"),
        new("checkNetworkConnectivity", "cannot reach internal websites or ping default gateway"),
        new("checkNetworkConnectivity", "DNS resolution failing for internal hosts"),

        new("resetPassword", "forgot password and need to set a new one"),
        new("resetPassword", "account password expired and locked out"),
        new("resetPassword", "request password reset link for user"),

        new("getSystemLogs", "retrieve event viewer logs around the time of the crash"),
        new("getSystemLogs", "need recent system logs to diagnose unexpected shutdown"),
        new("getSystemLogs", "application error entries from the last hour"),

        new("runDiagnostic", "run a quick health check on laptop hardware"),
        new("runDiagnostic", "diagnose random crashes and overheating"),
        new("runDiagnostic", "check memory and disk health"),

        new("getHardwareInfo", "get CPU and RAM specs for asset inventory"),
        new("getHardwareInfo", "laptop fan is loud, check hardware details"),
        new("getHardwareInfo", "report device hardware configuration"),

        new("checkDiskSpace", "disk is almost full and cannot save files"),
        new("checkDiskSpace", "low storage warning on C drive"),
        new("checkDiskSpace", "cleanup needed due to insufficient disk space"),

        new("getInstalledSoftware", "list software installed on this machine"),
        new("getInstalledSoftware", "check installed applications and versions"),
        new("getInstalledSoftware", "software inventory request for compliance"),

        new("restartService", "restart the print spooler service"),
        new("restartService", "service stuck in starting state, needs restart"),
        new("restartService", "application service not responding"),

        new("createSupportTicket", "open a helpdesk ticket for this issue"),
        new("createSupportTicket", "need support to follow up on recurring problem"),
        new("createSupportTicket", "create incident ticket for escalation"),

        new("escalateToSpecialist", "this issue needs L2 or specialist attention"),
        new("escalateToSpecialist", "escalate complex network problem to senior engineer"),
        new("escalateToSpecialist", "route to specialist team for deeper investigation")
    };
}
