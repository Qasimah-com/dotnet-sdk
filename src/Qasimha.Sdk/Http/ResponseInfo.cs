namespace Qasimha.Sdk;

/// <summary>Transport-level facts about the response a result was built from.</summary>
public sealed class ResponseInfo
{
    internal ResponseInfo()
    {
    }

    /// <summary>The HTTP status.</summary>
    public int HttpStatus { get; internal init; }

    /// <summary>The request correlation id: the envelope <c>uuid</c>, else the <c>X-Request-Id</c> header.</summary>
    public string? RequestId { get; internal init; }

    /// <summary>The <c>X-Request-Id</c> header as sent by the API.</summary>
    public string? RequestIdHeader { get; internal init; }

    /// <summary>The <c>X-Correlation-Id</c> the SDK generated for this logical call.</summary>
    public string CorrelationId { get; internal init; } = string.Empty;

    /// <summary>The <c>X-Transaction-Id</c> header, when present.</summary>
    public string? TransactionId { get; internal init; }

    /// <summary><see langword="true"/> when the API answered from its idempotency store (<c>Idempotent-Replay: true</c>).</summary>
    public bool Replayed { get; internal init; }

    /// <summary>Which attempt of the logical call produced this response (1 for the first).</summary>
    public int Attempt { get; internal init; }
}
