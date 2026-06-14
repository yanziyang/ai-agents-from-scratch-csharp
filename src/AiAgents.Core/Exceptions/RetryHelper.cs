using System.Security.Cryptography;

namespace AiAgents.Core.Exceptions;

public static class RetryHelper
{
    public static async Task<T> WithTimeout<T>(Func<CancellationToken, Task<T>> action, TimeSpan timeout, string label = "operation")
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            return await action(cts.Token);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            throw new AppError("TIMEOUT", $"{label} timed out after {timeout.TotalMilliseconds}ms", retryable: true, details: new { label, timeoutMs = timeout.TotalMilliseconds });
        }
    }

    public static async Task<T> WithRetries<T>(Func<Task<T>> action, int retries = 2, int baseDelayMs = 200, int maxDelayMs = 3000, string label = "operation", Func<Exception, bool>? retryOn = null)
    {
        retryOn ??= ErrorClassifier.IsRetryable;
        var maxAttempts = retries + 1;
        Exception? lastError = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                lastError = ex;
                if (attempt == maxAttempts || !retryOn(ex))
                    break;

                var delay = JitteredBackoffDelay(attempt, baseDelayMs, maxDelayMs);
                Console.WriteLine($"[retry] {label} failed (attempt {attempt}/{maxAttempts}). Retrying in {delay}ms.");
                await Task.Delay(delay);
            }
        }

        throw lastError!;
    }

    private static int JitteredBackoffDelay(int attempt, int baseDelayMs, int maxDelayMs)
    {
        var exponential = Math.Min(maxDelayMs, baseDelayMs * Math.Pow(2, attempt - 1));
        var jitter = RandomNumberGenerator.GetInt32(0, Math.Max(1, (int)(exponential * 0.25)));
        return (int)exponential + jitter;
    }
}
