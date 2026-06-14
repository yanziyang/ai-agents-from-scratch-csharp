namespace AiAgents.Core.Exceptions;

public static class ErrorClassifier
{
    public static AppError Normalize(Exception ex)
    {
        if (ex is AppError appError)
            return appError;

        return new AppError("UNKNOWN_ERROR", ex.Message, retryable: false, details: new { originalName = ex.GetType().Name }, innerException: ex);
    }

    public static bool IsRetryable(Exception ex)
    {
        return ex is AppError appError && appError.Retryable;
    }
}
