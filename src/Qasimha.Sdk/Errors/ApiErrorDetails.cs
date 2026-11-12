namespace Qasimha.Sdk;

/// <summary>The API-side facts attached to a <see cref="QasimhaException"/>.</summary>
/// <param name="Code">The envelope <c>code</c>, or <see langword="null"/> when the response had none.</param>
/// <param name="HttpStatus">The HTTP status, or <see langword="null"/> for a transport failure.</param>
/// <param name="RequestId">The envelope <c>uuid</c>, falling back to the <c>X-Request-Id</c> header.</param>
/// <param name="Errors">Field-level errors from the envelope.</param>
/// <param name="RawEnvelope">The decoded envelope, when the response carried one.</param>
public sealed record ApiErrorDetails(
    string? Code,
    int? HttpStatus,
    string? RequestId,
    IReadOnlyList<ApiFieldError>? Errors,
    ApiEnvelope? RawEnvelope)
{
    /// <summary>No API-side facts: used for client-side and transport failures.</summary>
    public static ApiErrorDetails None { get; } = new(null, null, null, null, null);
}
