using System.Net.Http.Headers;

namespace Qasimha.Sdk.Http;

internal static class RetryPolicy
{
    private const double BaseDelayMilliseconds = 500;

    private const double MaxBackoffMilliseconds = 8_000;

    private const int MaxJitterMilliseconds = 250;

    private static readonly TimeSpan MaxRetryAfter = TimeSpan.FromSeconds(30);

    public static bool IsRetryable(ApiResponse response) =>
        response.Status is 429 or 502 or 503 or 504
        || (response.Status == 409
            && string.Equals(response.Envelope?.Code, ErrorMapper.RequestInProgress, StringComparison.OrdinalIgnoreCase));

    public static TimeSpan Backoff(int attempt)
    {
        var milliseconds = Math.Min(Math.Pow(2, attempt) * BaseDelayMilliseconds, MaxBackoffMilliseconds);
        return TimeSpan.FromMilliseconds(milliseconds + Random.Shared.Next(0, MaxJitterMilliseconds + 1));
    }

    public static TimeSpan? ParseRetryAfter(HttpResponseHeaders headers, DateTimeOffset now)
    {
        var retryAfter = headers.RetryAfter;
        if (retryAfter is null)
        {
            return null;
        }

        TimeSpan? wait = retryAfter.Delta ?? (retryAfter.Date is { } date ? date - now : null);
        if (wait is null || wait <= TimeSpan.Zero)
        {
            return null;
        }

        return wait > MaxRetryAfter ? MaxRetryAfter : wait;
    }
}
