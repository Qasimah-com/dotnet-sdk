namespace Qasimha.Sdk;

/// <summary>A 5xx. 502, 503 and 504 are retried first; a 500 is never retried because a write may have committed.</summary>
public class ServerException : ApiException
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">What went wrong.</param>
    public ServerException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the error from an API response.</summary>
    /// <param name="message">The API's message, or a fixed description when the body had none.</param>
    /// <param name="details">The API-side facts.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public ServerException(string message, ApiErrorDetails details, Exception? innerException = null)
        : base(message, details, innerException)
    {
    }
}
