namespace Qasimha.Sdk;

/// <summary>Input for <see cref="OrdersResource.FulfillAsync"/>. Unset members are not sent.</summary>
public sealed class FulfillRequest
{
    /// <summary>Leave unset to fulfil everything outstanding, or give a positive amount for a partial fulfilment. The API rejects 0.</summary>
    public decimal? ShippedAmount { get; set; }

    /// <summary>The carrier.</summary>
    public string? ShippingCompany { get; set; }

    /// <summary>The carrier's tracking number.</summary>
    public string? TrackingNumber { get; set; }

    /// <summary>The carrier's tracking page.</summary>
    public string? TrackingUrl { get; set; }
}
