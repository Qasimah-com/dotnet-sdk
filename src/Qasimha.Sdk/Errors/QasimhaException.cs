namespace Qasimha.Sdk;

/// <summary>
/// Base type of every error the SDK raises. <see cref="Exception.Message"/> is the API's own
/// message (already localised by <c>Accept-Language</c>) or, for client-side failures, a fixed
/// description. The API key never appears in a message.
/// </summary>
public class QasimhaException : Exception
{
    /// <summary>Creates a client-side error with no API facts.</summary>
    /// <param name="message">What went wrong.</param>
    public QasimhaException(string message)
        : this(message, ApiErrorDetails.None, null)
    {
    }

    /// <summary>Creates a client-side error wrapping another exception.</summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">The underlying exception.</param>
    public QasimhaException(string message, Exception? innerException)
        : this(message, ApiErrorDetails.None, innerException)
    {
    }

    /// <summary>Creates an error carrying the API's facts.</summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="details">The API-side facts.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public QasimhaException(string message, ApiErrorDetails details, Exception? innerException = null)
        : base(message, innerException)
    {
        ArgumentNullException.ThrowIfNull(details);
        Code = details.Code;
        HttpStatus = details.HttpStatus;
        RequestId = details.RequestId;
        Errors = details.Errors ?? Array.Empty<ApiFieldError>();
        RawEnvelope = details.RawEnvelope;
    }

    /// <summary>The envelope <c>code</c> (for example <c>VALIDATION_ERROR</c>), or <see langword="null"/>.</summary>
    public string? Code { get; }

    /// <summary>The HTTP status, or <see langword="null"/> for transport and client-side failures.</summary>
    public int? HttpStatus { get; }

    /// <summary>The request correlation id: the envelope <c>uuid</c>, else the <c>X-Request-Id</c> header.</summary>
    public string? RequestId { get; }

    /// <summary>Field-level errors, with field names in the API's PascalCase (<c>Items[0].Quantity</c>).</summary>
    public IReadOnlyList<ApiFieldError> Errors { get; }

    /// <summary>The decoded response envelope, when there was one.</summary>
    public ApiEnvelope? RawEnvelope { get; }

    internal ResponseInfo? Response { get; set; }
}
