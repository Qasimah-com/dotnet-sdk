namespace Qasimha.Sdk;

/// <summary>A refund (reversal) recorded on an order.</summary>
public sealed class OrderReversal
{
    /// <summary>The reversal id.</summary>
    public string? Id { get; init; }

    /// <summary>The refunded amount.</summary>
    public decimal Amount { get; init; }

    /// <summary>ISO 4217 currency code.</summary>
    public string? CurrencyCode { get; init; }

    /// <summary>The reason you gave.</summary>
    public string? Comment { get; init; }

    /// <summary>The fulfilment this reversal applies to, when tied to one.</summary>
    public string? FulfillmentId { get; init; }

    /// <summary>The payment provider's refund id.</summary>
    public string? ProviderRefundId { get; init; }

    /// <summary>When it was recorded.</summary>
    public DateTimeOffset? CreatedAt { get; init; }
}
