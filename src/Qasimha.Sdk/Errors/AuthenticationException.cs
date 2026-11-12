namespace Qasimha.Sdk;

/// <summary>A 401: the API key is missing, malformed or revoked. The body is usually empty.</summary>
public class AuthenticationException : QasimhaException
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">What went wrong.</param>
    public AuthenticationException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the error from an API response.</summary>
    /// <param name="message">The API's message, or a fixed description when the body was empty.</param>
    /// <param name="details">The API-side facts.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public AuthenticationException(string message, ApiErrorDetails details, Exception? innerException = null)
        : base(message, details, innerException)
    {
    }
}
