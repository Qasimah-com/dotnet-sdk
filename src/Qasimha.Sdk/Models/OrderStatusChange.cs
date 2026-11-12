using System.Text.Json.Serialization;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>One entry of an order's status history.</summary>
public sealed class OrderStatusChange
{
    /// <summary>The status before, as an <see cref="OrderStatus"/> name.</summary>
    [JsonConverter(typeof(OrderStatusConverter))]
    public string? FromStatus { get; init; }

    /// <summary>The status after, as an <see cref="OrderStatus"/> name.</summary>
    [JsonConverter(typeof(OrderStatusConverter))]
    public string? ToStatus { get; init; }

    /// <summary>What caused the change, for example <c>DecisionDeclined</c>.</summary>
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Trigger { get; init; }

    /// <summary>Free-form detail recorded with the change.</summary>
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Metadata { get; init; }

    /// <summary>When it happened.</summary>
    public DateTimeOffset? CreatedAt { get; init; }
}
