namespace Qasimha.Sdk;

/// <summary>
/// An in-store order, for <see cref="CheckoutResource.CreatePosSessionAsync"/>. The customer completes
/// the hosted journey at the returned <see cref="PosSession.CheckoutUrl"/>. <see cref="Consumer.Email"/>
/// is required here, and the SDK applies the basket identities because the POS endpoint does not.
/// </summary>
public sealed class CreatePosSessionRequest
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

    /// <summary>The customer, with <see cref="Consumer.Email"/> set.</summary>
    public required Consumer Consumer { get; set; }
}
