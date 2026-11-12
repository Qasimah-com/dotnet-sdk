namespace Qasimha.Sdk;

/// <summary>A fulfilment recorded on an order.</summary>
public sealed class OrderFulfillment
{
    /// <summary>The fulfilment id.</summary>
    public string? Id { get; init; }

    /// <summary>The fulfilled amount.</summary>
    public decimal Amount { get; init; }

    /// <summary>ISO 4217 currency code.</summary>
    public string? CurrencyCode { get; init; }

    /// <summary>The carrier.</summary>
    public string? ShippingCompany { get; init; }

    /// <summary>The carrier's tracking number.</summary>
    public string? TrackingNumber { get; init; }

    /// <summary>The carrier's tracking page.</summary>
    public string? TrackingUrl { get; init; }

    /// <summary>The payment provider's capture id.</summary>
    public string? ProviderCaptureId { get; init; }

    /// <summary>When it was recorded.</summary>
    public DateTimeOffset? CreatedAt { get; init; }
}
