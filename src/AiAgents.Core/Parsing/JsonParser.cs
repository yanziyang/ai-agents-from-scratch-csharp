using System.Text.Json;
using System.Text.RegularExpressions;

namespace AiAgents.Core.Parsing;

public static class JsonParser
{
    public static JsonElement Parse(string text, bool expectArray = false, bool debug = false)
    {
        var cleaned = CleanText(text);
        var extracted = ExtractJson(cleaned, expectArray);

        try
        {
            return JsonDocument.Parse(extracted).RootElement;
        }
        catch (JsonException first)
        {
            if (debug)
                Console.WriteLine("First parse attempt failed: " + first.Message);

            var repaired = AttemptRepairs(extracted);
            return JsonDocument.Parse(repaired).RootElement;
        }
    }

    public static T? Deserialize<T>(string text) where T : class
    {
        var element = Parse(text, expectArray: typeof(T).IsArray);
        return element.Deserialize<T>();
    }

    private static string CleanText(string text)
    {
        var cleaned = text
            .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
            .Replace("```", "")
            .Trim();

        cleaned = Regex.Replace(cleaned, @"^(Here's the plan:|JSON output:|Plan:|Output:)\s*", "", RegexOptions.IgnoreCase);
        return cleaned.Trim();
    }

    private static string ExtractJson(string text, bool expectArray)
    {
        var startChar = expectArray ? '[' : '{';
        var endChar = expectArray ? ']' : '}';

        var startIdx = text.IndexOf(startChar);
        var lastIdx = text.LastIndexOf(endChar);

        if (startIdx == -1 || lastIdx == -1 || startIdx >= lastIdx)
            return text;

        return text.Substring(startIdx, lastIdx - startIdx + 1);
    }

    private static string AttemptRepairs(string json)
    {
        var current = json;

        // Remove trailing commas before closing braces/brackets.
        current = Regex.Replace(current, @",(\s*[}\]])", "$1");

        // Quote unquoted property names.
        current = Regex.Replace(current, @"([{,]\s*)([a-zA-Z_][a-zA-Z0-9_]*)\s*:", "$1\"$2\":");

        // Convert single quotes to double quotes.
        current = current.Replace("'", "\"");

        // Add missing closing braces.
        var openBraces = current.Count(c => c == '{');
        var closeBraces = current.Count(c => c == '}');
        if (openBraces > closeBraces)
            current += new string('}', openBraces - closeBraces);

        // Add missing closing brackets.
        var openBrackets = current.Count(c => c == '[');
        var closeBrackets = current.Count(c => c == ']');
        if (openBrackets > closeBrackets)
            current += new string(']', openBrackets - closeBrackets);

        return current;
    }

    public static void ValidatePlan(JsonElement plan, string[] allowedTools, string[]? allowedDecisions = null)
    {
        if (plan.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Plan must be an object.");

        if (!plan.TryGetProperty("atoms", out var atomsProperty) || atomsProperty.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Plan must have an 'atoms' array.");

        var atoms = atomsProperty.EnumerateArray().ToList();
        if (atoms.Count == 0)
            throw new InvalidOperationException("Plan must have at least one atom.");

        var ids = new HashSet<int>();
        foreach (var atom in atoms)
        {
            var id = atom.GetProperty("id").GetInt32();
            if (!ids.Add(id))
                throw new InvalidOperationException($"Duplicate atom ID {id}.");

            var kind = atom.GetProperty("kind").GetString()!;
            var name = atom.GetProperty("name").GetString()!;

            if (kind == "tool" && !allowedTools.Contains(name))
                throw new InvalidOperationException($"Unknown tool '{name}' in atom {id}.");

            if (kind == "decision" && allowedDecisions != null && !allowedDecisions.Contains(name))
                throw new InvalidOperationException($"Unknown decision '{name}' in atom {id}.");

            if (kind == "tool" && (!atom.TryGetProperty("input", out var input) || input.ValueKind != JsonValueKind.Object))
                throw new InvalidOperationException($"Tool atom {id} must have an input object.");
        }
    }
}
