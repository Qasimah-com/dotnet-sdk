namespace Qasimha.Sdk;

/// <summary>Any 409 other than a duplicate reference: <c>MERCHANT_SUSPENDED</c>, <c>INVALID_STATE</c>, <c>NOT_OPEN</c>, or <c>REQUEST_IN_PROGRESS</c> after the retries ran out.</summary>
public class ConflictException : QasimhaException
{
    /// <summary>Creates the error.</summary>
    /// <param name="message">What went wrong.</param>
    public ConflictException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the error from an API response.</summary>
    /// <param name="message">The API's message.</param>
    /// <param name="details">The API-side facts.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public ConflictException(string message, ApiErrorDetails details, Exception? innerException = null)
        : base(message, details, innerException)
    {
    }
}
