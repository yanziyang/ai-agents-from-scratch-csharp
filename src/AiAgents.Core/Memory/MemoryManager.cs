using System.Text.Json;

namespace AiAgents.Core.Memory;

public record MemoryEntry(
    string Type,
    string Key,
    string Value,
    string Source = "user",
    DateTime? Timestamp = null);

public class MemoryStore
{
    public List<MemoryEntry> Memories { get; set; } = new();
    public List<string> ConversationHistory { get; set; } = new();
}

public class MemoryManager
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public MemoryManager(string filePath)
    {
        _filePath = filePath;
    }

    public MemoryStore Load()
    {
        if (!File.Exists(_filePath))
            return new MemoryStore();

        try
        {
            var json = File.ReadAllText(_filePath);
            var store = JsonSerializer.Deserialize<MemoryStore>(json, JsonOptions);
            return store ?? new MemoryStore();
        }
        catch
        {
            return new MemoryStore();
        }
    }

    public void Save(MemoryStore store)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(_filePath, JsonSerializer.Serialize(store, JsonOptions));
    }

    public void AddMemory(string type, string key, string value, string source = "user")
    {
        var store = Load();

        var normType = type.Trim().ToLowerInvariant();
        var normKey = key.Trim().ToLowerInvariant();
        var normValue = value.Trim();

        var existing = store.Memories.FirstOrDefault(m =>
            m.Type.Equals(normType, StringComparison.OrdinalIgnoreCase) &&
            m.Key.Equals(normKey, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            if (!existing.Value.Equals(normValue, StringComparison.OrdinalIgnoreCase))
            {
                store.Memories.Remove(existing);
                store.Memories.Add(new MemoryEntry(normType, normKey, normValue, source, DateTime.UtcNow));
                Console.WriteLine($"Updated memory: {normKey} -> {normValue}");
            }
            else
            {
                Console.WriteLine($"Skipped duplicate memory: {normKey}");
            }
        }
        else
        {
            store.Memories.Add(new MemoryEntry(normType, normKey, normValue, source, DateTime.UtcNow));
            Console.WriteLine($"Added memory: {normKey} = {normValue}");
        }

        Save(store);
    }

    public string GetMemorySummary()
    {
        var store = Load();
        var facts = store.Memories.Where(m => m.Type.Equals("fact", StringComparison.OrdinalIgnoreCase)).ToList();
        var prefs = store.Memories.Where(m => m.Type.Equals("preference", StringComparison.OrdinalIgnoreCase)).ToList();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("\n=== LONG-TERM MEMORY ===");

        if (facts.Count > 0)
        {
            sb.AppendLine("\nKnown Facts:");
            foreach (var f in facts)
                sb.AppendLine($"- {f.Key}: {f.Value}");
        }

        if (prefs.Count > 0)
        {
            sb.AppendLine("\nUser Preferences:");
            foreach (var p in prefs)
                sb.AppendLine($"- {p.Key}: {p.Value}");
        }

        return sb.ToString();
    }
}
