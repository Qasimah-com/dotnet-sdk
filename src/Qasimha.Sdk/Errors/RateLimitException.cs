namespace Qasimha.Sdk;

/// <summary>A 429 after the retries ran out. <see cref="RetryAfter"/> carries the server's hint when it sent one.</summary>
public class RateLimitException : QasimhaException
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">What went wrong.</param>
    public RateLimitException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the error from an API response.</summary>
    /// <param name="message">The API's message.</param>
    /// <param name="details">The API-side facts.</param>
    /// <param name="retryAfter">The parsed <c>Retry-After</c> header, capped at 30 seconds, when present.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public RateLimitException(string message, ApiErrorDetails details, TimeSpan? retryAfter, Exception? innerException = null)
        : base(message, details, innerException)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>How long the server asked you to wait, when it said.</summary>
    public TimeSpan? RetryAfter { get; }
}
