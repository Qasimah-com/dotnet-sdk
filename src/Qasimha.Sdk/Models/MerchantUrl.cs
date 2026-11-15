using System.Text.Json.Serialization;

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

    /// <summary>Opened when the shopper abandons the checkout. The API uses <see cref="Failure"/> when omitted.</summary>
    public string? Cancel { get; set; }

    /// <summary>Reserved. The API refuses any value, so the SDK never sends it and refuses a request that sets it.</summary>
    [Obsolete("MerchantUrl.Notification is reserved and must not be set. Register a webhook endpoint instead.")]
    [JsonIgnore]
    public string? Notification { get; set; }
}
