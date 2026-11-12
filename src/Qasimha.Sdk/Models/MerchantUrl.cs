namespace Qasimha.Sdk;

/// <summary>
/// Where the shopper is sent after the hosted checkout. Honoured since the API release of 2026-09-25.
/// </summary>
public sealed class MerchantUrl
{
    /// <summary>Opened after a completed checkout.</summary>
    public required string Success { get; set; }

    /// <summary>Opened after a declined or failed checkout.</summary>
    public required string Failure { get; set; }

    /// <summary>Opened when the shopper abandons the checkout.</summary>
    public required string Cancel { get; set; }

    /// <summary>Server-to-server notification URL, when different from your configured webhook endpoint.</summary>
    public string? Notification { get; set; }
}
