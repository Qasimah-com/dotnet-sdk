using System.Text.Json.Serialization;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>Result of <see cref="OrdersResource.FulfillAsync"/>. Further fields are in <see cref="ApiResource.Raw"/>.</summary>
public sealed class FulfillResult : ApiResource
{
    /// <summary>The Qasimha order id.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The order status after the fulfilment, as an <see cref="OrderStatus"/> name.</summary>
    [JsonConverter(typeof(OrderStatusConverter))]
    public string? Status { get; init; }

    /// <summary>Total fulfilled on the order so far, when returned.</summary>
    public decimal? FulfilledAmount { get; init; }
}
