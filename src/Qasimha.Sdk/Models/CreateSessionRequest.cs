namespace Qasimha.Sdk;

/// <summary>
/// An online basket to finance, for <see cref="CheckoutResource.CreateSessionAsync"/>. The SDK checks
/// the basket before sending it: required fields, two-decimal amounts, and the two identities
/// <c>item.TotalAmount == Quantity × UnitPrice − item.DiscountAmount</c> and
/// <c>TotalAmount == Σ item.TotalAmount + TaxAmount + ShippingAmount − DiscountAmount</c>, to the halala.
/// </summary>
public sealed class CreateSessionRequest
{
    /// <summary>Your order reference, at most 255 characters. Reusing one answers <see cref="DuplicateOrderReferenceException"/>.</summary>
    public required string OrderReferenceId { get; set; }

    /// <summary>The amount to finance.</summary>
    public required decimal TotalAmount { get; set; }

    /// <summary>Order-level tax. Default 0.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Shipping charged. Default 0.</summary>
    public decimal ShippingAmount { get; set; }

    /// <summary>Order-level discount. Default 0.</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>The API accepts <c>SAR</c> only.</summary>
    public string CurrencyCode { get; set; } = "SAR";

    /// <summary>The API accepts <c>SA</c> only.</summary>
    public string CountryCode { get; set; } = "SA";

    /// <summary>Free text shown on the order.</summary>
    public string? Description { get; set; }

    /// <summary>One of the <see cref="CheckoutPaymentType"/> values. Default <see cref="CheckoutPaymentType.PayByInstalments"/>.</summary>
    public string PaymentType { get; set; } = CheckoutPaymentType.PayByInstalments;

    /// <summary>Requested number of instalments. Default 4.</summary>
    public int Instalments { get; set; } = 4;

    /// <summary>The basket lines; at least one.</summary>
    public required IReadOnlyList<CheckoutItem> Items { get; set; }

    /// <summary>The shopper.</summary>
    public required Consumer Consumer { get; set; }

    /// <summary>The delivery address. The API's validator requires it even for digital baskets.</summary>
    public required Address ShippingAddress { get; set; }

    /// <summary>The billing address, when different.</summary>
    public Address? BillingAddress { get; set; }

    /// <summary>Optional risk signals about the shopper.</summary>
    public RiskAssessment? RiskAssessment { get; set; }

    /// <summary>Where the shopper is sent after the hosted checkout. Honoured since the API release of 2026-09-25.</summary>
    public MerchantUrl? MerchantUrl { get; set; }
}
