namespace Qasimha.Sdk;

/// <summary>How a checkout link reaches the customer.</summary>
public static class CheckoutLinkChannel
{
    /// <summary>Sent by SMS to <see cref="CreateCheckoutLinkRequest.Phone"/>, which is then required.</summary>
    public const string Link = "Link";

    /// <summary>Returned as a URL you render as a QR code.</summary>
    public const string Qr = "Qr";

    internal static bool IsKnown(string? value) =>
        string.Equals(value, Link, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, Qr, StringComparison.OrdinalIgnoreCase);

    internal static bool IsLink(string? value) => string.Equals(value, Link, StringComparison.OrdinalIgnoreCase);
}
