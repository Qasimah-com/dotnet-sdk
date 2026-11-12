using System.Text.Json.Serialization;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>A created QR session: render <see cref="QrCodeUrl"/> as a QR code for the customer to scan.</summary>
public sealed class QrSession : ApiResource
{
    /// <summary>The Qasimha order id (<c>installmentId</c>).</summary>
    [JsonPropertyName("installmentId")]
    public string OrderId { get; init; } = string.Empty;

    /// <summary>The URL to encode in the QR code.</summary>
    public string? QrCodeUrl { get; init; }

    /// <summary>The checkout provider's session id, when one exists.</summary>
    public string? ProviderSessionId { get; init; }

    /// <summary>The order status, as an <see cref="OrderStatus"/> name.</summary>
    [JsonConverter(typeof(OrderStatusConverter))]
    public string? Status { get; init; }
}
