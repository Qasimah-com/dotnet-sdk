using System.Text.Json.Serialization;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>Result of <see cref="OrdersResource.CaptureAsync"/>.</summary>
public sealed class CaptureResult : ApiResource
{
    /// <summary>The Qasimha order id.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The order status after the capture, as an <see cref="OrderStatus"/> name.</summary>
    [JsonConverter(typeof(OrderStatusConverter))]
    public string? Status { get; init; }

    /// <summary>When the order was confirmed.</summary>
    public DateTimeOffset? ConfirmedAt { get; init; }
}
