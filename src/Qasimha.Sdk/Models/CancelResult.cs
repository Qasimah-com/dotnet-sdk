using System.Text.Json.Serialization;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>Result of <see cref="OrdersResource.CancelAsync"/>.</summary>
public sealed class CancelResult : ApiResource
{
    /// <summary>The Qasimha order id.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The order status after the cancellation, as an <see cref="OrderStatus"/> name.</summary>
    [JsonConverter(typeof(OrderStatusConverter))]
    public string? Status { get; init; }
}
