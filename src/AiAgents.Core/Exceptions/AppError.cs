namespace AiAgents.Core.Exceptions;

public class AppError : Exception
{
    public string Code { get; }
    public string UserMessage { get; }
    public bool Retryable { get; }
    public object? Details { get; }

    public AppError(string code, string message, string? userMessage = null, bool retryable = false, object? details = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        UserMessage = userMessage ?? "Something went wrong. Please try again.";
        Retryable = retryable;
        Details = details;
    }
}

public class ValidationError : AppError
{
    public ValidationError(string message, string? userMessage = null, object? details = null, Exception? innerException = null)
        : base("VALIDATION_ERROR", message, userMessage ?? "I couldn't understand that request. Please rephrase and try again.", retryable: false, details, innerException)
    {
    }
}

public class LLMCallError : AppError
{
    public string? Model { get; }

    public LLMCallError(string message, string? model = null, string? userMessage = null, bool retryable = true, object? details = null, Exception? innerException = null)
        : base("LLM_CALL_FAILED", message, userMessage ?? "I'm having trouble generating a response right now. Please try again in a moment.", retryable, details, innerException)
    {
        Model = model;
    }
}

public class ToolExecutionError : AppError
{
    public string ToolName { get; }

    public ToolExecutionError(string toolName, string message, string? userMessage = null, bool retryable = false, object? details = null, Exception? innerException = null)
        : base("TOOL_EXECUTION_FAILED", message, userMessage ?? $"I couldn't run the tool \"{toolName}\" successfully. You can try again, or choose a different approach.", retryable, details, innerException)
    {
        ToolName = toolName;
    }
}

public class AgentWorkflowError : AppError
{
    public string Step { get; }

    public AgentWorkflowError(string step, string message, string? userMessage = null, bool retryable = false, object? details = null, Exception? innerException = null)
        : base("AGENT_WORKFLOW_FAILED", message, userMessage ?? "I ran into a problem while completing your request. Please try again, or provide a bit more detail.", retryable, details, innerException)
    {
        Step = step;
    }
}
