using System.Text.Json.Serialization;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>A created POS order: the customer opens <see cref="CheckoutUrl"/> to complete the hosted journey.</summary>
public sealed class PosSession : ApiResource
{
    /// <summary>The Qasimha order id (<c>installmentId</c>).</summary>
    [JsonPropertyName("installmentId")]
    public string OrderId { get; init; } = string.Empty;

    /// <summary>The hosted checkout page for this order.</summary>
    public string? CheckoutUrl { get; init; }

    /// <summary>The checkout provider's session id, when one exists.</summary>
    public string? ProviderSessionId { get; init; }

    /// <summary>The order status, as an <see cref="OrderStatus"/> name; <see cref="OrderStatus.AwaitingCheckout"/> on creation.</summary>
    [JsonConverter(typeof(OrderStatusConverter))]
    public string? Status { get; init; }
}
