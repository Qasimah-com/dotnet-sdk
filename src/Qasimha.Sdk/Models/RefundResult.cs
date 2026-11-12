using System.Text.Json.Serialization;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>Result of <see cref="OrdersResource.RefundAsync"/>.</summary>
public sealed class RefundResult : ApiResource
{
    /// <summary>The Qasimha order id.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The order status after the refund, as an <see cref="OrderStatus"/> name.</summary>
    [JsonConverter(typeof(OrderStatusConverter))]
    public string? Status { get; init; }

    /// <summary>Total refunded on the order so far.</summary>
    public decimal RefundedAmount { get; init; }
}
