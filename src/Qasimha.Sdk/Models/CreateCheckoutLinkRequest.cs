namespace Qasimha.Sdk;

/// <summary>Input for <see cref="CheckoutLinksResource.CreateAsync"/>: an order created up-front, reached by SMS link or QR code.</summary>
public sealed class CreateCheckoutLinkRequest
{
    /// <summary>The amount to finance.</summary>
    public required decimal Amount { get; set; }

    /// <summary><see cref="CheckoutLinkChannel.Link"/> (default) or <see cref="CheckoutLinkChannel.Qr"/>.</summary>
    public string Channel { get; set; } = CheckoutLinkChannel.Link;

    /// <summary>The customer's mobile number; required for the <c>Link</c> channel.</summary>
    public string? Phone { get; set; }

    /// <summary><c>"ar"</c> or <c>"en"</c>: the language of the SMS, the call, the push and the hosted page. Defaults to Arabic.</summary>
    public string? Language { get; set; }

    /// <summary>Your order reference. Generated when omitted.</summary>
    public string? OrderReferenceId { get; set; }

    /// <summary>Optional basket lines shown to the customer.</summary>
    public IReadOnlyList<CheckoutLinkItem>? Items { get; set; }

    /// <summary>The Ejar contract the order finances; required for real-estate merchants, ignored otherwise.</summary>
    public string? EjarContractNumber { get; set; }
}
