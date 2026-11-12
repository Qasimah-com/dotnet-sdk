namespace Qasimha.Sdk;

/// <summary>A webhook delivery failed verification. Never act on the payload; answer with a 4xx.</summary>
public class WebhookSignatureException : QasimhaException
{
    /// <summary>Creates the error.</summary>
    /// <param name="reason">Which check failed.</param>
    /// <param name="message">What went wrong.</param>
    public WebhookSignatureException(WebhookSignatureFailure reason, string message)
        : base(message)
    {
        Reason = reason;
    }

    /// <summary>Which check failed.</summary>
    public WebhookSignatureFailure Reason { get; }

    /// <summary>The reason as the snake_case code shared by every Qasimha SDK.</summary>
    public string ReasonCode => Reason switch
    {
        WebhookSignatureFailure.MissingHeader => "missing_header",
        WebhookSignatureFailure.MalformedHeader => "malformed_header",
        WebhookSignatureFailure.TimestampOutsideTolerance => "timestamp_outside_tolerance",
        _ => "signature_mismatch",
    };
}
