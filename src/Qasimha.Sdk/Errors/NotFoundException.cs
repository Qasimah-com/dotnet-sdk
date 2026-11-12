namespace Qasimha.Sdk;

/// <summary>A 404: the resource does not exist or belongs to another merchant.</summary>
public class NotFoundException : QasimhaException
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">What went wrong.</param>
    public NotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the error from an API response.</summary>
    /// <param name="message">The API's message.</param>
    /// <param name="details">The API-side facts.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public NotFoundException(string message, ApiErrorDetails details, Exception? innerException = null)
        : base(message, details, innerException)
    {
    }
}
