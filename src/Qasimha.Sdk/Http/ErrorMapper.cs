using Qasimha.Sdk.Json;

namespace Qasimha.Sdk.Http;

internal static class ErrorMapper
{
    public const string DuplicateOrderReference = "DUPLICATE_ORDER_REFERENCE";

    public const string OrderDeclined = "ORDER_DECLINED";

    public const string ValidationError = "VALIDATION_ERROR";

    public const string RequestInProgress = "REQUEST_IN_PROGRESS";

    public static QasimhaException Map(ApiResponse response)
    {
        var envelope = response.Envelope;
        var code = envelope?.Code;
        var details = new ApiErrorDetails(code, response.Status, response.Info.RequestId, envelope?.Errors, envelope);
        var message = envelope?.Message ?? DefaultMessage(response);

        QasimhaException error = response.Status switch
        {
            400 => new ValidationException(message, details),
            401 => new AuthenticationException(message, details),
            403 => new PermissionException(message, details),
            404 => new NotFoundException(message, details),
            409 when IsCode(code, DuplicateOrderReference) => Duplicate(message, details, envelope),
            409 => new ConflictException(message, details),
            422 when IsCode(code, OrderDeclined) => Declined(message, details, envelope),
            429 => new RateLimitException(message, details, response.RetryAfter),
            >= 500 => new ServerException(message, details),
            >= 200 and < 300 => MapEnvelopeFailure(message, details, envelope, response),
            _ => new ApiException(message, details),
        };

        error.Response = response.Info;
        return error;
    }

    private static QasimhaException MapEnvelopeFailure(string message, ApiErrorDetails details, ApiEnvelope? envelope, ApiResponse response)
    {
        if (response.MalformedJson)
        {
            return new ApiException("The response body is not an API envelope.", details);
        }

        if (IsCode(details.Code, DuplicateOrderReference))
        {
            return Duplicate(message, details, envelope);
        }

        if (IsCode(details.Code, OrderDeclined))
        {
            return Declined(message, details, envelope);
        }

        if (IsCode(details.Code, ValidationError))
        {
            return new ValidationException(message, details);
        }

        return new ApiException(message, details);
    }

    private static DuplicateOrderReferenceException Duplicate(string message, ApiErrorDetails details, ApiEnvelope? envelope) =>
        new(message, details, DataString(envelope, "installmentId"), DataString(envelope, "paymentUrl"));

    private static OrderDeclinedException Declined(string message, ApiErrorDetails details, ApiEnvelope? envelope) =>
        new(message, details, DataString(envelope, "installmentId"));

    private static bool IsCode(string? code, string expected) =>
        string.Equals(code, expected, StringComparison.OrdinalIgnoreCase);

    private static string? DataString(ApiEnvelope? envelope, string name) =>
        envelope?.Data.GetStringOrNull(name);

    private static string DefaultMessage(ApiResponse response) => response.Status switch
    {
        401 => "The API key was rejected (HTTP 401).",
        403 => "The API key lacks the scope for this operation (HTTP 403).",
        _ => string.IsNullOrEmpty(response.ReasonPhrase)
            ? FormattableString.Invariant($"HTTP {response.Status}")
            : FormattableString.Invariant($"HTTP {response.Status} {response.ReasonPhrase}"),
    };
}
