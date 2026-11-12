namespace Qasimha.Sdk;

/// <summary>A scan-to-pay order, for <see cref="CheckoutResource.CreateQrSessionAsync"/>.</summary>
public sealed class CreateQrSessionRequest
{
    /// <summary>Your order reference, at most 255 characters.</summary>
    public required string OrderReferenceId { get; set; }

    /// <summary>The amount to finance: the sum of the line totals.</summary>
    public required decimal TotalAmount { get; set; }

    /// <summary>The API accepts <c>SAR</c> only.</summary>
    public string CurrencyCode { get; set; } = "SAR";

    /// <summary>The API accepts <c>SA</c> only.</summary>
    public string CountryCode { get; set; } = "SA";

    /// <summary>The basket lines; at least one.</summary>
    public required IReadOnlyList<CheckoutItem> Items { get; set; }
}
