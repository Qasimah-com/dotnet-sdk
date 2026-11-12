namespace Qasimha.Sdk;

/// <summary>Per-call options for a write (POST) request.</summary>
public sealed class RequestOptions
{
    /// <summary>
    /// Your own <c>Idempotency-Key</c> for this call. Leave it unset and the SDK generates a UUID v4
    /// per logical call and reuses it across that call's retries.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// When <see langword="false"/>, no <c>Idempotency-Key</c> is sent and the POST is never retried.
    /// </summary>
    public bool Idempotent { get; set; } = true;
}
