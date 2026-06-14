using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text;
using System.Text.Json;

namespace AiAgents.Core.Client;

/// <summary>
/// Logs the raw HTTP request sent to DeepSeek and the raw HTTP response
/// received from DeepSeek to a human-readable file in the configured log
/// directory. One file is created per request/response round-trip.
/// </summary>
public sealed class DeepSeekLoggingPolicy : PipelinePolicy
{
    private readonly LoggingOptions _options;

    public DeepSeekLoggingPolicy(LoggingOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        var logState = PrepareLogState(message.Request);
        WriteRequestSection(logState);

        try
        {
            ProcessNext(message, pipeline, currentIndex);
            WriteResponseSection(message, logState);
        }
        catch (Exception ex)
        {
            // Make sure a failed request is still partially logged.
            WriteExceptionFooter(logState.FilePath, ex);
            throw;
        }
    }

    public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        var logState = PrepareLogState(message.Request);
        await WriteRequestSectionAsync(logState, message.CancellationToken).ConfigureAwait(false);

        try
        {
            await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
            await WriteResponseSectionAsync(message, logState).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await WriteExceptionFooterAsync(logState.FilePath, ex).ConfigureAwait(false);
            throw;
        }
    }

    private LogState PrepareLogState(PipelineRequest request)
    {
        Directory.CreateDirectory(_options.LogDirectory);

        var correlationId = Guid.NewGuid().ToString("N")[..8];
        var timestamp = DateTime.UtcNow;
        var fileName = $"deepseek_{timestamp:yyyyMMdd_HHmmss_fff}_{correlationId}.log";
        var filePath = Path.Combine(_options.LogDirectory, fileName);

        return new LogState(filePath, correlationId, request.ClientRequestId, timestamp)
        {
            Request = request
        };
    }

    private static void WriteRequestSection(LogState state)
    {
        var request = state.Request;
        if (request is null || request.Content is null)
        {
            WriteFile(state.FilePath, BuildHeader(state) + "=== REQUEST ===\n(no request body)\n\n");
            return;
        }

        using var buffer = new MemoryStream();
        request.Content.WriteTo(buffer);
        var bodyText = Encoding.UTF8.GetString(buffer.ToArray());

        var builder = new StringBuilder();
        builder.Append(BuildHeader(state));
        builder.AppendLine("=== REQUEST ===");
        builder.AppendLine($"Timestamp: {state.Timestamp:O}");
        builder.AppendLine($"Method: {request.Method}");
        builder.AppendLine($"URI: {request.Uri}");
        builder.AppendLine("Headers:");
        AppendHeaders(builder, request.Headers);
        builder.AppendLine("Body:");
        builder.AppendLine(FormatJson(bodyText));
        builder.AppendLine();

        WriteFile(state.FilePath, builder.ToString());
    }

    private static async Task WriteRequestSectionAsync(LogState state, CancellationToken cancellationToken)
    {
        var request = state.Request;
        if (request is null || request.Content is null)
        {
            await WriteFileAsync(state.FilePath, BuildHeader(state) + "=== REQUEST ===\n(no request body)\n\n", cancellationToken).ConfigureAwait(false);
            return;
        }

        using var buffer = new MemoryStream();
        await request.Content.WriteToAsync(buffer, cancellationToken).ConfigureAwait(false);
        var bodyText = Encoding.UTF8.GetString(buffer.ToArray());

        var builder = new StringBuilder();
        builder.Append(BuildHeader(state));
        builder.AppendLine("=== REQUEST ===");
        builder.AppendLine($"Timestamp: {state.Timestamp:O}");
        builder.AppendLine($"Method: {request.Method}");
        builder.AppendLine($"URI: {request.Uri}");
        builder.AppendLine("Headers:");
        AppendHeaders(builder, request.Headers);
        builder.AppendLine("Body:");
        builder.AppendLine(FormatJson(bodyText));
        builder.AppendLine();

        await WriteFileAsync(state.FilePath, builder.ToString(), cancellationToken).ConfigureAwait(false);
    }

    private static void WriteResponseSection(PipelineMessage message, LogState state)
    {
        var response = message.Response;
        if (response is null)
        {
            AppendFile(state.FilePath, "=== RESPONSE ===\n(no response received)\n");
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("=== RESPONSE ===");
        builder.AppendLine($"Timestamp: {DateTime.UtcNow:O}");
        builder.AppendLine($"Status: {response.Status} {response.ReasonPhrase}");
        builder.AppendLine("Headers:");
        AppendHeaders(builder, response.Headers);

        if (message.BufferResponse)
        {
            builder.AppendLine("Body:");
            builder.AppendLine(FormatJson(response.Content.ToString()));
            AppendFile(state.FilePath, builder.ToString());
        }
        else if (response.ContentStream is not null)
        {
            builder.AppendLine("Body (streaming - raw SSE chunks follow):");
            AppendFile(state.FilePath, builder.ToString());
            response.ContentStream = new LoggingStream(response.ContentStream, state.FilePath);
        }
        else
        {
            builder.AppendLine("Body: (empty)");
            AppendFile(state.FilePath, builder.ToString());
        }
    }

    private static async Task WriteResponseSectionAsync(PipelineMessage message, LogState state)
    {
        var response = message.Response;
        if (response is null)
        {
            await AppendFileAsync(state.FilePath, "=== RESPONSE ===\n(no response received)\n", message.CancellationToken).ConfigureAwait(false);
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("=== RESPONSE ===");
        builder.AppendLine($"Timestamp: {DateTime.UtcNow:O}");
        builder.AppendLine($"Status: {response.Status} {response.ReasonPhrase}");
        builder.AppendLine("Headers:");
        AppendHeaders(builder, response.Headers);

        if (message.BufferResponse)
        {
            builder.AppendLine("Body:");
            builder.AppendLine(FormatJson(response.Content.ToString()));
            await AppendFileAsync(state.FilePath, builder.ToString(), message.CancellationToken).ConfigureAwait(false);
        }
        else if (response.ContentStream is not null)
        {
            builder.AppendLine("Body (streaming - raw SSE chunks follow):");
            await AppendFileAsync(state.FilePath, builder.ToString(), message.CancellationToken).ConfigureAwait(false);
            response.ContentStream = new LoggingStream(response.ContentStream, state.FilePath);
        }
        else
        {
            builder.AppendLine("Body: (empty)");
            await AppendFileAsync(state.FilePath, builder.ToString(), message.CancellationToken).ConfigureAwait(false);
        }
    }

    private static string BuildHeader(LogState state)
    {
        var builder = new StringBuilder();
        builder.AppendLine("=== DeepSeek API Log ===");
        builder.AppendLine($"Generated At: {DateTime.UtcNow:O}");
        builder.AppendLine($"Correlation ID: {state.CorrelationId}");
        builder.AppendLine($"Client Request ID: {state.ClientRequestId}");
        builder.AppendLine();
        return builder.ToString();
    }

    private static void AppendHeaders(StringBuilder builder, IEnumerable<KeyValuePair<string, string>> headers)
    {
        foreach (var header in headers)
        {
            var value = header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                ? "[REDACTED]"
                : header.Value;
            builder.AppendLine($"  {header.Key}: {value}");
        }
    }

    private static string FormatJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            // If the payload isn't valid JSON, return it as-is.
            return json;
        }
    }

    private static void WriteFile(string path, string content)
    {
        try
        {
            File.WriteAllText(path, content, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[DeepSeekLoggingPolicy] Failed to write log file '{path}': {ex.Message}");
        }
    }

    private static async Task WriteFileAsync(string path, string content, CancellationToken cancellationToken)
    {
        try
        {
            await File.WriteAllTextAsync(path, content, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"[DeepSeekLoggingPolicy] Failed to write log file '{path}': {ex.Message}").ConfigureAwait(false);
        }
    }

    private static void AppendFile(string path, string content)
    {
        try
        {
            File.AppendAllText(path, content, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[DeepSeekLoggingPolicy] Failed to append log file '{path}': {ex.Message}");
        }
    }

    private static async Task AppendFileAsync(string path, string content, CancellationToken cancellationToken)
    {
        try
        {
            await File.AppendAllTextAsync(path, content, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"[DeepSeekLoggingPolicy] Failed to append log file '{path}': {ex.Message}").ConfigureAwait(false);
        }
    }

    private static void WriteExceptionFooter(string path, Exception exception)
    {
        try
        {
            File.AppendAllText(path, $"=== RESPONSE ===\nRequest failed before a response was received.\nException: {exception.Message}\n", Encoding.UTF8);
        }
        catch
        {
            // Best-effort: don't let logging failures mask the original exception.
        }
    }

    private static async Task WriteExceptionFooterAsync(string path, Exception exception)
    {
        try
        {
            await File.AppendAllTextAsync(path, $"=== RESPONSE ===\nRequest failed before a response was received.\nException: {exception.Message}\n", Encoding.UTF8).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort: don't let logging failures mask the original exception.
        }
    }

    private sealed class LogState
    {
        public LogState(string filePath, string correlationId, string clientRequestId, DateTime timestamp)
        {
            FilePath = filePath;
            CorrelationId = correlationId;
            ClientRequestId = clientRequestId;
            Timestamp = timestamp;
        }

        public string FilePath { get; }
        public string CorrelationId { get; }
        public string ClientRequestId { get; }
        public DateTime Timestamp { get; }

        public PipelineRequest? Request { get; set; }
    }
}
