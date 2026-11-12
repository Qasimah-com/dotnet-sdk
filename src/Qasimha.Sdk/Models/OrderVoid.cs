namespace Qasimha.Sdk;

/// <summary>A void recorded on an order.</summary>
public sealed class OrderVoid
{
    /// <summary>The void id.</summary>
    public string? Id { get; init; }

    /// <summary>The voided amount.</summary>
    public decimal Amount { get; init; }

    /// <summary>ISO 4217 currency code.</summary>
    public string? CurrencyCode { get; init; }

    /// <summary>The reason recorded.</summary>
    public string? Reason { get; init; }

    /// <summary>The payment provider's cancellation id.</summary>
    public string? ProviderCancelId { get; init; }

    /// <summary>When it was recorded.</summary>
    public DateTimeOffset? CreatedAt { get; init; }
}
