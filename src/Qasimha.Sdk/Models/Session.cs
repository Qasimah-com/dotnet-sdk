using System.Text.Json.Serialization;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>A created online payment session: redirect the shopper to <see cref="PaymentUrl"/>.</summary>
public sealed class Session : ApiResource
{
    /// <summary>The Qasimha order id (<c>installmentId</c>). Store it against your order reference.</summary>
    [JsonPropertyName("installmentId")]
    public string OrderId { get; init; } = string.Empty;

    /// <summary>The hosted checkout URL. Always present on a 201.</summary>
    public string? PaymentUrl { get; init; }

    /// <summary>The checkout provider's session id.</summary>
    public string? ProviderSessionId { get; init; }

    /// <summary>The order status, as an <see cref="OrderStatus"/> name.</summary>
    [JsonConverter(typeof(OrderStatusConverter))]
    public string? Status { get; init; }
}
