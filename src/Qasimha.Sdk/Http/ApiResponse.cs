using System.Net.Http.Headers;

namespace Qasimha.Sdk.Http;

internal sealed class ApiResponse
{
    public int Status { get; init; }

    public string? ReasonPhrase { get; init; }

    public ApiEnvelope? Envelope { get; init; }

    public bool MalformedJson { get; init; }

    public byte[] Body { get; init; } = Array.Empty<byte>();

    public string? ContentType { get; init; }

    public string? FileName { get; init; }

    public ResponseInfo Info { get; init; } = new();

    public TimeSpan? RetryAfter { get; init; }

    public bool IsSuccess =>
        Status is >= 200 and < 300
        && !MalformedJson
        && (Envelope is null || !Envelope.HasSuccessFlag || Envelope.Success);

    public static ApiResponse From(HttpResponseMessage response, byte[] body, string correlationId, int attempt, DateTimeOffset now)
    {
        var status = (int)response.StatusCode;
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        var looksJson = mediaType is null
            ? LooksLikeJson(body)
            : mediaType.Contains("json", StringComparison.OrdinalIgnoreCase);

        ApiEnvelope? envelope = null;
        var malformed = false;
        if (looksJson && body.Length > 0)
        {
            envelope = ApiEnvelope.TryParse(body);
            malformed = envelope is null;
        }

        var requestIdHeader = FirstHeader(response.Headers, "X-Request-Id");
        var replayed = string.Equals(FirstHeader(response.Headers, "Idempotent-Replay"), "true", StringComparison.OrdinalIgnoreCase);

        return new ApiResponse
        {
            Status = status,
            ReasonPhrase = response.ReasonPhrase,
            Envelope = envelope,
            MalformedJson = malformed,
            Body = body,
            ContentType = mediaType,
            FileName = FileNameOf(response.Content.Headers.ContentDisposition),
            RetryAfter = RetryPolicy.ParseRetryAfter(response.Headers, now),
            Info = new ResponseInfo
            {
                HttpStatus = status,
                RequestId = envelope?.Uuid ?? requestIdHeader,
                RequestIdHeader = requestIdHeader,
                CorrelationId = correlationId,
                TransactionId = FirstHeader(response.Headers, "X-Transaction-Id"),
                Replayed = replayed,
                Attempt = attempt,
            },
        };
    }

    private static string? FirstHeader(HttpResponseHeaders headers, string name) =>
        headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;

    private static string? FileNameOf(ContentDispositionHeaderValue? disposition)
    {
        var name = disposition?.FileNameStar ?? disposition?.FileName;
        return string.IsNullOrEmpty(name) ? null : name.Trim('"');
    }

    private static bool LooksLikeJson(byte[] body)
    {
        foreach (var b in body)
        {
            if (b is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n' or 0xEF or 0xBB or 0xBF)
            {
                continue;
            }

            return b is (byte)'{' or (byte)'[';
        }

        return false;
    }
}
