using System.Text.Json.Serialization;
using Qasimha.Sdk.Json;

namespace Qasimha.Sdk;

/// <summary>One row of <see cref="OrdersResource.ListAsync"/>.</summary>
public sealed class OrderSummary
{
    /// <summary>The Qasimha order id.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Your order reference.</summary>
    public string? OrderReferenceId { get; init; }

    /// <summary>The order status, as an <see cref="OrderStatus"/> name.</summary>
    [JsonConverter(typeof(OrderStatusConverter))]
    public string? Status { get; init; }

    /// <summary>The decision policy applied.</summary>
    public string? PolicyCode { get; init; }

    /// <summary>The policy type, for example <c>BNPL</c>.</summary>
    public string? PolicyType { get; init; }

    /// <summary>The financed amount.</summary>
    public decimal TotalAmount { get; init; }

    /// <summary>ISO 4217 currency code.</summary>
    public string? CurrencyCode { get; init; }

    /// <summary>When the order was created.</summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>The Qasimha customer id.</summary>
    public string? CustomerId { get; init; }

    /// <summary>The customer's name.</summary>
    public string? CustomerName { get; init; }

    /// <summary>The customer's email.</summary>
    public string? CustomerEmail { get; init; }

    /// <summary>The customer's phone.</summary>
    public string? CustomerPhone { get; init; }

    /// <summary>Number of instalments.</summary>
    public int? Instalments { get; init; }

    /// <summary>The channel, as an <see cref="OrderChannel"/> name.</summary>
    [JsonConverter(typeof(OrderChannelConverter))]
    public string? Channel { get; init; }

    /// <summary>Amount captured so far.</summary>
    public decimal CapturedAmount { get; init; }

    /// <summary>Amount refunded so far.</summary>
    public decimal RefundedAmount { get; init; }

    /// <summary>The Ejar contract number, for real-estate merchants.</summary>
    public string? EjarContractNumber { get; init; }
}
