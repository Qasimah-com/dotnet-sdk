namespace Qasimha.Sdk;

/// <summary>
/// A 400 from the API (<c>VALIDATION_ERROR</c>, <c>INVALID_AMOUNT</c>, <c>PHONE_REQUIRED</c>,
/// <c>INVALID_STATUS</c>, <c>AMOUNT_EXCEEDED</c>, …) or a request the SDK refused before sending it.
/// <see cref="QasimhaException.Errors"/> names the failing fields when known.
/// </summary>
public class ValidationException : QasimhaException
{
    /// <summary>Creates a client-side validation error.</summary>
    /// <param name="message">What was wrong.</param>
    public ValidationException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a client-side validation error naming the failing fields.</summary>
    /// <param name="message">What was wrong.</param>
    /// <param name="errors">The failing fields.</param>
    public ValidationException(string message, IReadOnlyList<ApiFieldError> errors)
        : base(message, new ApiErrorDetails(null, null, null, errors, null))
    {
    }

    /// <summary>Creates the error from an API response.</summary>
    /// <param name="message">The API's message.</param>
    /// <param name="details">The API-side facts.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public ValidationException(string message, ApiErrorDetails details, Exception? innerException = null)
        : base(message, details, innerException)
    {
    }
}
