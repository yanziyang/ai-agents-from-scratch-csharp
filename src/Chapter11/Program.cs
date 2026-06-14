using AiAgents.Core.Client;
using AiAgents.Core.Exceptions;
using AiAgents.Core.Tools;
using OpenAI.Chat;
using System.Text.RegularExpressions;

Console.WriteLine("=== Error Handling ===\n");

var config = ConfigurationFactory.Create();
var chatClient = DeepSeekClientFactory.CreateChatClient(config);

var messages = new List<ChatMessage>
{
    ChatMessage.CreateSystemMessage("""
        You are a helpful software engineering assistant.
        You can call tools to fetch user profile data.
        If the primary tool fails, try the fallback tool.
        When you answer the user:
        - be concise
        - include the user id you used
        - if the data came from fallback, mention it clearly
        """)
};

// Demo simulation rules
var forceNotFound = new HashSet<string>(StringComparer.Ordinal) { "u_999" };
var forcePrimaryAndFallbackFail = new HashSet<string>(StringComparer.Ordinal) { "u_777" };

var toolbox = new ToolBox();
toolbox.Add(new AgentTool("fetchUserFromPrimary", "Fetch a user profile from the primary user service",
    new { type = "object", properties = new { userId = new { type = "string" } }, required = new[] { "userId" } },
    async args => await FetchUserFromPrimary(args.GetProperty("userId").GetString()!)));

toolbox.Add(new AgentTool("fetchUserFromFallback", "Fetch a user profile from the fallback user service (less detailed but more reliable)",
    new { type = "object", properties = new { userId = new { type = "string" } }, required = new[] { "userId" } },
    async args => await FetchUserFromFallback(args.GetProperty("userId").GetString()!)));

var options = toolbox.CreateOptions();

async Task<object> FetchUserFromPrimary(string userId)
{
    await Task.Delay(80);

    if (forceNotFound.Contains(userId))
        throw new ToolExecutionError("fetchUserFromPrimary", "User not found", retryable: false, details: new { userId });

    if (forcePrimaryAndFallbackFail.Contains(userId))
        throw new ToolExecutionError("fetchUserFromPrimary", "Primary service temporarily overloaded", retryable: true, details: new { userId, demo = true });

    if (Random.Shared.NextDouble() < 0.2)
        throw new ToolExecutionError("fetchUserFromPrimary", "Network error while fetching user profile", retryable: true, details: new { userId });

    return new
    {
        userId,
        name = "Alex Developer",
        role = "Software Engineer",
        lastLoginIso = DateTime.UtcNow.AddHours(-6).ToString("O"),
        source = "primary"
    };
}

async Task<object> FetchUserFromFallback(string userId)
{
    await Task.Delay(60);

    if (forcePrimaryAndFallbackFail.Contains(userId))
        throw new ToolExecutionError("fetchUserFromFallback", "Fallback service unavailable", retryable: false, details: new { userId, demo = true });

    return new
    {
        userId,
        name = "Alex Developer",
        role = "Engineer",
        lastLoginIso = (string?)null,
        source = "fallback"
    };
}

async Task<string> PromptLlmAsync(string prompt, string correlationId)
{
    return await RetryHelper.WithRetries(async () =>
    {
        return await RetryHelper.WithTimeout(async ct =>
        {
            var requestMessages = new List<ChatMessage>(messages) { ChatMessage.CreateUserMessage(prompt) };
            var response = await chatClient.CompleteChatAsync(requestMessages, options, ct);

            var text = response.Value.Content[0].Text.Trim();
            if (string.IsNullOrEmpty(text))
                throw new LLMCallError("LLM returned empty output", details: new { correlationId });

            return text;
        }, TimeSpan.FromSeconds(15), "LLM prompt");
    }, retries: 1, label: "LLM prompt", retryOn: ErrorClassifier.IsRetryable);
}

async Task<string> RunDegradedProfileResolutionAsync(string userInput, string correlationId, Match? forcedMatch)
{
    var match = forcedMatch ?? Regex.Match(userInput, @"\b(u_\d+)\b", RegexOptions.IgnoreCase);
    if (!match.Success)
        throw new ValidationError("No user id found in request", userMessage: "Include a user id like \"u_123\" so I can fetch a profile.");

    var userId = match.Groups[1].Value;

    object profile;
    try
    {
        profile = await RetryHelper.WithRetries(
            () => RetryHelper.WithTimeout(ct => FetchUserFromPrimary(userId), TimeSpan.FromSeconds(1.2), "tool:fetchUserFromPrimary"),
            retries: 1,
            label: "tool:fetchUserFromPrimary",
            retryOn: e => e is ToolExecutionError tee && tee.Retryable);
    }
    catch (Exception toolErr)
    {
        if (ErrorClassifier.Normalize(toolErr) is ToolExecutionError { Retryable: true })
        {
            try
            {
                profile = await RetryHelper.WithTimeout(ct => FetchUserFromFallback(userId), TimeSpan.FromSeconds(1.2), "tool:fetchUserFromFallback");
            }
            catch (Exception fallbackErr)
            {
                throw new AgentWorkflowError("resolve_user_profile", "Primary user service had issues and fallback profile fetch also failed.",
                    details: new { correlationId, userId, phase = "degraded_fallback" }, innerException: fallbackErr);
            }
        }
        else
        {
            throw;
        }
    }

    dynamic p = profile;
    return $"Model unavailable; answered via deterministic fallback.\n- Name: {p.name} ({p.role})\n- Last login: {p.lastLoginIso ?? "unknown"} (source: {p.source})";
}

async Task<(bool ok, string output)> RunAgentAsync(string userInput)
{
    var correlationId = Guid.NewGuid().ToString("N")[..12];

    try
    {
        if (string.IsNullOrWhiteSpace(userInput))
            throw new ValidationError("Empty user input", userMessage: "Please provide a request (e.g., \"Fetch user u_123 and summarize their profile.\").");

        if (Regex.IsMatch(userInput, @"\bu_demo_workflow\b", RegexOptions.IgnoreCase))
            throw new AgentWorkflowError("policy_guard", "Demo: workflow cannot proceed (simulated blocked branch after validation).",
                userMessage: "I can't complete that request right now because of an internal workflow constraint. Try a normal user id like u_123.", retryable: false, details: new { reason = "demo_blocked_branch", correlationId });

        var forcedDegradedMatch = Regex.Match(userInput, @"SKIP_LLM_DEGRADED\s+\b(u_\d+)\b", RegexOptions.IgnoreCase);

        try
        {
            if (forcedDegradedMatch.Success)
                throw new LLMCallError("Skipped LLM for deterministic degraded-mode demo", retryable: false, details: new { correlationId, demo = "SKIP_LLM_DEGRADED" });

            var answer = await PromptLlmAsync(userInput, correlationId);
            return (true, answer);
        }
        catch (LLMCallError)
        {
            Console.WriteLine("[degraded_mode] LLM unavailable; switching to deterministic fallback.");
            var degraded = await RunDegradedProfileResolutionAsync(userInput, correlationId, forcedDegradedMatch.Success ? forcedDegradedMatch : null);
            return (true, degraded);
        }
    }
    catch (Exception ex)
    {
        var normalized = ErrorClassifier.Normalize(ex);

        if (normalized is AgentWorkflowError workflowError)
            PrintWorkflowErrorBanner(workflowError, correlationId);

        Console.Error.WriteLine("[agent_error] code={0} message={1} correlationId={2}", normalized.Code, normalized.Message, correlationId);
        return (false, FormatUserFacingError(normalized, correlationId));
    }
}

string FormatUserFacingError(AppError err, string correlationId)
{
    return $"{err.UserMessage}\n\n(Reference: {correlationId})";
}

void PrintWorkflowErrorBanner(AgentWorkflowError err, string correlationId)
{
    var divider = new string('═', 72);
    Console.Error.WriteLine("\n" + divider);
    Console.Error.WriteLine(" AGENT WORKFLOW FAILED");
    Console.Error.WriteLine(divider);
    Console.Error.WriteLine($" Step:           {err.Step}");
    Console.Error.WriteLine($" Code:           {err.Code}");
    Console.Error.WriteLine($" Correlation ID: {correlationId}");
    Console.Error.WriteLine($" User-facing:    {err.UserMessage}");
    Console.Error.WriteLine(new string('─', 72));
    Console.Error.WriteLine($" Developer msg:  {err.Message}");
    if (err.InnerException != null)
        Console.Error.WriteLine($" Cause:          {err.InnerException.GetType().Name}: {err.InnerException.Message}");
    Console.Error.WriteLine(divider + "\n");
}

var inputs = new[]
{
    "Fetch user u_123 and summarize their profile in 2 bullet points.",
    "Fetch user u_999 and tell me when they last logged in.",
    "Please fetch profile for u_demo_workflow.",
    "SKIP_LLM_DEGRADED u_777",
    ""
};

foreach (var input in inputs)
{
    Console.WriteLine("\n" + new string('-', 80));
    Console.WriteLine("USER: " + (string.IsNullOrEmpty(input) ? "(empty)" : input));
    var (ok, output) = await RunAgentAsync(input);
    Console.WriteLine(ok ? "\nASSISTANT:\n" + output : "\nASSISTANT (error):\n" + output);
}
