namespace Qasimha.Sdk;

/// <summary>Any other HTTP failure: an unexpected 4xx, a 3xx (the API never redirects), or a 2xx whose envelope says <c>success: false</c>.</summary>
public class ApiException : QasimhaException
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">What went wrong.</param>
    public ApiException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the error from an API response.</summary>
    /// <param name="message">The API's message, or a fixed description when the body had none.</param>
    /// <param name="details">The API-side facts.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public ApiException(string message, ApiErrorDetails details, Exception? innerException = null)
        : base(message, details, innerException)
    {
    }
}
