namespace Qasimha.Sdk;

/// <summary>Why a webhook delivery failed verification.</summary>
public enum WebhookSignatureFailure
{
    /// <summary>No <c>X-Qasimha-Signature</c> header. There is no unsigned path.</summary>
    MissingHeader,

    /// <summary>The signature or timestamp header could not be parsed.</summary>
    MalformedHeader,

    /// <summary>The delivery timestamp is too far from now.</summary>
    TimestampOutsideTolerance,

    /// <summary>No signature in the header matches the body.</summary>
    SignatureMismatch,
}
